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
Round 4 is done: L-F5 and U12 (visible split-mode help), plus the review fixes. U15 was reverted after security review H-1 and is now an accepted minor item. U11 and U16 were confirmed already fixed. Round 4 isn't runtime-verified yet: the owner's own ReRoundedTB was running, and COMODO contains test builds.

Before re-running the phase 7 gate:
1. Run `docs/release-smoke-test.md` (now 16 checks) on an uncontained build. Checks 2 and 13–16 cover the round 4 changes. The CI build of 4f8ae80 passed check 1 (attestation); its runtime run was COMODO-contained.
2. Light-theme contrast is now smoke-test check 13. It couldn't be measured here (see audit round 4).
3. Done: the round 4 reviews (security H-1 led to the U15 revert; the UX lows are fixed).

Still open, not gate-blocking:
- U5 main-window reflow at 300%+ (known limitation)
- U12 on real Windows 10
- P6 (owner decision)
