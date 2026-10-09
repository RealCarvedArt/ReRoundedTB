# Data Inventory: ReRoundedTB

Last reviewed: 2026-10-09 (commit 52d6ee8; updated for the round-2 fixes) · privacy-reviewer

ReRoundedTB keeps everything on the device. It has no telemetry, no update check, no network code and no clipboard use. The only outbound traffic is GitHub links the user clicks in the About window. Lawful basis: not applicable, since no personal data is collected or transmitted by the publisher.

| Data item | Source | Purpose | Storage | Retention | Who can read | PII? | Needed? |
|---|---|---|---|---|---|---|---|
| Settings (per-segment corner radius and margins, mode flags, AutoHide, etc.) | `Types.cs:29-65`, `Interaction.cs:30-68` | Keep the user's taskbar configuration | `%LOCALAPPDATA%\rtb.json` (name shared with upstream RoundedTB for migration) | Until the user deletes it; not removed automatically | User, admins, SYSTEM | No | Yes (`IsCentred` and `IsWindows11` are redundant) |
| "Seen the tray notice" flag (`HasSeenTrayNotice` in rtb.json) | `Types.cs`, `MainWindow.xaml.cs` (OnClosing) | Show the "still running in the tray" notice only once | `%LOCALAPPDATA%\rtb.json` | As settings | As settings | No | Yes |
| Registry read: `SystemUsesLightTheme` (HKCU `Themes\Personalize`) | `MainWindow.xaml.cs` (`IsTaskbarLight`) | Pick a tray icon that's visible on the taskbar | Memory | Read about once a second | Process | No | Yes |
| Settings temp file | `Interaction.cs:58-66` | Atomic save | `%LOCALAPPDATA%\rtb.json.tmp` | Transient; may be left behind after a crash | As above | No | Yes |
| Log file | removed | No longer created (P3 fixed). Older versions left an empty `%LOCALAPPDATA%\rtb.log` | n/a | n/a | n/a | No | No |
| Startup shortcut (opt-in) | `MainWindow.xaml.cs:613-663` | Run at logon | `%APPDATA%\...\Startup\ReRoundedTB.lnk` | Until toggled off | User, admins | Target path may include the username | Yes |
| Registry read: `CurrentBuild` (HKLM) | `MainWindow.xaml.cs:65-66` | Detect Windows 10 or 11 | Memory | Process lifetime | Process | No | Yes |
| Registry read: `TaskbarAl` (HKCU) | `MainWindow.xaml.cs:250-254`, `Taskbar.cs:136-141` | Detect centred taskbar | Memory (and copied into rtb.json) | Process lifetime | Process | No | Yes |
| Other apps' window class names, positions and states | `Taskbar.cs` (maximised / task-switcher checks) | Detect maximised windows and the task switcher | Memory only, discarded immediately | Milliseconds | Process | No; other apps' titles are not read (P2 fixed) | Yes |
| ReRoundedTB's own window title | `Background.cs:50`; `MainWindow.xaml.cs` second-launch path (only windows whose class is ReRoundedTB's) | "Show settings" request from older versions; newer launches use a broadcast window message | Memory | Milliseconds | Process | No | Yes |
| Process names | `MainWindow.xaml.cs:84,115` | Single instance; detect upstream RoundedTB | Memory | Milliseconds | Process | No | Yes |
| Cursor position | `Background.cs` (hover and auto-hide checks) | Hover segments and auto-hide | Memory | Milliseconds | Process | No | Yes (when enabled) |
| TranslucentTB IPC (mutex probe and refresh message) | `Interaction.cs:140-157` | Compatibility refresh (opt-in) | None | n/a | Local TranslucentTB | No | Yes |
| Global hotkey Win+F2 | `MainWindow.xaml.cs:759` | Toggle the tray segment | None | n/a | Process | No (not a keylogger) | Yes |
| Outbound links (github.com) | `AboutWindow.xaml:110-117` | Help and docs | Opened in the default browser on click | n/a | GitHub (normal browser request) | Browser IP/UA, user-initiated | Yes |
| Crash reports | none in the app; OS WER / Event Log | OS default | Windows | OS policy | Admins; Microsoft if WER upload is enabled | Possibly paths in stack traces | OS-controlled (disclosure: owner decision, P6) |
