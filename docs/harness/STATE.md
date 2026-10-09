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
Round 3 is done (tests and BinSkim in CI, ASVS target, M2 accepted as R1, U8, U10).

Before re-running the phase 7 gate:
1. Run `docs/release-smoke-test.md` on an uncontained build. This is the runtime evidence that COMODO containment blocked here.
2. Measure light-theme contrast.

Still open, not gate-blocking:
- U5 main-window reflow at 300%+
- U12 on Win10
- L-F5
- U11, U15, U16 minor items
- P6 (owner decision)