# Design

## Context

The original updates through Sparkle with automatic checks and an EdDSA-signed feed. On Windows we use Velopack. Verified against current Velopack docs (context7, `/velopack/velopack.docs`): `UpdateManager` checks a downloaded package's checksum against the release feed (`VerifyPackageChecksumAsync`, throws `ChecksumFailedException`); signing is done at pack time with `vpk pack --signParams` (signtool). The feed itself is not signed, so feed integrity rests on HTTPS alone — weaker than Sparkle's EdDSA.

## Goals / Non-Goals

**Goals:**
- Close the gap between "checksum matches a feed" and "the code is ours".
- Zero-admin install and update.

**Non-Goals:**
- MSIX / Microsoft Store distribution (possible later).
- `uiAccess` manifest (would require a Program Files install and admin rights; without it UIPI keeps Rekey out of elevated windows, see keyboard-capture "Elevated windows").

## Decisions

### D1. Velopack
- `VelopackApp.Build().Run()` first line of `Main`; pack id `key-switcher` (the repository name), so the install root is `%LOCALAPPDATA%\key-switcher`; stable stub `Rekey.exe` used for the `Run` key and shortcuts.
- User data stays in `%LOCALAPPDATA%\Rekey` (from `bootstrap-solution`), outside the install root: Velopack deletes the whole install root on uninstall (checked in the Velopack docs, "Uninstalling"), so data inside it could not be kept when the user declines deletion. Not `%APPDATA%` (roaming), because roaming profiles would copy word lists and the word-analysis log to domain servers.
- Channels: `stable` only at first; `beta` later.

### D2. Authenticity beyond checksum
Subclass `UpdateManager` and override `VerifyPackageChecksumAsync`: call base (checksum), then extract the package's main executable to a temp folder and verify with `WinVerifyTrust` (`WTD_REVOKE_WHOLECHAIN`) plus a pinned publisher check (certificate subject and the issuing CA's thumbprint list in code). Any failure → delete package, throw, record Incident `update-signature-failed`.
- Alternative: sign the feed with our own Ed25519 key and verify in the app. Kept as a later hardening step; Authenticode reuses the certificate we need anyway for SmartScreen and antivirus reputation.

### D3. Transport and versions
Releases are published on GitHub Releases (owner's decision). The update source is a compile-time constant: Velopack `GithubSource(repoUrl, accessToken: null, prerelease: false)` on the public repository `hasper16/key-switcher`, so no access token ever ships in the app. The unauthenticated GitHub API limit (60 requests per hour per IP, Velopack docs) is far above one check per 24 h. Default certificate validation; no user-configurable source. Reject versions ≤ current (semantic version compare) before download.

### D4. Release pipeline
GitHub Actions `release.yml` on tag `v*`: build, test, `dotnet publish` (self-contained, single-file, R2R, win-x64), `vpk pack --signParams …` with the certificate from a secret store (cloud HSM or a cloud signing service), then `vpk upload github --repoUrl <release repository> --publish --tag v<version>`. The upload token is the workflow's `GITHUB_TOKEN` with `contents: write`. Signing secrets are never available to pull-request workflows.

### D5. Uninstall
Velopack uninstall hook (`OnBeforeUninstallFastCallback`) removes the `Run` value and asks (message box) whether to delete `%LOCALAPPDATA%\Rekey`. Velopack then removes the install root itself; updates replace only its `current` folder and never touch user data.

## Risks / Trade-offs

- [Compromise of the GitHub account or release repository, or TLS interception with a trusted root] → Authenticode + pinned publisher check stops unsigned or foreign-signed code.
- [Compromise of the signing key] → hardware-backed key (HSM / Trusted Signing); revocation checking enabled.
- [Antivirus flags a keyboard hook in a new binary] → signed builds, consistent publisher, submit releases to Microsoft for analysis.

## Open Questions

- Code-signing certificate (owner deferred; for an open-source individual the cheapest option that names the owner as publisher is an open-source code-signing certificate). The publisher pin in D2 is set once it is chosen.
