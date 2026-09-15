# Windows installer

The release installer is a single **Windows ARM64** EXE. It includes the companion app, all 34 display sprites and the .NET 8 runtime. It installs for the current Windows account, creates a Start menu shortcut and an entry in Windows Installed Apps, and offers automatic startup. Existing preferences stay in `%LOCALAPPDATA%\FableAstra\settings.json`; an upgrade keeps them.

Download the installer from the repository's [v1.2.1 release](https://github.com/clxcht/fable-astra-desktop-companions/releases/tag/v1.2.1). Run it, leave **Start automatically when I sign in** enabled if wanted, and select **Install and launch**. The installer has no code-signing certificate.

## Build

With the .NET 8 SDK installed, run `Installer\Build-Installer.ps1` from the repository. It publishes a self-contained ARM64 app, copies the final sprites from `FableAstra/Assets`, creates and embeds a SHA-256 manifest, then publishes a single installer to `Release/`. Every embedded app file is verified before the installed app is replaced. Large installer binaries are attached to GitHub Releases rather than stored in Git history.

The build uses .NET's [self-contained deployment and single-file publishing](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview). The earlier portable app and backup ZIP use the installed .NET 8 Desktop Runtime; the release installer includes that runtime.

## Installation checks

- `--verify-payload` checks the entire embedded archive and writes `Payload-verification.json` beside the installer.
- `--quiet` installs without the wizard and retains the existing startup choice, or enables startup on a new installation.
- `--quiet --no-launch` performs the same installation without opening the companions.
- `--preview <path.png>` renders the installer window for visual inspection.

Uninstall through **Settings → Apps → Installed apps → Fable + Astra**. Uninstall removes the app, shortcut, startup entry and installation registration while preserving preferences for a future reinstall.
