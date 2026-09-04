# Third-Party Notices

Nexus (the "Software") is distributed under the MIT License (see `LICENSE`). It relies on
external command-line programs and third-party libraries that retain their own licenses.
This document summarizes them. Each project's authoritative license text is available
from its official source; the summaries below are informational.

---

## External tools (invoked as separate processes)

Nexus launches these programs as independent child processes. It does **not** statically
or dynamically link them into the application, and it does not modify or redistribute
their source. When you build a release with `scripts/publish.ps1`, the tools are fetched
from their official channels and placed in a `tools\` folder next to the executable.

### yt-dlp

- Project: https://github.com/yt-dlp/yt-dlp
- License: **The Unlicense** (public domain dedication).
- Role in Nexus: media/metadata extraction and download.

### FFmpeg (ffmpeg.exe, ffprobe.exe)

- Project: https://ffmpeg.org/
- Windows builds used by `scripts/fetch-tools.ps1`: https://www.gyan.dev/ffmpeg/builds/
  (a build provider listed on the official FFmpeg download page).
- License: FFmpeg is licensed under the **GNU Lesser General Public License (LGPL)
  version 2.1 or later**. Some builds — including typical Windows "essentials" builds —
  incorporate components licensed under the **GNU General Public License (GPL)** and are
  therefore distributed as a whole under the GPL. Consult the specific build's
  documentation for its exact license and enabled components.
- Role in Nexus: muxing/remuxing, audio extraction, format conversion, and embedding
  thumbnails/subtitles/chapters/metadata.
- Source availability: FFmpeg source corresponding to any distributed build is available
  from https://ffmpeg.org/ and from the build provider.

> Because Nexus communicates with FFmpeg only through its command-line interface and
> bundles it as an unmodified, separate executable, the two remain independent works. If
> you redistribute Nexus together with an FFmpeg build, comply with that build's license
> (LGPL/GPL), including providing corresponding source availability where required.

---

## Bundled libraries (NuGet)

These packages are referenced by the projects and redistributed as part of the
application's binaries. Versions are pinned centrally in `Directory.Packages.props`.

| Package | License |
| --- | --- |
| CommunityToolkit.Mvvm | MIT |
| Microsoft.Extensions.DependencyInjection (+ .Abstractions) | MIT |
| Microsoft.Extensions.Hosting (+ .Abstractions) | MIT |
| Microsoft.Extensions.Http | MIT |
| Microsoft.Extensions.Options | MIT |
| Microsoft.Extensions.Logging.Abstractions | MIT |
| Microsoft.EntityFrameworkCore (incl. Sqlite, Design) | MIT |
| SQLitePCLRaw (core / bundle_e_sqlite3 / lib / provider) | Apache-2.0 |
| SQLite (native engine, via SQLitePCLRaw) | Public Domain |
| Serilog | Apache-2.0 |
| Serilog.Extensions.Hosting | Apache-2.0 |
| Serilog.Sinks.File / .Console / .Debug | Apache-2.0 |

### Test-only dependencies (not shipped)

| Package | License |
| --- | --- |
| xunit / xunit.runner.visualstudio | Apache-2.0 |
| Microsoft.NET.Test.Sdk | MIT |
| Microsoft.EntityFrameworkCore.InMemory | MIT |
| NSubstitute | BSD-3-Clause |
| FluentAssertions (6.12.2) | Apache-2.0 |
| coverlet.collector | MIT |

---

## Fonts and icons

Nexus renders its UI glyphs using the **Segoe Fluent Icons** / **Segoe MDL2 Assets**
symbol fonts that ship with Windows 10/11. These fonts are provided by the operating
system and are not redistributed with Nexus.

---

*If you find an omission or error in these notices, please open an issue.*
