#!/usr/bin/env python3
"""
Validateur de Knowledge Pack — à brancher en CI (.github/workflows).

Ce que le JSON Schema ne peut PAS vérifier et que ce script vérifie :

  1. ADR-005 — tout `actionId` référencé existe dans le registre COMPILÉ.
     Le registre est lu directement dans le code C# : impossible de déclarer
     une capacité par la donnée.
  2. Parité FR/EN — un champ traduit manquant se voit à l'exécution chez
     l'utilisateur, jamais en test.
  3. Placeholders — `FrFixHints` est rendu tel quel (jamais string.Format).
     Un `{0}` qui s'y glisse s'affiche littéralement à l'écran.
  4. Cohérence des scénarios — codes référencés existants, ≥2 requis,
     playbook ordonné sans trou, ruleId résolvable.
  5. Cohérence du gating — une règle non réversible qui prétend être
     automatique, une confiance à 100 posée au doigt mouillé.
  6. TODO restants — un pack avec des TODO ne part pas en production.

Usage :
    python3 validate_pack.py pack-2026.08.json --registry ../src/PincabToolbox.Repair
    python3 validate_pack.py pack-2026.08.json --actions unblock_file,relink_binary
"""
from __future__ import annotations
import argparse, json, re, sys
from pathlib import Path

ERR: list[str] = []
WARN: list[str] = []

def err(m): ERR.append(m)
def warn(m): WARN.append(m)

TRANSLATED = ("title", "player", "explanation", "impact", "cause",
              "verification", "manualProcedure", "reason")

PLACEHOLDER = re.compile(r"\{\d+\}")
# 14/08/2026: word-bounding alone isn't enough here — the Spanish word "todo" ("everything") is a
# complete standalone word that IS "TODO" case-insensitively, so \bTODO\b (IGNORECASE) still flags
# ordinary Spanish prose the moment ES entries exist in this file. TODO/À MIGRER/A MIGRER now
# require their real, conventional ALL-CAPS form — no natural FR/EN/ES sentence produces that by
# accident. FIXME/XXX stay case-insensitive: neither is a real word in any of the three languages,
# so there's nothing legitimate for them to collide with.
TODO = re.compile(r"\b(TODO|À MIGRER|A MIGRER)\b|\b(?i:FIXME|XXX)\b")


# ── 1. registre d'actions, lu dans le CODE (ADR-005) ────────────────────────

def discover_registry(src: Path) -> set[str]:
    """Extrait les ActionId déclarés en C#. Le code fait foi, jamais la donnée."""
    ids: set[str] = set()
    for f in src.rglob("*.cs"):
        t = f.read_text(encoding="utf-8", errors="ignore")
        # public string ActionId => "unblock_file";
        ids |= set(re.findall(r'ActionId\s*(?:=>|=)\s*"([a-z][a-z0-9_]*)"', t))
        # ActionId = "unblock_file"  (initialiseur d'objet)
        ids |= set(re.findall(r'ActionId\s*=\s*"([a-z][a-z0-9_]*)"\s*[,;}]', t))
    return ids


# ── 2..6. règles métier ─────────────────────────────────────────────────────

def check_translation_parity(obj: dict, where: str):
    for base in TRANSLATED:
        fr, en = f"{base}Fr", f"{base}En"
        has_fr, has_en = fr in obj and obj[fr], en in obj and obj[en]
        if has_fr and not has_en:
            err(f"{where}: '{fr}' présent mais '{en}' absent")
        if has_en and not has_fr:
            err(f"{where}: '{en}' présent mais '{fr}' absent")


def check_text_hygiene(obj: dict, where: str):
    for k, v in obj.items():
        if not isinstance(v, str):
            continue
        if TODO.search(v):
            err(f"{where}.{k}: contient un TODO — un pack incomplet ne part pas en production")
        # Le hint de correction est rendu tel quel : un placeholder s'y afficherait littéralement.
        if k.startswith("manualProcedure") and PLACEHOLDER.search(v):
            err(f"{where}.{k}: placeholder numérique interdit (rendu tel quel, pas de string.Format)")
        if v != v.strip():
            warn(f"{where}.{k}: espaces en début ou fin de chaîne")


