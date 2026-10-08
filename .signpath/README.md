# Code signing setup (SignPath Foundation)

CI (`.github/workflows/ci.yml`) is ready to sign releases. The signing steps are skipped until the settings below exist, so builds keep working unsigned in the meantime.

## After SignPath Foundation approves the project

1. **In SignPath**
   - Add the predefined **GitHub.com** trusted build system to the organization and link it to the ReRoundedTB project.
   - Create the artifact configuration from [`artifact-configuration.xml`](artifact-configuration.xml) and make it the project's default.
   - Note the **organization ID**, the **project slug** and the **signing policy slug** (usually `release-signing`).
   - Create an API token for a user with submitter permission on that signing policy.

2. **In GitHub** (repository → Settings → Secrets and variables → Actions)
   - Secret `SIGNPATH_API_TOKEN`: the API token.
   - Variable `SIGNPATH_ORGANIZATION_ID`: the organization ID.
   - Variable `SIGNPATH_PROJECT_SLUG`: the project slug.
   - Variable `SIGNPATH_SIGNING_POLICY_SLUG`: the signing policy slug.

3. **Run CI** (Actions → release → Run workflow). Each signing request waits for manual approval in SignPath. Once approved, CI checks that `ReRoundedTB.exe` and `ReRoundedTB.dll` carry valid Authenticode signatures, packages `ReRoundedTB.zip` from the signed files, and attests it.

4. **Remove the status note** under "Code signing policy" in the main README.

## How it works

1. CI builds the app and fails on known-vulnerable packages.
2. It uploads the unsigned build output as a GitHub artifact and submits it to SignPath with the version (e.g. `3.7.0.0`) as a parameter.
3. SignPath verifies the build came from this repository's workflow on a GitHub-hosted runner, enforces the product name `ReRoundedTB` and the version on both files, and signs them after approval.
4. CI downloads the signed files, checks the signatures, then packages and attests the release zip.
