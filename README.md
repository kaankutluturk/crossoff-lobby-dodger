# Lobby dodger for DBD Ranked cross-off queues

[![CI](https://github.com/kaankutluturk/crossoff-lobby-dodger/actions/workflows/ci.yml/badge.svg)](https://github.com/kaankutluturk/crossoff-lobby-dodger/actions/workflows/ci.yml)
[![Latest release](https://img.shields.io/github/v/release/kaankutluturk/crossoff-lobby-dodger)](https://github.com/kaankutluturk/crossoff-lobby-dodger/releases/latest)

A small Windows OCR helper for [DBD Ranked](https://discord.com/servers/dbdranked-1410340318250926182) cross-off lobbies. It reads the visible lobby-name area locally, checks names against a reviewed blacklist, warns on matches, and can optionally complete the normal lobby-leave flow.

Built specifically around the DBD Ranked cross-off queue. It is not official Behaviour Interactive software.

**[Download the latest Windows release](https://github.com/kaankutluturk/crossoff-lobby-dodger/releases/latest)** · [View the blacklist](blacklist/blacklist.json) · [Submit evidence](https://github.com/kaankutluturk/crossoff-lobby-dodger/issues/new?template=blacklist-submission.yml)

## What it does

- Watches only the lobby-name area you select and runs OCR locally.
- Compares visible names with the reviewed blacklist stored in this repository.
- Requires the same name to appear in consecutive scans before reacting.
- Shows the warning first, including the alias, group, reason, and evidence.
- In automatic mode, waits briefly and sends `Esc`, followed by `Enter`, to use DBD's normal leave-lobby confirmation.
- In manual mode, warns without sending any keyboard input.

The same scan path is used on both sides of a cross-off lobby, so there is no separate killer/survivor setup.

## Use it

1. Download `CrossOffLobbyDodger-win-x64.zip` from the [latest release](https://github.com/kaankutluturk/crossoff-lobby-dodger/releases/latest).
2. Extract the complete ZIP and open `CrossOffLobbyDodger.exe`.
3. Select the rectangle containing the visible lobby player names.
4. Run **Test OCR** while a lobby is visible.
5. Choose automatic leaving or warning-only mode, then start monitoring.

Borderless or windowed mode is recommended. Exclusive fullscreen can prevent ordinary screen capture from seeing the game. Select the area again after changing resolution, monitor layout, or UI scale.

## Blacklist

The current backend is [`blacklist/blacklist.json`](blacklist/blacklist.json). It starts empty intentionally. Only reviewed entries marked `active` are matched.

Blacklist submissions should include photo or video evidence. A maintainer reviews the evidence before the live list changes; direct unreviewed additions should not be merged. The planned Discord bot integration may eventually replace this GitHub-backed workflow; the JSON stays for now.

Player names can be changed, copied, or imitated. A warning means the visible text matched an approved alias—it does not prove that the current player owns the original account or is grouped with anyone else.

## Screen-only boundary

The client captures a user-selected screen rectangle, downloads the public blacklist over HTTPS, and can send normal Windows keyboard input. It does not attach to DBD, read or write game memory, inject code, install a driver, inspect game files, or upload screenshots/OCR output.

That narrow boundary reduces interaction with the game, but it is not an anti-cheat guarantee. The executable is currently unsigned, the complete source is public, and users run it at their own risk.

<details>
<summary>Build from source</summary>

Requirements: Windows 10 or later, the .NET 10 SDK, and the Visual C++ 2015–2022 x64 runtime.

```powershell
pwsh scripts/Get-Tessdata.ps1
dotnet restore CrossOffLobbyDodger.sln
dotnet build CrossOffLobbyDodger.sln -c Release
dotnet run --project tests/CrossOffLobbyDodger.SelfTest/CrossOffLobbyDodger.SelfTest.csproj -c Release
dotnet publish src/CrossOffLobbyDodger.Client/CrossOffLobbyDodger.Client.csproj -c Release -r win-x64 --self-contained true -o publish
```

The model downloader pins and verifies the English Tesseract model. GitHub Actions builds and tests every change.

</details>

See [CONTRIBUTING.md](CONTRIBUTING.md) for code and moderation guidelines. The application is [MIT licensed](LICENSE); OCR components are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
