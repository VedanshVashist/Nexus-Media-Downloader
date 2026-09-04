# Nexus — Architecture

This document describes how Nexus is structured, the principles behind that structure,
and how a request flows through the system. It complements the code, which is the source
of truth.

## Goals & principles

1. **Separation of concerns via layering.** UI, application logic, domain, and
   integrations are distinct and depend inward only.
2. **Dependency inversion.** Cross-layer collaborators are expressed as interfaces owned
   by the domain (`Nexus.Core`); concrete implementations live further out.
3. **Testability.** Because behavior sits behind interfaces in cross-platform libraries,
   the important logic is unit-testable without a UI or a real network.
4. **Safety by default.** Untrusted input (URLs, paths, filenames, tool arguments,
   playlist/metadata strings) is validated and never passed through a shell.
5. **No hidden magic.** Composition is explicit and happens once, at startup.

## The layers

```
+-----------------------------------------------------------+
|  Nexus.App  (net10.0-windows, WPF)                        |
|  Views  -  ViewModels (MVVM)  -  Converters               |
|  UI-only services: dialogs, dispatcher, theme application |
+------------------------------ | ---------------------------+
                                | depends on
+------------------------------ v ---------------------------+
|  Nexus.Core  (net10.0, no WPF)                            |
|  Models - Enums - DTOs - Interfaces - Exceptions - Consts |
+------------------------------ ^ ---------------------------+
                                | implemented by
+------------------------------ | ---------------------------+
|  Nexus.Infrastructure  (net10.0)                          |
|  yt-dlp & FFmpeg process wrappers                         |
|  EF Core + SQLite persistence                             |
|  Settings, themes, thumbnails, dependency manager,        |
|  notifications, update check, logging setup               |
+-----------------------------------------------------------+
```

**The dependency rule:** arrows point inward. `Nexus.Core` references nothing but the
BCL — crucially, **never WPF**. `Nexus.App` and `Nexus.Infrastructure` both reference
`Nexus.Core`. `Nexus.App` references `Nexus.Infrastructure` only to wire up the DI
container at startup; view-models depend on Core interfaces, not concrete types.

### Nexus.Core

The heart of the domain, with no framework entanglements:

- **Models / DTOs** — media metadata, formats, download requests and progress, history
  and favorite records, settings.
- **Enums** — download state/type, theme, and similar closed sets.
- **Interfaces** — the contracts the app programs against: media extraction, download
  management, persistence/repositories, settings, themes, wallpaper, notifications,
  dependency resolution, the UI dispatcher, and so on.
- **Constants** — `AppConstants` (names, defaults), `AppPaths` (per-user data locations),
  `AppLinks` (official URLs). Centralizing these keeps behavior consistent and auditable.
- **Exceptions** — typed errors that carry enough context to produce friendly messages.

### Nexus.Infrastructure

Concrete implementations of the Core interfaces:

- **yt-dlp wrapper** — builds argument lists and parses yt-dlp's JSON/progress output.
- **FFmpeg/ffprobe wrapper** — probes and performs muxing/remuxing/conversion and
  embedding.
- **Download manager & queue** — schedules concurrent downloads, tracks progress, and
  supports pause/cancel/retry.
- **Persistence** — EF Core `DbContext` over SQLite for history and favorites, plus a
  JSON-backed settings store.
- **Support services** — theme catalog, thumbnail cache, dependency manager (tool
  resolution + updater), notifications, update check, and Serilog configuration.

### Nexus.App

The WPF presentation layer:

- **Views** — XAML for the shell (`MainWindow`), the first-run wizard (`FirstRunWindow`),
  and one `UserControl` per page (Home, Downloads, Queue, History, Favorites, Settings,
  About), plus the toast host.
- **ViewModels** — `MainViewModel` (shell/navigation), a `PageViewModel` base, one
  view-model per page, and item view-models (e.g. a download item). Built with
  `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`).
