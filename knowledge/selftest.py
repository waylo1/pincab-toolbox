"""Le validateur attrape-t-il vraiment ce qu'il prétend ? Packs volontairement cassés."""
import json, subprocess, sys, copy, pathlib, tempfile

base = json.loads(pathlib.Path("pack-2026.08.json").read_text(encoding="utf-8"))
ACTIONS = ("unblock_file,restore_rom_archive,popper_register_table,relink_binary,"
           "kill_zombie_pinup_display,quarantine_orphaned_media")

def run(pack):
    with tempfile.NamedTemporaryFile("w", suffix=".json", delete=False, encoding="utf-8") as f:
        json.dump(pack, f); p = f.name
    r = subprocess.run([sys.executable, "validate_pack.py", p, "--actions", ACTIONS],
                       capture_output=True, text=True)
    return r.returncode, r.stdout

def run_with_knowledge(pack):
    with tempfile.NamedTemporaryFile("w", suffix=".json", delete=False, encoding="utf-8") as f:
        json.dump(pack, f); p = f.name
    r = subprocess.run([sys.executable, "validate_pack.py", p, "--actions", ACTIONS,
                        "--knowledge-cs", "../src/PincabToolbox.App/Knowledge.cs"],
                       capture_output=True, text=True)
    return r.returncode, r.stdout

cases = []

def case(name, mutate, must_fail=True, expect="", runner=run):
    p = copy.deepcopy(base); mutate(p)
    code, out = runner(p)
    ok = (code != 0) if must_fail else (code == 0)
    if expect: ok = ok and expect in out
    cases.append((name, ok, out.strip().splitlines()[-1] if out else ""))

# ADR-005 : action inconnue
case("action hors registre (ADR-005) → rejet",
     lambda p: p["entries"][0]["repairRules"][0].__setitem__("actionId", "rm_rf_everything"),
     expect="absent du registre")

# parité FR/EN
case("traduction EN manquante → rejet",
     lambda p: p["entries"][0].pop("impactEn"), expect="'impactEn' absent")

# TODO résiduel
case("TODO résiduel → rejet",
     lambda p: p["entries"][0].__setitem__("causeFr", "TODO à migrer depuis Knowledge.cs"),
     expect="TODO")

# placeholder dans une procédure manuelle
case("placeholder {0} dans une procédure manuelle → rejet",
     lambda p: p["entries"][0]["repairRules"][0].__setitem__("manualProcedureFr", "Débloquer {0} puis relancer"),
     expect="placeholder")

# confiance basse sans procédure manuelle
case("confiance <70 sans procédure manuelle → rejet",
     lambda p: (p["entries"][0]["repairRules"][0].update(repairConfidence=50),
                p["entries"][0]["repairRules"][0].pop("manualProcedureFr")),
     expect="OBLIGATOIRE")

# scénario référençant un code inexistant
case("scénario → code inconnu → rejet",
     lambda p: p["scenarios"][0]["requires"].append("CODE_QUI_NEXISTE_PAS"),
     expect="absent des entries")

# scénario à un seul code requis
case("scénario à 1 seul code requis → rejet",
     lambda p: p["scenarios"][0].__setitem__("requires", ["BLOCKED_DLL"]),
     expect="au moins 2 codes")

# requis ET exclu
case("code à la fois requis et exclu → rejet",
     lambda p: p["scenarios"][0].__setitem__("excludes", ["BITNESS_MISMATCH_VPM"]),
     expect="jamais se déclencher")

# playbook avec un trou
case("étapes de playbook non consécutives → rejet",
     lambda p: p["scenarios"][0]["repairPlaybook"][2].__setitem__("step", 7),
     expect="non consécutives")

# étape manualOnly sans raison
case("étape manualOnly sans raison FR/EN → rejet",
     lambda p: (p["scenarios"][0]["repairPlaybook"][2].pop("reasonFr"),
                p["scenarios"][0]["repairPlaybook"][2].pop("reasonEn")))

# code dupliqué
case("code dupliqué → rejet",
     lambda p: p["entries"].append(copy.deepcopy(p["entries"][0])))

# couverture éditoriale : un code connu de Knowledge.cs disparaît du pack (le bug ROM_MISSING du 14/08)
case("code Knowledge.cs sans entrée pack → avertissement (pas un rejet)",
     lambda p: p.__setitem__("entries", [e for e in p["entries"] if e["code"] != "ROM_MISSING"]),
     must_fail=False, expect="ROM_MISSING: connu de Knowledge.cs mais absent du pack",
     runner=run_with_knowledge)

# pack sain
case("le pack de référence reste valide", lambda p: None, must_fail=False)
case("le pack de référence reste valide avec --knowledge-cs aussi",
     lambda p: None, must_fail=False, runner=run_with_knowledge)

print()
fails = 0
for n, ok, last in cases:
    print(("  ok    " if ok else "  ÉCHEC ") + n + ("" if ok else f"   → {last}"))
    fails += 0 if ok else 1
print(f"\n{len(cases)-fails}/{len(cases)} garde-fous confirmés")
sys.exit(1 if fails else 0)
