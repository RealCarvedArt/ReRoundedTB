<img width="16" height="16" alt="RoundedTB" src="https://github.com/user-attachments/assets/2bbcd777-2902-4e4d-ba7d-0681276f6335" /> ReRoundedTB

# ReRoundedTB
#### Add margins, rounded corners and segments to your taskbars!

ReRoundedTB is a maintained fork of [RoundedTB](https://github.com/torchgm/RoundedTB) by torchgm, which is no longer developed. It fixes bugs on current Windows 11 builds and runs on a supported .NET version.

![ReRoundedTB settings and About windows on Windows 11, with a rounded, dynamic taskbar](docs/screenshots/ReRoundedTB.png)

## How do I get it?
1. Install the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (x64) if you don't have it.
2. Download the latest `ReRoundedTB_*.zip` from [Releases](https://github.com/RealCarvedArt/ReRoundedTB/releases/latest), unzip it anywhere and run `ReRoundedTB.exe`.
3. ReRoundedTB starts in the tray. Left-click its tray icon to open the settings.

**Requirements:** Windows 11.

**Switching from RoundedTB:** close RoundedTB from its tray icon first. ReRoundedTB won't start while RoundedTB is running, so the two don't fight over the taskbar. Settings from RoundedTB P3.2 or later (including the RoundedTB-named 3.7.0) carry over fully. With settings from RoundedTB R3.1 or earlier, the dynamic mode setting carries over, but margins and corner radius go back to the defaults.

**Verifying a download:** releases are built by GitHub Actions with signed build provenance. To check a download came from this repository, run `gh attestation verify <file> --repo RealCarvedArt/ReRoundedTB` with the [GitHub CLI](https://cli.github.com/).

## To use
The preview at the top of the settings window shows your taskbar's segments. Click a segment to edit it, change the values, then click **Apply**.

### Basic options
- **Corner radius** - how rounded the selected segment's corners are.
- **Top, bottom, left and right margin** - how many pixels to trim from each side of the selected segment. The trimmed area is see-through and click-through.

Without dynamic mode, the whole taskbar is a single segment.

### Advanced options
- **Dynamic mode (Windows 11)** - shrinks the taskbar to fit its icons, a bit like a dock. The app list and the system tray become separate segments, plus the widgets area on the far left when the taskbar is centred. Each segment has its own corner radius and margins.
- **Show this segment** - shown when the tray or widgets segment is selected; shows or hides that segment in dynamic mode. Press <kbd>Win</kbd>+<kbd>F2</kbd> at any time to toggle the tray.
- **Show segments only when hovered over with the mouse** - shows the tray and widgets segments only while the mouse is over them. This uses more CPU.
- **When a window is maximised, restore the taskbar** - turns the taskbar back to normal on any monitor with a maximised window.
- **When alt+tab or win+tab is pressed, restore the taskbar (Windows 11)** - does the same while the task switcher is open. Needs the option above.
- **Improve compatibility with TranslucentTB and other mods** - lets ReRoundedTB work alongside [TranslucentTB](https://github.com/TranslucentTB/TranslucentTB). Due to a bug in Windows, apps that change the taskbar's composition stop ReRoundedTB's changes from showing up; this asks TranslucentTB to refresh the taskbar after each change. It may flicker slightly. It was built for TranslucentTB 2021.5; newer versions haven't been tested.
- **Auto-hide** - "Always hide" fades the taskbar out until the mouse reaches the bottom of the screen. This is ReRoundedTB's own auto-hide and is experimental. Keep Windows' own taskbar auto-hide turned off.

### Tray menu
- **Run at startup** - starts ReRoundedTB when you sign in.
- **Show / Hide ReRoundedTB** - opens or hides the settings window. Closing the settings window also hides it to the tray.
- **Close ReRoundedTB** - quits and puts your taskbars back to normal.

The **Help** button opens the About window, where the **Debug** section can open your settings file.

## Known issues
- Windows' own taskbar auto-hide setting isn't supported and can cause heavy flickering or an unreachable taskbar. ReRoundedTB's own auto-hide is experimental and may flicker, especially with TranslucentTB compatibility or dynamic mode on. ([#36](https://github.com/torchgm/RoundedTB/issues/36))
- Rounded corners aren't antialiased, due to a Windows limitation. ([#4](https://github.com/torchgm/RoundedTB/issues/4))
- Dynamic mode only works when the taskbar is at the top or bottom of the screen.
- In dynamic mode the taskbar can occasionally end up the wrong size or not update. Opening or moving a window on that monitor, or briefly changing the taskbar alignment, usually corrects it.
- Taskbar mods other than TranslucentTB may conflict with ReRoundedTB.

Fixed in ReRoundedTB: dynamic mode not hiding the left side of the taskbar when the alignment has never been changed ([#98](https://github.com/torchgm/RoundedTB/issues/98)), dynamic mode cutting off icons on current Windows 11, second-monitor taskbars disappearing in dynamic mode, the taskbar going square after running for a while, and taskbars staying square after Explorer restarts.

## Troubleshooting
- **Nothing happens to the taskbar and there's no tray icon:** your antivirus may have sandboxed ReRoundedTB. Builds aren't code-signed yet, so tools such as Comodo can auto-contain them, especially on first launch. Add `ReRoundedTB.exe` to your antivirus's trusted files.
- **Something breaks badly:** press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Esc</kbd> to open Task Manager, end ReRoundedTB, then restart Windows Explorer. At worst, reboot. ReRoundedTB makes no permanent changes to Windows, so this clears any issues.
- **Uninstalling:** turn off **Run at startup** in the tray menu, close ReRoundedTB, then delete its folder. Your settings are in `%LOCALAPPDATA%\rtb.json` if you want to remove them too.

## Code signing policy
Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

> **Status:** code signing is being set up. Until it's active, releases are unsigned. You can still confirm a download was built from this repository with `gh attestation verify` (see [How do I get it?](#how-do-i-get-it)).

Releases are built automatically from this repository by GitHub Actions, and every signing request is approved manually.

**Team roles**
- Committers and reviewers: [RealCarvedArt](https://github.com/RealCarvedArt)
- Approvers: [RealCarvedArt](https://github.com/RealCarvedArt)

**Privacy policy**
This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it.

## Credits and licence
ReRoundedTB is based on [RoundedTB](https://github.com/torchgm/RoundedTB), created by torchgm. Parts of this README are adapted from RoundedTB's documentation. Like RoundedTB, ReRoundedTB is free software under the [GNU General Public License v3.0](LICENSE).
