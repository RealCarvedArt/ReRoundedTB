# Harness State: ReRoundedTB

Current phase: 10 Deployment (v3.7.3, at the owner's request; phases 8-9 not formally run, see below)
Last updated: 2026-10-10

| # | Phase | Gate | Record | Open findings |
|---|---|---|---|---|
| 1 | Discovery & Planning | retroactive (2026-10-09), not recorded | | |
| 2 | Architecture & Design | retroactive (2026-10-09), not recorded | | |
| 3 | Scaffold & CI Setup | retroactive (2026-10-09), not recorded | | CI lacks SAST/SBOM/secret scan (L5) |
| 4 | Backend Development | N/A (desktop app, no backend) | | |
| 5 | Frontend Development | retroactive (2026-10-09), not recorded | | see U1–U16 |
| 6 | API Integration | N/A (no API) | | |
| 7 | Security & Quality Gates | passed (2026-10-09), with accepted risk R1 | [07-security-quality-gates.md](07-security-quality-gates.md) | None above Low. Known limitations: U5 (fixed layout; scrolls), U12 (Win10 untested), High Contrast/Narrator unverified. Audit rounds 1-6 in `docs/security/audit-2026-10-09.md` |
| 8 | Performance & Optimization | pending | | |
| 9 | Staging / QA | not formally run | [smoke-2026-10-09.md](smoke-2026-10-09.md) | Release smoke test (16 checks) on CI builds stands in as QA evidence |
| 10 | Deployment | v3.7.2 released at the owner's request (2026-10-09) | | Rollback: v3.7.1 stays published (not rehearsed) |
| 11 | Monitor & Iterate | pending | | |

Gate values: pending, passed (date), failed (date), retroactive (date).

## Accepted risks
| Finding | Accepted by | Date | Expires | Logged in threat register |
|---|---|---|---|---|
| M2 unsigned releases | RealCarvedArt | 2026-10-09 | 2027-04-09 | R1 in `docs/security/threat-register.md` |

## Phase 7 gate decision (2026-10-09)
Passed. The security review of rounds 5-6 passed (no Critical/High/Medium). The UX review was a conditional pass, and its conditions were met: on-screen evidence for `fd61b70`, plus hover/pressed accent contrast fixed and measured. The remaining items are Low/Info or known limitations, listed above. M2 (unsigned releases) is accepted as R1 until 2027-04-09. The final fixes after the reviews (SR6-1/2/5, accent states) were requested by the reviewers and verified on screen. They were not re-reviewed.

## Deployment note
The owner asked for a v3.7.2 release after the fixes. The harness wants a passed phase 9 gate and a rehearsed rollback first. Phase 8 (performance) and phase 9 (staging/QA) were not formally run. The 2026-10-09 release smoke test on CI builds is the QA evidence, and v3.7.1 stays downloadable as the rollback.

The release was first built from `c75f683` (CI run 37996207270, zip SHA-256 `828DA976…D7A7`). Later the same day, at the owner's request, the `v3.7.2` tag was moved to `5836ade` and the zip replaced with the build from CI run 38009790575 (zip SHA-256 `9E306356501EA11088FE4FBC97AE0899F78ECEACCC59C47312D79444FCD31158`). That commit only changes UI text, README and docs to US spelling (centered, color, maximized, customize, license). The exe is unchanged (`8892892A…DEAD`), so antivirus trust carries over. The new zip's attestation and hash were checked after downloading it from the release. That build passed the smoke test the same day (`docs/harness/smoke-2026-10-09.md`, last addendum), which also found a pre-existing focus bug: Apply activated the taskbar. Fixed and runtime-verified in `31ec3ed`.

v3.7.3 was released on 2026-10-10 at the owner's request with that fix: tag `v3.7.3` on `cecd270` (version bump and "What's new" line on top of `31ec3ed`), CI run 38049167468 (22 tests passed), zip SHA-256 `E1DBDDFFCF785CCC963284D70C81C68D101B66F92E61F68F35E575B949826207`, exe `4306A583B77229E6906F59239CBCFC0B7183E2F9F7D55F86DD63E7044B98FA03`. The published zip's hash and attestation were checked after downloading it. The fix was runtime-verified on the `31ec3ed` CI build. The 3.7.3 exe itself has a new hash and was not run here. Rollback: v3.7.2.

## Next action
- Run smoke check 12 (crash, debug build) and 16 (Windows 10), plus High Contrast and Narrator passes.
- Phase 8 if performance matters (the worker polls every 100 ms).
- Before 2027-04-09: revisit R1 (code signing).
