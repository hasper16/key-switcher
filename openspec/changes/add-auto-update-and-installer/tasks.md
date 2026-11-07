# Tasks

## 1. Data layout and versions

- [ ] 1.1 Write a failing test that the user-data folder never lies inside the Velopack root app dir (fake locator with root `%LOCALAPPDATA%\key-switcher`); implement the path resolver; verify it passes
- [ ] 1.2 Write failing tests for the version gate (lower or equal → rejected); implement; verify they pass

## 2. Velopack integration

- [ ] 2.1 Add Velopack, call `VelopackApp.Build().Run()` first in `Main`, pack locally with `vpk pack`; verify manually that the Setup installs per-user without UAC, appears in Start and Installed apps, and starts Rekey
- [ ] 2.2 Add `AutoCheckForUpdates` (on) to `Settings`; write failing scheduler tests with a fake clock (check at start and every 24 h when on; none when off; tray item checks at once); implement with the tray item showing "Up to date" / "Restart to update"; verify the tests pass and manually against a local feed with a newer version
- [ ] 2.3 Verify manually that an update keeps settings, word lists and launch at sign-in

## 3. Authenticity

- [ ] 3.1 Write failing tests for the signature policy (valid pinned publisher passes; unsigned, other publisher, revoked → fail) using test-signed fixtures; implement `WinVerifyTrust` + publisher pin; verify they pass
- [ ] 3.2 Override `VerifyPackageChecksumAsync` to run checksum then signature checks, discard on failure and record an Incident; verify manually with a tampered package and an unsigned package from a local feed

## 4. Uninstall and release

- [ ] 4.1 Implement the uninstall hook (remove `Run` value, ask to delete `%LOCALAPPDATA%\Rekey`); verify manually both answers, and that the data survives when the user declines
- [ ] 4.2 Create `.github/workflows/release.yml` (tag-triggered, publish, sign via secret store, `vpk pack`, `vpk upload github --publish`); verify with a dry run on a test tag that unsigned PR builds cannot access signing secrets
- [ ] 4.3 Document the release process and certificate handling in `docs/release.md`; verify a teammate can follow it to produce a local signed test build
