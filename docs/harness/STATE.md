# Harness State: ReRoundedTB

Current phase: 7 Security & Quality Gates (gate failed; remediation in progress)
Last updated: 2026-10-09

| # | Phase | Gate | Record | Open findings |
|---|---|---|---|---|
| 1 | Discovery & Planning | retroactive (2026-10-09), not recorded | | |
| 2 | Architecture & Design | retroactive (2026-10-09), not recorded | | |
| 3 | Scaffold & CI Setup | retroactive (2026-10-09), not recorded | | CI lacks SAST/SBOM/secret scan (L5) |
| 4 | Backend Development | N/A (desktop app, no backend) | | |
| 5 | Frontend Development | retroactive (2026-10-09), not recorded | | see U1–U16 |
| 6 | API Integration | N/A (no API) | | |
| 7 | Security & Quality Gates | failed (2026-10-09) | [07-security-quality-gates.md](07-security-quality-gates.md) | M1, M2, L1–L5, P1–P4, U1–U7 (+ info/low) — see `docs/security/audit-2026-10-09.md` |
| 8 | Performance & Optimization | pending | | |
| 9 | Staging / QA | pending (blocked by 7) | | |
| 10 | Deployment | pending | | v3.7.1 already shipped pre-harness |
| 11 | Monitor & Iterate | pending | | |

Gate values: pending, passed (date), failed (date), retroactive (date).

## Accepted risks
| Finding | Accepted by | Date | Expires | Logged in threat register |
|---|---|---|---|---|
| M2 unsigned releases | RealCarvedArt | 2026-10-09 | 2027-04-09 | R1 in `docs/security/threat-register.md` |

## Next action
The release smoke test ran uncontained on 2026-10-09 (`docs/harness/smoke-2026-10-09.md`): 11 checks pass, 1 not run (crash, covered in round 2), 1 N/A (Win10). It found 5 issues, all fixed and retested on CI builds (audit round 5: light contrast, the accent after a live theme switch, tray Close showing the hide notice, and two Explorer-restart gaps).

Before marking the phase 7 gate passed:
1. Independent security and UX review of the round 5 fixes (`f6dc255`, `81ab49f`, `eeb7109`).

Still open, not gate-blocking:
- U5 main-window reflow at 300%+ (known limitation)
- U15 (accepted after review H-1), ST-6, ST-7 (info)
- U12 and smoke check 16 on real Windows 10
- P6 (owner decision)
