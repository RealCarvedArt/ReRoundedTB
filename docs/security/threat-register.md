# Threat Register: ReRoundedTB

Accepted risks. Each entry was accepted in writing by the owner, for that specific finding only, and expires on the date shown. At expiry, close the finding or re-accept it.

| ID | Finding | Severity | Accepted by | Date | Expires | Owner | Mitigations in place | Revisit when |
|---|---|---|---|---|---|---|---|---|
| R1 | M2: releases are not Authenticode-signed (audit 2026-10-09) | Medium | RealCarvedArt (repo owner): "skip signing" | 2026-10-09 | 2027-04-09 | RealCarvedArt | Every release is built by CI from `master` with SLSA build provenance and an attested SBOM. The README gives the pinned `gh attestation verify` command (`--source-ref refs/heads/master --signer-workflow .../ci.yml`). It tells users to verify first, then allow the specific file by hash or signature, never a folder exclusion. Branch protection blocks force-pushes and deletion of `master`. CI signing via SignPath is wired up and switches on automatically once the repository variables are set. | SignPath approves the project, or a free signing route becomes available |

## Known consequences of R1
- SmartScreen shows "Unknown publisher" and antivirus tools may auto-contain the exe. Example: COMODO contains unsigned builds, which virtualises their file writes and can make them hang at startup.
- A user who skips verification can't tell a tampered download from a genuine one.
