# Nexus — User Guide

A quick tour of everything you can do in Nexus. If you're building from source, see the
[README](../README.md) first.

## First launch: the setup wizard

The very first time you open Nexus, a short wizard appears:

1. **Welcome** — a quick hello.
2. **Required tools** — Nexus checks for **yt-dlp** (fetches media) and **FFmpeg** (merges
   and converts it). If yt-dlp is missing, click **Download yt-dlp** to install it
   automatically from its official release. Use **Re-check** after installing tools
   yourself. You can also proceed and set tool paths later in Settings.
3. **Make it yours** — choose a **theme** and your default **download folder**.
4. **All set** — finish and start using Nexus.

You can skip setup at any point; nothing is required to reach the main window, though you
won't be able to download until the tools are resolvable.

## The main window

- A **sidebar** on the left navigates between pages. It collapses to icons in compact
  mode.
- The **title bar** shows the app name and, when downloads are running, an active-download
  count.
- **Toasts** appear briefly in the bottom-right to confirm actions or surface errors.

### Pages

| Page | What it's for |
| --- | --- |
| **Home** | Paste and analyze a link, then choose what to download. |
| **Downloads** | Watch in-progress and completed downloads with progress, speed and ETA. |
| **Queue** | See and manage what's waiting or running; reorder/pause/cancel. |
| **History** | Browse and search everything you've downloaded. |
| **Favorites** | Quick access to links you save for reuse. |
| **Settings** | Tools, folders, naming, concurrency, appearance and more. |
| **About** | Version and project links. |

## Downloading

1. Go to **Home** and paste a URL (or several — batch input and playlists are supported).
2. Click **Analyze**. Nexus shows the title, a thumbnail preview, and the available
   formats.
3. Choose what you want:
   - **Video** at a specific quality/container, or
   - **Audio only** (extracted/converted), or
   - **Extras**: thumbnail, subtitles, chapters, metadata.
   - Optionally **embed** thumbnail/subtitles/chapters/metadata into the output file.
4. Start the download. It moves into the **Queue** and appears on the **Downloads** page.

### Managing the queue

- Nexus runs several downloads at once (**3 by default**, configurable up to **10**).
  Extra items wait in the queue and start automatically as slots free up.
- Each item supports **pause**, **cancel** and **retry**. Failed items can be retried
  without re-analyzing.

## History & favorites

- **History** records completed downloads so you can find, re-open, or re-download them.
  Use the search box to filter.
- **Favorites** keeps links you use often a click away.

## Settings

- **Tools** — set explicit paths to `yt-dlp.exe`, `ffmpeg.exe` and `ffprobe.exe`, or let
  Nexus resolve them from the bundled `tools\` folder or your system `PATH`. You can also
  update yt-dlp from here.
- **Downloads** — default download folder, optional per-type subfolders, and the file
  **name template** (default `{title} [{id}].{ext}`).
- **Queue** — maximum concurrent downloads and retry count.
- **Appearance** — pick a theme (**Midnight**, **Aurora**, **Crimson**, **Cyberpunk**),
  set a **custom wallpaper**, and tune its blur, darkness and opacity.

### File name templates

The name template controls how output files are named. Common fields include the title,
id and extension (for example `{title} [{id}].{ext}`). Whatever you choose, Nexus
**sanitizes** the final name and keeps files inside your chosen download folder.

## Where your files and data live

- **Downloads** go to the folder you set (default `Videos\Nexus`).
- **App data** is under `%LocalAppData%\Nexus`:
  - `nexus.db` — history & favorites
  - `settings.json` — your settings
  - `Wallpapers\` — imported wallpapers
  - `ThumbnailCache\` — cached previews
  - `logs\` — log files (useful for troubleshooting)

## Troubleshooting

**"yt-dlp/FFmpeg not found."**
Open **Settings → Tools** and either set explicit paths or use the wizard's download
button. Confirm `tools\yt-dlp.exe` (and `ffmpeg.exe`/`ffprobe.exe`) exist next to
`Nexus.exe`, or that the tools are on your `PATH`.

**A download fails or a format is unavailable.**
Some sites change frequently. Update yt-dlp (Settings → Tools) — most extraction issues
are fixed by shipping the latest yt-dlp. Then retry the item.

**Merging/conversion fails.**
Ensure FFmpeg and ffprobe are present and resolvable; embedding and audio extraction rely
on them.

**Something looks wrong — where are the logs?**
Check `%LocalAppData%\Nexus\logs`. Logs never contain credentials, tokens or cookies.

## Uninstalling

- **Installer build:** uninstall from Windows *Apps & features* (or the Start-menu
  *Uninstall Nexus* entry).
- **Portable build:** just delete the folder.
- To remove your data too, delete `%LocalAppData%\Nexus`.

## A note on responsible use

Only download content you have the right to download, and respect the terms of service of
the sites you use. Nexus does not bypass DRM or access controls.