def check_rule(rule: dict, registry: set[str] | None, where: str):
    aid = rule.get("actionId", "")
    if registry is not None and aid not in registry:
        err(f"{where}: actionId '{aid}' absent du registre compilé "
            f"(ADR-005) — connus : {sorted(registry) or '<aucun>'}")

    conf = rule.get("repairConfidence", 0)
    reversible = rule.get("reversible", False)

    if conf >= 95 and not reversible:
        warn(f"{where}: confiance {conf} sur une action NON réversible — "
             f"le moteur la rétrogradera en ConfirmationRequired (règle d'or)")
    if conf == 100:
        warn(f"{where}: confiance 100 — aucune réparation n'est certaine ; "
             f"98 est le maximum raisonnable avant calibration terrain")
    if conf < 70 and not (rule.get("manualProcedureFr") and rule.get("manualProcedureEn")):
        err(f"{where}: confiance {conf} (<70) → ManualOnly, "
            f"donc une procédure manuelle FR+EN est OBLIGATOIRE")
    if rule.get("backupRequired") is False:
        warn(f"{where}: backupRequired=false — à justifier, "
             f"le défaut du contrat de sûreté est true")

    check_translation_parity(rule, where)
    check_text_hygiene(rule, where)


def check_scenario(sc: dict, codes: set[str], rule_ids: set[str], where: str):
    for c in sc.get("requires", []) + sc.get("supports", []) + sc.get("excludes", []):
        if c not in codes:
            err(f"{where}: code '{c}' référencé mais absent des entries")

    if len(sc.get("requires", [])) < 2:
        err(f"{where}: un scénario exige au moins 2 codes requis (anti-faux-positif)")

    overlap = set(sc.get("requires", [])) & set(sc.get("excludes", []))
    if overlap:
        err(f"{where}: {sorted(overlap)} à la fois requis et exclu — le scénario ne peut jamais se déclencher")

    pb = sc.get("repairPlaybook", [])
    if pb:
        steps = [s["step"] for s in pb]
        if sorted(steps) != list(range(1, len(steps) + 1)):
            err(f"{where}: étapes du playbook non consécutives à partir de 1 : {sorted(steps)}")
        for s in pb:
            if not s.get("manualOnly") and s["ruleId"] not in rule_ids:
                err(f"{where}: étape {s['step']} référence la règle inconnue '{s['ruleId']}'")
            if s.get("manualOnly") and not (s.get("reasonFr") and s.get("reasonEn")):
                err(f"{where}: étape {s['step']} est manualOnly sans raison FR+EN — "
                    f"l'utilisateur doit savoir POURQUOI avant de cliquer")
        if any(s.get("manualOnly") for s in pb):
            warn(f"{where}: playbook PARTIEL — vérifier que l'UI l'annonce avant d'agir")

    check_translation_parity(sc, where)
    check_text_hygiene(sc, where)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("pack")
    ap.add_argument("--registry", help="dossier source de PincabToolbox.Repair")
    ap.add_argument("--actions", help="liste d'ActionId séparés par des virgules (si pas de source)")
    a = ap.parse_args()

    try:
        pack = json.loads(Path(a.pack).read_text(encoding="utf-8"))
    except Exception as e:
        print(f"✗ pack illisible : {e}")
        return 2

    registry: set[str] | None = None
    if a.registry:
        registry = discover_registry(Path(a.registry))
        print(f"registre compilé : {sorted(registry) or '<aucune action trouvée>'}")
    elif a.actions:
        registry = {x.strip() for x in a.actions.split(",") if x.strip()}
    else:
        warn("aucun registre fourni — la règle ADR-005 n'a PAS été vérifiée")

    entries = pack.get("entries", [])
    codes = {e["code"] for e in entries}
    rule_ids: set[str] = set()

    seen: set[str] = set()
    for e in entries:
        code = e.get("code", "?")
        where = f"entries[{code}]"
        if code in seen:
            err(f"{where}: code dupliqué")
        seen.add(code)

        check_translation_parity(e, where)
        check_text_hygiene(e, where)

        if e.get("severity") in ("Ok", "Info") and e.get("repairRules"):
            warn(f"{where}: sévérité {e['severity']} avec une règle de réparation — "
                 f"réparer ce qui va bien ?")

        for r in e.get("repairRules", []):
            rid = r.get("id", "?")
            if rid in rule_ids:
                err(f"{where}: id de règle dupliqué '{rid}'")
            rule_ids.add(rid)
            check_rule(r, registry, f"{where}.rules[{rid}]")

    for sc in pack.get("scenarios", []):
        check_scenario(sc, codes, rule_ids, f"scenarios[{sc.get('id','?')}]")

    print(f"\n{len(entries)} entrées · {len(rule_ids)} règles · "
          f"{len(pack.get('scenarios', []))} scénarios")
    for w in WARN:
        print(f"  ⚠  {w}")
    for e in ERR:
        print(f"  ✗  {e}")

    if ERR:
        print(f"\n✗ ÉCHEC — {len(ERR)} erreur(s), {len(WARN)} avertissement(s)")
        return 1
    print(f"\n✓ pack valide — {len(WARN)} avertissement(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