- **Converters** — value/multi-value converters that keep XAML declarative.
- **UI-only services** — things that legitimately need WPF (dialogs, the dispatcher
  wrapper, applying a theme's resource dictionary). These implement Core interfaces so
  view-models stay UI-agnostic.

## Composition & startup

Nexus uses the **.NET Generic Host**. At startup the app:

1. Ensures the per-user data directories exist (`AppPaths.EnsureCreated()`).
2. Configures **Serilog** (rolling file sink under `%LocalAppData%\Nexus\logs`, plus
   console/debug).
3. Registers services with `Microsoft.Extensions.DependencyInjection`: Core interfaces →
   Infrastructure/App implementations, the `DbContext`, all view-models, and the windows.
4. Runs database migrations/creation for the SQLite store.
5. Decides between the **first-run wizard** and the **main window** based on whether
   setup has completed, resolving each window (and its view-model graph) from the
   container.

`ShutdownMode` is explicit: closing the main window shuts the app down; closing the
wizard before completion declines setup and shuts down.

## MVVM specifics

- View-models never reference views; navigation is data-driven — `MainViewModel` exposes
  the current `PageViewModel`, and the shell hosts it in a `ContentControl` with
  `DataTemplate`s mapping view-model types to views.
- The active nav item is highlighted by comparing each item's `NavigationKey` to the
  current page's key (a multi-value equality converter) rather than sharing a selection,
  which avoids selection-coercion pitfalls across separate lists.
- Long-running work is asynchronous and marshaled back to the UI thread through an
  `IUiDispatcher` abstraction, so view-models remain unit-testable.

## Theming

Themes are resource dictionaries of **semantic brushes** (e.g. `Brush.Accent`,
`Brush.Text.Primary`, `Brush.Bg.Surface`). Views bind these with `DynamicResource`, and
switching themes swaps the merged dictionary at runtime. There are **no hard-coded colors
in views** — the sole deliberate exception is the Windows-convention red close-button
hover. Sizing/typography tokens (radii, font sizes, font families) and control styles
live in a shared dictionary and are referenced with `StaticResource`.

## Data flow: analyzing and downloading a link

```
User pastes URL
      |
      v
HomeViewModel.Analyze()  --(IMediaExtractor)-->  yt-dlp (ArgumentList) --> JSON
      |                                                   |
      |  <-------------------- parsed metadata -----------+
      v
UI shows title/thumbnail/formats; user picks options
      |
      v
Build DownloadRequest  -->  IDownloadManager.Enqueue()
      |
      v
Queue schedules (<= max concurrent)  -->  yt-dlp downloads, reporting progress
      |                                             |
      |                                   FFmpeg mux/convert/embed (if needed)
      v                                             |
Progress/speed/ETA stream to the UI  <-------------+
      |
      v
On success: file written to the configured folder (sanitized, templated name),
history recorded via the persistence layer.
```

Pause/cancel are cooperative: the manager signals the running process/token, and retry
re-enqueues with the same request (bounded by the configured retry count).

## Persistence

- **SQLite** via EF Core stores history and favorites. The database lives at
  `%LocalAppData%\Nexus\nexus.db`.
- **Settings** are stored as JSON (`settings.json`) so they are easy to inspect and are
  independent of schema migrations.
- Thumbnails are cached on disk under `ThumbnailCache\` and referenced by the UI through
  a path-to-image converter that decodes at a bounded size.

## Dependency (tool) management

`DependencyManager` resolves each external tool by probing, in order:

1. an explicit path configured in **Settings**,
2. a `tools\` folder next to the executable (`AppContext.BaseDirectory`),
3. the system `PATH`.

It can also update yt-dlp in place: it downloads the official Windows binary to a
temporary file and atomically moves it over `tools\yt-dlp.exe`, reporting progress. The
first-run wizard surfaces the tool status and offers the download.

## Security enforcement points

| Concern | Where it's handled | How |
| --- | --- | --- |
| Command injection | Process wrappers | `ProcessStartInfo.ArgumentList`; never `cmd.exe`/PowerShell, never a concatenated command line |
| Path traversal | Output path building | Validate/normalize destinations; reject paths escaping the chosen folder |
| Unsafe filenames | Filename sanitization | Strip/replace invalid and reserved characters before writing |
| Untrusted downloads | `scripts/fetch-tools.ps1` + updater | TLS; SHA-256 verification against publisher hashes; atomic replace |
| Secret leakage | Logging | Never log credentials/tokens/cookies; friendly messages omit sensitive detail |
| Access-control bypass | Product scope | No DRM circumvention or authentication/paywall bypass is implemented |

## Packaging

- `scripts/publish.ps1` produces a **self-contained** win-x64 build (bundled runtime),
  fetches the verified tools into `tools\`, and zips a portable archive.
- `scripts/build-installer.ps1` + `installer/Nexus.iss` compile a per-user Inno Setup
  installer that lays down the same output.
- The app project copies any present `tools\**` into the build/publish output
  (`CopyToOutputDirectory`/`CopyToPublishDirectory`) so development runs and releases both
  resolve tools from beside the executable. The binaries themselves are gitignored.

## Testing strategy

Unit tests target the cross-platform layers: argument construction, filename
sanitization, metadata mapping, queue/concurrency behavior, and persistence. The WPF
layer is kept thin and declarative so most logic is exercised without a UI.
