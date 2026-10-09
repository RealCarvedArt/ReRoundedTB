# Release Smoke Test: ReRoundedTB

Run on a real Windows 11 machine with the release zip from CI, before publishing. Record the date, version and result in the release notes or in `docs/harness/`.

Before starting:
- Back up `%LOCALAPPDATA%\rtb.json`.
- If your antivirus auto-contains unsigned apps (COMODO does), allow the test exe by hash first. Contained runs get a virtualised disk (settings don't really save) and can hang at startup, so their results don't count. A contained window's class name starts with `COMODO:DefaultSandBox:`.

## Checks
| # | Check | Pass when |
|---|---|---|
| 1 | Verify the download: `gh attestation verify ReRoundedTB.zip --repo RealCarvedArt/ReRoundedTB --source-ref refs/heads/master --signer-workflow RealCarvedArt/ReRoundedTB/.github/workflows/ci.yml` | Exit code 0 |
| 2 | Start ReRoundedTB | Taskbar is shaped; tray icon is visible and suits the taskbar's light/dark mode |
| 3 | Change a margin, click Apply, quit, start again | The changed value is still there (`rtb.json` updated) |
| 4 | Type `abc` in a margin, click Apply | Message names the field; focus returns to it; nothing applied |
| 5 | Keyboard only: Tab through the settings window; move the slider with arrow keys; use Alt+T / Alt+R | Order is diagram, radius, margins, options, auto-hide, Help, Apply; slider value saves |
| 6 | Launch the exe a second time | No second copy; the running copy shows its settings |
| 7 | Tray: Pause, then untick Pause | Normal taskbar, then shaped again; tooltip says "paused" while paused |
| 8 | Tray: Reset settings to defaults..., answer No, then Yes | No: nothing changes. Yes: defaults shown and applied |
| 9 | Restart Explorer (Task Manager > Windows Explorer > Restart) | Tooltip says it's waiting, then the taskbar is shaped again |
| 10 | Close the settings window for the first time on a fresh `rtb.json` | One-time notice that ReRoundedTB keeps running in the tray |
| 11 | Tray: Close ReRoundedTB | Normal taskbar; process gone |
| 12 | Crash recovery (debug build with a forced exception, or end the task while auto-hide is off and restart Explorer) | After a handled crash: error box on top, normal taskbar |

Afterwards, restore your `rtb.json` backup.
