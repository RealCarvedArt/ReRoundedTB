<img width="16" height="16" alt="RoundedTB" src="https://github.com/user-attachments/assets/2bbcd777-2902-4e4d-ba7d-0681276f6335" /> ReRoundedTB

# ReRoundedTB
#### Add margins, rounded corners and segments to your taskbars!

ReRoundedTB is a maintained fork of [RoundedTB](https://github.com/torchgm/RoundedTB) by torchgm, which is no longer developed. It fixes bugs on current Windows 11 builds and runs on a supported .NET version. The original author's documentation below still applies.

![ReRoundedTB settings and About windows on Windows 11, with a rounded, dynamic taskbar](docs/screenshots/ReRoundedTB.png)

## How do I get it?
Download the latest `ReRoundedTB_*.zip` from [Releases](https://github.com/RealCarvedArt/ReRoundedTB/releases/latest), unzip it and run `ReRoundedTB.exe`. It needs the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (x64). Settings from RoundedTB carry over automatically. If you're switching from the original RoundedTB, close it first so the two don't fight over the taskbar.

Releases are built by GitHub Actions with signed build provenance. To check a download came from this repository, run `gh attestation verify <file> --repo RealCarvedArt/ReRoundedTB` with the [GitHub CLI](https://cli.github.com/).

## To use
### Basic options
The simplest way to use RoundedTB is by simply entering a margin and corner radius.
 - **Margin** - controls how many pixels to remove from each side of the taskbar, creating a margin around it that you can see and click through.
 -  **Corner Radius** - adjusts how round the corners of the taskbar should be.

### Advanced options
The advanced options allow for further customisation, at the cost of some user-friendliness.
- **Independent Margins** - in the advanced settings, a <kbd>...</kbd> button appears on the margin box. Click it to enable independent margins, which allow you to specify the margin for each side of the taskbar. You can also use negative values to hide the rounded corners for some sides, allowing you to "attach" the taskbar to different sides of the monitor.
- **Dynamic Mode (Windows 11)** - dynamic mode automatically resizes the taskbars to accommodate the number of icons in it, making the taskbar behave similarly to macOS' Dock.
- **Show System Tray** - this toggles whether or not the system tray, clock etc. is displayed in dynamic/split mode. It can be toggled at any time by pressing <kbd>Win</kbd>+<kbd>F2</kbd>.
- **TranslucentTB Compatibility** - due to a bug in Windows, apps that alter the composition of the taskbar don't allow RoundedTB's changes to show up automatically. Whilst I'm currently not aware of a fix, I've worked closely with [Sylveon](https://github.com/sylveon) to enable some level of compatibility between [TranslucentTB](https://github.com/TranslucentTB/TranslucentTB) and RoundedTB. This is experimental and *will* flicker slightly. It requires TranslucentTB version 2021.5 to function.
- **About RoundedTB** - provides information about the current version of RoundedTB. The "Debug" section lets you open the config and log files.







## Known issues
 - Auto-hiding is still incredibly experimental and may lead to a lot of flickering, especially with TranslucentTB compatibility or dynamic/split mode enabled. ([#36](https://github.com/torchgm/RoundedTB/issues/36))
 - Rounded corners are not antialiased due to a Windows limitation. ([#4](https://github.com/torchgm/RoundedTB/issues/4))
 - Dynamic mode/split mode only work correctly when the taskbar is horizontal at the top/bottom of the screen.
 - Split mode on Windows 10 only supports the main taskbar, secondary taskbars will not be split.
 - When using dynamic mode, the taskbar may occasionally become too large, too small or not update. This can usually be fixed by moving a window to or from that monitor or briefly changing the taskbar alignment. These issues will be reduced in upcoming updates, don't worry! I just need to refactor a lot of code first.
 - Compatibility with taskbar mods outside of TranslucentTB version 2021.5 is not currently guaranteed.

Fixed in ReRoundedTB: dynamic mode not hiding the left side of the taskbar when the alignment has never been changed ([#98](https://github.com/torchgm/RoundedTB/issues/98)), dynamic mode cutting off icons on current Windows 11, and second-monitor taskbars disappearing in dynamic mode.

## Other info
If anything breaks catastrophically, press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Esc</kbd> to open Task Manager, end ReRoundedTB and then restart Explorer. At worst, just reboot your PC. ReRoundedTB makes no permanent changes (though it will run on startup if you enable it from the tray icon), so restarting should clear any issues.

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
ReRoundedTB is based on [RoundedTB](https://github.com/torchgm/RoundedTB), created by torchgm. The documentation above was written by the original author. Like RoundedTB, ReRoundedTB is licensed under the [GNU General Public License v3.0](LICENSE).

