# Data Inventory: ReRoundedTB

Last reviewed: 2026-10-09 (commit 52d6ee8) · privacy-reviewer

ReRoundedTB keeps everything on the device. It has no telemetry, no update check, no network code and no clipboard use. The only outbound traffic is GitHub links the user clicks in the About window. Lawful basis: not applicable, since no personal data is collected or transmitted by the publisher.

| Data item | Source | Purpose | Storage | Retention | Who can read | PII? | Needed? |
|---|---|---|---|---|---|---|---|
| Settings (per-segment corner radius and margins, mode flags, AutoHide, etc.) | `Types.cs:29-65`, `Interaction.cs:30-68` | Keep the user's taskbar configuration | `%LOCALAPPDATA%\rtb.json` (name shared with upstream RoundedTB for migration) | Until the user deletes it; not removed automatically | User, admins, SYSTEM | No | Yes (`IsCentred` and `IsWindows11` are redundant) |
| Settings temp file | `Interaction.cs:58-66` | Atomic save | `%LOCALAPPDATA%\rtb.json.tmp` | Transient; may be left behind after a crash | As above | No | Yes |
| Log file | `Interaction.cs:72,134-138` | None (logging disabled) | `%LOCALAPPDATA%\rtb.log`, 0 bytes | Truncated every launch | As above | No | No, remove (P3) |
| Startup shortcut (opt-in) | `MainWindow.xaml.cs:613-663` | Run at logon | `%APPDATA%\...\Startup\ReRoundedTB.lnk` | Until toggled off | User, admins | Target path may include the username | Yes |
| Registry read: `CurrentBuild` (HKLM) | `MainWindow.xaml.cs:65-66` | Detect Windows 10 or 11 | Memory | Process lifetime | Process | No | Yes |
| Registry read: `TaskbarAl` (HKCU) | `MainWindow.xaml.cs:250-254`, `Taskbar.cs:136-141` | Detect centred taskbar | Memory (and copied into rtb.json) | Process lifetime | Process | No | Yes |
| Top-level window titles and classes | `Background.cs:48-56`, `MainWindow.xaml.cs:89-96`, `Taskbar.cs:648-669` | Single-instance signal; detect maximised windows and the task switcher | Memory only, discarded immediately | Milliseconds | Process | Titles may hold sensitive text | Classes yes; title reads can be reduced (P2) |
| Process names | `MainWindow.xaml.cs:84,115` | Single instance; detect upstream RoundedTB | Memory | Milliseconds | Process | No | Yes |
| Cursor position | `Background.cs:140,171`, `TaskbarEffect.xaml.cs:64` | Hover segments and auto-hide | Memory | Milliseconds | Process | No | Yes (when enabled) |
| TranslucentTB IPC (mutex probe and refresh message) | `Interaction.cs:140-157` | Compatibility refresh (opt-in) | None | n/a | Local TranslucentTB | No | Yes |
| Global hotkey Win+F2 | `MainWindow.xaml.cs:759` | Toggle the tray segment | None | n/a | Process | No (not a keylogger) | Yes |
| Outbound links (github.com) | `AboutWindow.xaml:110-117` | Help and docs | Opened in the default browser on click | n/a | GitHub (normal browser request) | Browser IP/UA, user-initiated | Yes |
| Crash reports | none in the app; OS WER / Event Log | OS default | Windows | OS policy | Admins; Microsoft if WER upload is enabled | Possibly paths in stack traces | OS-controlled (disclosure: owner decision, P6) |
