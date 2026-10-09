# Security Requirements: ReRoundedTB

## Verification target
**OWASP ASVS 4.0.3 Level 1**, applied to the chapters that fit a local, non-networked desktop app. ReRoundedTB has no server, accounts, sessions, network code or stored personal data, so L1 is proportionate. The real risks are local: tampered binaries, hostile settings files, and leaving Explorer in a broken state.

| ASVS chapter | Applies | Why / how it's checked |
|---|---|---|
| V1 Architecture | Partly | Trust boundary is the user's own session: runs asInvoker, no uiAccess, no elevation |
| V5 Validation | Yes | `rtb.json` and typed values are parsed with type-safe deserialisation and clamped; malformed-settings tests in CI |
| V7 Errors and logging | Yes | Crash handler restores the taskbar; no logs; error text shows the exception type only, no paths |
| V8 Data protection | Yes | No personal data stored or sent (`docs/privacy/data-inventory.md`) |
| V10 Malicious code and supply chain | Yes | Pinned actions and packages, lock file, NuGet audit, CodeQL, gitleaks, SBOM, build provenance, binary hardening check |
| V14 Configuration | Yes | Manifest (asInvoker, PerMonitorV2), branch protection, least-privilege CI tokens |
| V2-V4, V9, V11-V13 | No | No authentication, sessions, access control, network communication, business logic, files from others, or APIs |

## Desktop replacements for web-only gate items
DAST, security headers, CORS and TLS don't apply. The phase 7 gate uses these instead:
- Binary hardening (ASLR, DEP, CFG, high-entropy VA) checked in CI
- Malformed `rtb.json` tests in CI
- Pinned `gh attestation verify` on releases
- Crash-recovery and Explorer-restart checks in the release smoke test
