# Phase 07: Security & Quality Gates

Project: ReRoundedTB · Date: 2026-10-09 · Lead: main session (security-reviewer, privacy-reviewer, ux-reviewer, qa-runner) · Status: gate failed

## Objective
Run a full pre-staging audit of ReRoundedTB 3.7.1, a released WPF/.NET 10 Windows taskbar tool that modifies Explorer's windows through P/Invoke. The audit was driven by security first: the tool runs at every logon and reshapes a system process's windows. Privacy came second, because the tool reads window titles. UI/UX came third, because it changes something users rely on and must be operable and reversible for everyone.

## Key Activities
- [SEC] Manual security review of P/Invoke, settings deserialization, autostart, manifest, shell/URL launching, CI and supply chain, and secret patterns in git history. Live repo settings were checked through the GitHub API.
- [PRIV] Data inventory and flow review: files, registry, window enumeration, network, crash reporting and committed artifacts.
- [UX] Static WCAG 2.2 AA review of all WPF windows and the tray menu; journeys walked: first launch, edit segment, apply, hide, quit or restore.
- Release build, NuGet vulnerable/outdated audit, test inventory, and analysis of committed SARIF.
- Main session independently re-checked M1, M2, L1, U1 and U4 against the source and the live repo.

## Deliverables
| Deliverable | Path / link | Priority it serves |
|---|---|---|
| Audit report with findings, severity, status | `docs/security/audit-2026-10-09.md` | SEC / PRIV / UX |
| Data inventory | `docs/privacy/data-inventory.md` | PRIV |
| Harness state | `docs/harness/STATE.md` | all |
| Suppression log | in audit report (empty: no scanners configured) | SEC |

## Tools/Best Practices
| Tool or standard | Used for | Priority |
|---|---|---|
| Manual code review + grep; scratch-project tests for deserialization and struct sizes | Security review, since no SAST was available | SEC |
| `gh api` (branch protection, variables, Dependabot, secret scanning), `git ls-remote` (verifying action SHA pins), `gh attestation verify` | CI and supply chain | SEC |
| `dotnet list package --vulnerable/--outdated` | Dependency audit | SEC |
| OWASP ASVS L1 (V5, V10, V14) | Checklist; target level undefined | SEC |
| WCAG 2.2 AA (static) | Accessibility | UX |
| GDPR minimization principle (pointer only) | Privacy | PRIV |

## Reasoning

### 1. Security (paramount)
The threat model is a same-user, medium-integrity desktop tool. The app runs asInvoker with no uiAccess, has no network surface, and deserializes only POCOs with `TypeNameHandling.None`. That rules out remote and privilege-escalation classes, which is why no critical or high findings exist. The remaining risk falls into two areas:

**(a) Release integrity.**
- The binaries are unsigned (M2).
- The attestation can be produced from any branch through `workflow_dispatch` (M1).
- `master` is unprotected.
- So a compromised collaborator token can ship a "verified" malicious build.
- The README's AV-exclusion advice widens the impact to persistence with no scanning.
- Both are medium, not high, because exploitation needs repo write access or same-user code execution first.

**(b) Robustness against Explorer and its own settings (L1–L4).**
- A crash leaves the taskbar invisible or click-through, which is a denial of service to the user.
- None crosses a privilege boundary, so all are low.

The gate requires zero open critical/high findings, which is met. It also requires that mediums have owners and dates; they are assigned to the repo owner with a 2026-11-09 due date, pending the owner's confirmation.

The gate fails on missing controls rather than on findings. There is no SAST, no SBOM, no secret scanning in CI, and the ASVS target is undefined. A gate cannot be "green" when the checks that produce its colour are not running.

DAST and security headers do not apply to a desktop binary. The desktop equivalents are listed in the audit: BinSkim, Authenticode, pinned attestation verification, a malformed-settings corpus, and Explorer-restart and crash-recovery tests. These are adopted as the replacement gate items, rather than marking the web items as passed.

### 2. Privacy
Nothing leaves the device. There is no telemetry, network, clipboard use or registry write. The README claim was verified against the code and against strings in the Release binary. The remaining issues are minimization and hygiene:
- Window titles are read about every second for an IPC trick that only needs class names (P2).
- An empty `rtb.log` is created on every launch (P3).
- Committed upstream tool output contains a developer username (P1).

All are low. A history rewrite for P1 was rejected because the data is already public in the upstream history, so a rewrite would add disruption without reducing exposure. Whether to disclose OS-level crash reporting (WER) is a legal or wording decision left to the owner (P6). The new data inventory makes future privacy regressions detectable by diff.

### 3. UI/UX
Accessibility is a blocking gate because the tool changes a surface every user depends on. A user who cannot operate the settings or understand failures is locked into a broken taskbar. The static review found:
- a keyboard blocker (U1: the radius slider can't be focused)
- unnamed controls (U2, U3)
- silent input rejection (U4)
- fixed layouts that clip at larger text sizes (U5)
- no feedback when Explorer restarts (U7)

On the positive side, quitting restores the original taskbar, and the help text is honest. No runtime UI Automation or screen-reader pass was possible, so contrast findings (U10) remain "Likely". The a11y gate therefore fails.

### Trade-offs
- **Security over convenience.** Removing the AV-exclusion advice (M2) may raise false-positive friction for users until signing lands. Security takes priority, and the fix is to sign, not to exclude.
- **Privacy over simplicity.** Replacing the title-based single-instance IPC (P2/I5) costs a small refactor. It is preferred because it also closes I5.
- **UI/UX vs. Win10 compatibility.** Replacing the invisible `splitHelpButton` (U12) touches the Win10-only flow, which is untested. This is accepted, with a note to test on Win10.

## Summary/Conclusions
ReRoundedTB has no critical or high security findings and no critical/high/medium privacy findings, and its core local-only design is sound. The phase 7 gate **fails** for these reasons:
- the gate controls are missing: no SAST, SBOM or secret scanning in CI, and no ASVS target
- 2 medium release-integrity findings (M1, M2) are open
- the accessibility gate fails (U1 blocker, U2–U7)
- there are no automated tests, so there is no visual or a11y regression gate

Phase 9 (staging/QA) must not start until these are resolved.

Recommended order of work:
1. **CI and repo settings:** protect master, gate sign/attest on master, add CodeQL + gitleaks + SBOM + Dependabot + lock file, pin the README verification.
2. **SignPath onboarding**, and remove the AV advice.
3. **Robustness:** L1/L2 (clamp settings, global handler with taskbar reset).
4. **UX:** U1–U4 (slider focus, accessible names, inline validation).
5. **Remaining items:** U5–U7 and the remaining lows and infos.

## Exit Gate
- [ ] [SEC] Zero open critical/high findings; mediums have owners and dates. Critical/high is met; M1 and M2 owners and dates need the owner's confirmation.
- [ ] [SEC] DAST clean against preview; ASVS target items verified. DAST is N/A and is replaced by desktop gates (not yet in place); the ASVS target is undefined.
- [x] [PRIV] PII and inventory checks green. No PII leaves the device and the inventory now exists; lows P1–P3 are open but don't block.
- [ ] [UX] a11y and visual gates green. Failed: U1 blocker plus U2–U7; no visual regression tooling.
- [x] Suppression log reviewed by `security-reviewer`. Empty; no scanners configured.

Verified by: security-reviewer 2026-10-09, privacy-reviewer 2026-10-09, ux-reviewer 2026-10-09 (static only)
