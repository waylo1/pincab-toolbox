# Launch post — VPUniverse / VPForums (EN) — v2 (2026-07-27)

> **Open-beta launch, without prior validation on a real cab (Maxime traveling) — the post is framed accordingly: a call for testers, not a validated result.** Personalize [bracketed] parts.
> Target: VPUniverse "Virtual Pinball Software" section (check forum rules for tool announcements; VPForums equivalent thread a few days later to avoid cross-post spam vibes).
>
> *Reworked 2026-07-27 (evening): the closing teaser promised features that are NOT in Repair v1 (focus watchdog, screen setup, media renaming = the parked Play Optimizer / Table Companion lines). Trimmed to what's actually shippable. Scan-time claim aligned with reality. Added an explicit beta-tester call-out (no real-cab scan yet) + screenshot noted as coming from demo mode, not a real scan. The rest of the post was already solid.*

---

**Thread title:** [FREE TOOL] Pincab Toolbox — scan your VPX/Popper install and find what's broken (missing ROMs, 64-bit mismatches, backglasses…)

---

Hi everyone,

Like many of you, I've spent more evenings **fixing** my cab than playing it. A table crashing with a cryptic VPinMAME error, a DMD that died after moving to 64-bit, a backglass that stopped showing… and every time, an hour of digging through folders to find the cause.

So I built the tool I wished existed: **Pincab Toolbox**, a **free** scanner that diagnoses your install in seconds (a bit longer on a large collection).

**Honest disclaimer:** I'm traveling this week and haven't been able to run it on a real cab yet — 117 automated tests pass green, but nothing replaces a real scan on a real, messy collection. I'm shipping it now anyway as an **open beta**: if you run it at home, tell me what it misses or gets wrong. That's exactly what this post is for.

**What it checks:**

- **Missing ROMs** — reads every table script (`cGameName`), resolves `VPMAlias.txt`, and names the exact missing `.zip` in `VPinMAME\roms`. Original/EM tables are recognized, so it doesn't flag them.
- **Extracted ROMs** — a ROM unzipped into a folder won't be loaded by VPinMAME; it flags that.
- **32/64-bit mismatches** — THE trap of the VPX 10.8 transition: it inventories the bitness of every exe/DLL (VPinMAME, dmddevice, B2S, FlexDMD) and flags broken hybrid installs.
- **Windows-blocked DLLs** — a file extracted from a downloaded ZIP can be quarantined ("Mark of the Web") and fail to load; it detects that.
- **Missing dependencies** — B2S Backglass Server or FlexDMD required by your tables but absent.
- **Backglass & Popper** — missing `.directb2s` files, tables not registered in the PinUP Popper database, PUP-Pack presence.
- **Compatibility** — nFozzy/Roth physics signatures and minimum VPX versions declared in scripts.
- **Outdated tables** *(beta)* — checks against the open-source Virtual Pinball Spreadsheet database and **links to the official page only**.
- **Script Diff** — compare two versions of a table side by side before overwriting yours.

**Ground rules I committed to** (because this community rightly cares):

- **100% local** — nothing is uploaded, zero telemetry, no account.
- **Read-only** — the scanner NEVER modifies a file, a registry key or a database.
- **Never downloads content** — no tables, no ROMs, no media, ever. This is a diagnostic tool, not a downloader. The community sites remain THE source.

**Screenshot (built-in demo mode — no real scan yet, see above):** see `marketing/screenshot-scanner-demo-EN.png`

**Download:** https://github.com/waylo1/pincab-toolbox/releases/latest/download/PincabToolbox.zip
(Unzip the folder and run **PincabToolbox.exe** from inside it — keep the files together.)
(Unsigned exe for now → the usual SmartScreen warning: "More info" → "Run anyway". Code signing lands in the next release.)

There's a built-in **demo mode** if you want to see it in action before pointing it at your real setup.

It's a v0.1 open beta and I'm looking for blunt feedback — false positives, exotic tables, confusing messages. I fix fast, tonight/tomorrow if needed. If it proves useful, a version that **repairs** some of what the scanner finds is in the works — always with a backup first, a preview of what will change, and undo. The scanner itself stays **free and read-only, forever.**

**If that's something you'd want, just say so in a comment** ("interested in the repair version"). No email, no signup — I just want to know how many people it actually concerns before sinking weeks into it, and I'll know who to ping when it ships.

Thanks, and happy flipping!

[Your handle]

---

*Private notes (do not publish):*
- *Reply to EVERY comment in the first 48 hours — even more critical since no real scan happened before publishing.*
- *Top priority: the first confirmed critical false positive → fix and publicly thank the reporter before anything else (credibility).*
- *If a moderator objects to anything → DM, offer to adapt. Forum relationships outrank the post.*
- *Track: downloads D+7 / D+30, feedback count, feature requests.*
- *The download link must lead to an actual download, not the "beta" email-capture page (see the landing review).*
- *Count every "interested in repair" comment → that IS KPI #10 (go/no-go on building the Repair UI). Log each one.*
- *Every posted report → log it in the field-log (see PROCESS-capture-retours.md).*
