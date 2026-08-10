<div align="center">

# Lobby dodger for DBD Ranked cross-off queues

**Local OCR · Reviewed blacklist · Optional automatic dodge**

[![CI](https://github.com/kaankutluturk/crossoff-lobby-dodger/actions/workflows/ci.yml/badge.svg)](https://github.com/kaankutluturk/crossoff-lobby-dodger/actions/workflows/ci.yml)
[![Latest release](https://img.shields.io/github/v/release/kaankutluturk/crossoff-lobby-dodger)](https://github.com/kaankutluturk/crossoff-lobby-dodger/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/kaankutluturk/crossoff-lobby-dodger/total)](https://github.com/kaankutluturk/crossoff-lobby-dodger/releases)
[![Windows x64](https://img.shields.io/badge/platform-Windows%20x64-357a68)](https://github.com/kaankutluturk/crossoff-lobby-dodger/releases/latest)

A small Windows helper built specifically for [DBD Ranked](https://discord.com/servers/dbdranked-1410340318250926182) cross-off lobbies. It recognizes visible lobby names locally, checks them against a reviewed blacklist, warns on matches, and can optionally complete the normal lobby-leave flow.

**[Download for Windows](https://github.com/kaankutluturk/crossoff-lobby-dodger/releases/latest)** · [View the blacklist](blacklist/blacklist.json) · [Submit evidence](https://github.com/kaankutluturk/crossoff-lobby-dodger/issues/new?template=blacklist-submission.yml)

</div>

## At a glance

| Local OCR | Reviewed blacklist | Two dodge modes |
| --- | --- | --- |
| Reads only the screen area you select. Screenshots and recognized names stay on your PC. | Downloads the versioned GitHub list and retains a local fallback cache. | Warn only, or show the warning and automatically send `Esc` followed by `Enter`. |

A visible name must match in consecutive scans before the client reacts. The same scan path works from both sides of a cross-off lobby, so there is no separate killer/survivor setup.

## Quick start

1. Download `CrossOffLobbyDodger-win-x64.zip` from the [latest release](https://github.com/kaankutluturk/crossoff-lobby-dodger/releases/latest).
2. Extract the complete ZIP and open `CrossOffLobbyDodger.exe`.
3. Select the rectangle containing the visible lobby player names.
4. Run **Test OCR** while a lobby is visible.
5. Choose automatic leaving or warning-only mode, then start monitoring.

> [!TIP]
> Borderless or windowed mode is recommended. Exclusive fullscreen can prevent ordinary screen capture from seeing the game. Select the area again after changing resolution, monitor layout, or UI scale.

## Match behavior

| Mode | After a confirmed blacklist match |
| --- | --- |
| **Automatic** | Shows a non-activating warning first, waits briefly, sends `Esc`, verifies that DBD still has focus, then sends `Enter` and reports the result. |
| **Manual** | Shows the same alias, group, reason, and evidence warning without sending keyboard input. |

## Screen-only boundary

| The client does | The client does not |
| --- | --- |
| Capture a user-selected screen rectangle | Attach to the Dead by Daylight process |
| Run OCR locally with the bundled English model | Read or write game memory |
| Download the public blacklist over HTTPS | Inject code or install a driver |
| Send normal Windows keyboard input when enabled | Inspect game files or network traffic |
| Cache settings and the last valid blacklist locally | Upload screenshots, OCR output, or recognized names |

> [!NOTE]
> The narrow screen-only design reduces interaction with the game, but it is not an anti-cheat guarantee. This is not official Behaviour Interactive software, the executable is currently unsigned, and users run it at their own risk.

## Blacklist and moderation

The current backend is the versioned [`blacklist/blacklist.json`](blacklist/blacklist.json) file. It starts empty intentionally, and only reviewed entries marked `active` are matched.

Submissions should include photo or video evidence. A maintainer reviews the evidence before the live list changes; direct unreviewed additions should not be merged. The planned Discord bot integration may eventually replace this GitHub-backed workflow, but the JSON remains the client feed for now.

Player names can be changed, copied, or imitated. A warning means only that the visible text matched an approved alias—it does not prove the current player's identity or party.

<details>
<summary><strong>Build from source</strong></summary>

Requirements: Windows 10 or later, the .NET 10 SDK, and the Visual C++ 2015–2022 x64 runtime.

```powershell
pwsh scripts/Get-Tessdata.ps1
dotnet restore CrossOffLobbyDodger.sln
dotnet build CrossOffLobbyDodger.sln -c Release
dotnet run --project tests/CrossOffLobbyDodger.SelfTest/CrossOffLobbyDodger.SelfTest.csproj -c Release
dotnet publish src/CrossOffLobbyDodger.Client/CrossOffLobbyDodger.Client.csproj -c Release -r win-x64 --self-contained true -o publish
```

The model downloader pins and verifies the English Tesseract model. GitHub Actions builds, tests, and publishes a Windows artifact for every change.

</details>

See [CONTRIBUTING.md](CONTRIBUTING.md) for code and moderation guidelines. The application is [MIT licensed](LICENSE); OCR components are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
