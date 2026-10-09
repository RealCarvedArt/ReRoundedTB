# Release Smoke Test: ReRoundedTB

Run on a real Windows 11 machine with the release zip from CI, before publishing. Record the date, version and result in the release notes or in `docs/harness/`.

Before starting:
- Back up `%LOCALAPPDATA%\rtb.json`.
- If your antivirus auto-contains unsigned apps (COMODO does), allow the test exe by hash first. Contained runs get a virtualised disk (settings don't really save) and can hang at startup, so their results don't count. A contained window's class name starts with `COMODO:DefaultSandBox:`.

## Checks
| # | Check | Pass when |
|---|---|---|
| 1 | Verify the download: `gh attestation verify ReRoundedTB.zip --repo RealCarvedArt/ReRoundedTB --source-ref refs/heads/master --signer-workflow RealCarvedArt/ReRoundedTB/.github/workflows/ci.yml` | Exit code 0 |
| 2 | Start ReRoundedTB | Taskbar is shaped. Tray icon is visible and suits the taskbar's light/dark mode. Hover it: tooltip says "active". Right-click it: the menu opens and Show ReRoundedTB opens the settings window. Win+F2 toggles the tray segment (in dynamic mode) |
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
| 13 | Light/dark: start in Dark mode with the settings window never opened. Tray > Pause. Switch Windows to Light mode (Settings > Personalisation > Colours). Then open the settings window | While paused, the tray icon swaps to the dark glyph. The settings window opens in the light theme (not dark). With a contrast checker (e.g. Colour Contrast Analyser), sample the actual on-screen pixels of the dimmed heading, a dimmed segment button, a field label, the Split mode help button (if shown) and keyboard focus outlines. Text needs at least 4.5:1 (the 60 px heading 3:1), and segment-button borders and focus outlines need 3:1 against their surroundings. Repeat over a light and a dark wallpaper, because Mica tints with the wallpaper. Untick Pause and switch back |
| 14 | Close ReRoundedTB, rename `%LOCALAPPDATA%
tb.json` away, start ReRoundedTB, then close it and put the file back | The Help window opens first (no other ReRoundedTB window in Alt+Tab). After closing it, the settings window shows and the tray icon is there |
| 15 | With the settings window hidden: left-click the tray icon, then left-click it again. Also launch the exe a second time while it's hidden | The first click shows and focuses the window and the tray menu item reads "Hide ReRoundedTB". The second click keeps it shown, with no flicker. The second launch shows it, with no extra window or Alt+Tab entry |
| 16 | Windows 10 only: tick Dynamic mode, close the help, then press Tab from the Widgets segment back to the diagram group | Help opens once on ticking. A visible "Split mode help" button sits under the diagram text, is the first stop in the diagram group, and opens the help with Enter. Tray > Reset with dynamic mode on doesn't pop up the help |

Afterwards, restore your `rtb.json` backup.
