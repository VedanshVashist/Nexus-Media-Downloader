# Nexus

A fast, modern desktop media downloader for Windows. Paste a link, pick a format, and
download video, audio, thumbnails, subtitles, chapters and metadata — with a managed
download queue, history, favorites, four built-in themes and custom wallpapers.

Nexus is a WPF front-end over the excellent [yt-dlp](https://github.com/yt-dlp/yt-dlp)
and [FFmpeg](https://ffmpeg.org/) command-line tools. It focuses on a clean,
responsive, native-feeling UI and a robust, well-layered codebase.

> **Status:** version 0.1.0. Windows 10/11 (x64).

---

## Highlights

- **One-paste workflow** — analyze a URL, preview its metadata and thumbnail, and choose
  exactly what to download.
- **Full format control** — pick container/quality, extract audio, or grab thumbnails,
  subtitles, chapters and metadata. Optionally embed them into the output file.
- **Managed downloads** — a concurrent queue (3 by default, up to 10) with pause, cancel,
  retry and live progress, speed and ETA.
- **Batch & playlists** — queue many URLs at once and expand playlists.
- **History & favorites** — searchable history and a favorites list for links you reuse.
- **Made yours** — four themes (Midnight, Aurora, Crimson, Cyberpunk), optional custom
  wallpaper with blur/darkness/opacity, and a friendly first-run setup wizard.
- **Configurable output** — templated file names and per-type destination folders.
- **Portable or installed** — ship a self-contained portable folder/zip, or a Windows
  installer.

## Screenshots

Screenshots live in `docs/images/` (not included in this repository snapshot). Build and
run the app to see the live UI.

---

## Architecture at a glance

Nexus uses a strict layered architecture with dependency inversion. See
[`docs/architecture.md`](docs/architecture.md) for the full picture.

```
Nexus.App            WPF, MVVM (views, view-models, converters, UI-only services)
   |  depends on
Nexus.Core           Domain models, enums, DTOs, service interfaces, exceptions
   ^  implemented by
Nexus.Infrastructure yt-dlp / FFmpeg process wrappers, EF Core + SQLite, settings,
                     themes, thumbnails, dependency manager, notifications, logging
```

- **`Nexus.Core` never references WPF.** All cross-layer contracts are interfaces in
  Core; concrete implementations live in Infrastructure (or, for UI-only concerns such
  as dialogs and the dispatcher, in the App).
- Composition happens once at startup through the .NET Generic Host and
  `Microsoft.Extensions.DependencyInjection`.
- The UI follows MVVM using
  [`CommunityToolkit.Mvvm`](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/).

## Tech stack

- **.NET 10**, C# (latest), nullable reference types
- **WPF** with `WindowChrome` custom title bar and a semantic, swappable theme system
- **CommunityToolkit.Mvvm** for observable objects and relay commands
- **Entity Framework Core 10 + SQLite** for history/favorites/settings persistence
- **Serilog** for structured file/console logging
- **yt-dlp** and **FFmpeg/ffprobe** as external, separate-process tools
- **xUnit** for unit tests

---

## Requirements

- Windows 10 or 11 (x64)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) to build from source
  (end users of a self-contained build do **not** need any runtime installed)
- PowerShell 5.1+ (Windows PowerShell) or PowerShell 7+ to run the helper scripts
- Network access to fetch the external tools on first setup

## Getting started (from source)

```powershell
# 1. Restore and build
dotnet restore
dotnet build -c Debug

# 2. Fetch the external tools (yt-dlp, FFmpeg) into src\Nexus.App\tools
pwsh .\scripts\fetch-tools.ps1

# 3. Run
dotnet run --project src\Nexus.App -c Debug
```

On first launch the **setup wizard** checks for the required tools, offers to download
yt-dlp for you, and lets you choose a theme and default download folder. You can re-run
these checks or set custom tool paths anytime under **Settings**.

> The class libraries (`Nexus.Core`, `Nexus.Infrastructure`, `Nexus.Tests`) are plain
> `net10.0` and build on any OS. `Nexus.App` targets `net10.0-windows` (WPF) and builds
> and runs only on Windows.

### About the external tools

Nexus does **not** bundle yt-dlp or FFmpeg in source control and never downloads them
from unofficial mirrors. `scripts/fetch-tools.ps1` retrieves them from their official
release channels and **verifies every download against the publisher's SHA-256
checksum** before installing it:

- **yt-dlp** — official [yt-dlp GitHub releases](https://github.com/yt-dlp/yt-dlp/releases)
- **FFmpeg / ffprobe** — the [gyan.dev](https://www.gyan.dev/ffmpeg/builds/) Windows
  builds listed on the official [ffmpeg.org download page](https://ffmpeg.org/download.html)

At runtime the app resolves each tool in this order: an explicit path from Settings, then
a `tools\` folder next to `Nexus.exe`, then the system `PATH`.

## Building a release

### Portable (self-contained folder + zip)

```powershell
pwsh .\scripts\publish.ps1
# -> dist\Nexus-0.1.0-win-x64\           (folder you can copy anywhere)
# -> dist\Nexus-0.1.0-win-x64-portable.zip
```

The published build is self-contained (bundles the .NET runtime) and includes a `tools\`
folder with the fetched binaries. Use `-SingleFile` for a single `Nexus.exe`, or
`-NoTools` to skip fetching the tools.

### Windows installer (Inno Setup)

```powershell
pwsh .\scripts\build-installer.ps1
# -> dist\Nexus-0.1.0-setup.exe
```

Requires the free [Inno Setup](https://jrsoftware.org/isdl.php) compiler (`ISCC.exe`).
The installer defaults to a per-user install (no admin prompt) and lets the user elevate
to an all-users install. The installer definition is
[`installer/Nexus.iss`](installer/Nexus.iss).

---

## Where your data lives

Everything is kept under `%LocalAppData%\Nexus`:

| Path                         | Contents                                   |
| ---------------------------- | ------------------------------------------ |
| `nexus.db`                   | SQLite database (history, favorites)       |
| `settings.json`              | Application settings                       |
| `Wallpapers\`                | Imported custom wallpapers                 |
| `ThumbnailCache\`            | Cached preview thumbnails                  |
| `logs\`                      | Rolling Serilog log files                  |

Downloads default to `Videos\Nexus` and are fully configurable in Settings, including a
templated file name (default `{title} [{id}].{ext}`).

## Themes & wallpaper

Four hand-tuned themes ship in the box: **Midnight**, **Aurora**, **Crimson** and
**Cyberpunk**. Themes are defined entirely with semantic brushes (no hard-coded colors in
views), so switching is instant and consistent. You can also set a custom wallpaper image
and tune its blur, darkness and opacity from Settings.

## Security & privacy

Nexus is built to treat all input as untrusted and to keep you in control:

- **No shell injection.** External tools are launched with
  `ProcessStartInfo.ArgumentList` — arguments are never concatenated into a command line
  or passed through `cmd.exe`/PowerShell.
- **Safe file handling.** Filenames are sanitized and output paths are validated to
  prevent path traversal outside the chosen download folder.
- **Verified downloads.** The tool fetcher uses TLS and checks SHA-256 checksums against
  the publisher's published hashes; a bad download can never replace a good binary.
- **No secrets in source, minimal logging.** Credentials, tokens and cookies are never
  written to logs.
- **No access-control bypass.** Nexus does not implement DRM circumvention or
  authentication/paywall bypassing.

See [`docs/architecture.md`](docs/architecture.md) for how these are enforced in code.

## Responsible use

Nexus is a general-purpose front-end for publicly available tooling. **You are
responsible for complying with the terms of service of the sites you use and with
applicable copyright law.** Only download content you have the right to download. Nexus
does not, and will not, help bypass DRM or access controls.

## Project layout

```
Nexus.sln
Directory.Build.props        Shared build settings (version, analyzers, nullable, ...)
Directory.Packages.props     Central package versions
global.json                  Pinned .NET SDK
src/
  Nexus.App/                 WPF UI (views, view-models, converters, themes, services)
  Nexus.Core/                Domain models, enums, DTOs, interfaces, exceptions
  Nexus.Infrastructure/      yt-dlp/FFmpeg, EF Core + SQLite, settings, themes, ...
  Nexus.Tests/               xUnit unit tests
scripts/
  fetch-tools.ps1            Download & verify yt-dlp / FFmpeg
  publish.ps1                Self-contained portable build + zip
  build-installer.ps1        Publish + compile the Inno Setup installer
installer/
  Nexus.iss                  Inno Setup script
docs/
  architecture.md            Layered design, patterns, data flow
  user-guide.md              End-user walkthrough
```

## Testing

```powershell
dotnet test
```

Unit tests cover the cross-platform Core/Infrastructure logic (argument building,
filename sanitization, mapping, queueing, persistence).

## License

Nexus is released under the [MIT License](LICENSE).

The external tools it invokes and its NuGet dependencies retain their own licenses; see
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md). In particular, **FFmpeg** is licensed
under the GPL/LGPL and **yt-dlp** is released into the public domain (Unlicense); Nexus
runs them as separate processes and does not link them into the application.

---

*Repository links in the app (About page) currently point at a `your-org` placeholder —
replace them in `src/Nexus.Core/Constants/AppLinks.cs` before publishing.*
