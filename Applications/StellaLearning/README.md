# Stella Learning

Modern, cross‑platform (Avalonia) learning & knowledge consolidation app focused on frictionless capture, spaced repetition with FSRS, automatic metadata generation (AI + parsing), Obsidian vault synchronization, and rich progress analytics.

---
## Vision / Goal
Help you move from raw notes, files, web pages, PDFs, and images to structured, reviewable knowledge with the smallest possible manual overhead. The app tries to:
1. Ingest & normalize heterogeneous sources (local files, Obsidian notes, web URLs, PDFs, images, executables, media).
2. Enrich them (AI generated titles, tags, summaries; front‑matter parsing; image tagging; web extraction).
3. Let you transform content into multiple spaced repetition item types (flashcards, cloze, quizzes, image cloze, files, exercises, etc.).
4. Schedule reviews using FSRS (Free Spaced Repetition Scheduler) with transparent state fields and statistics.
5. Track deep study analytics (streaks, per‑tag accuracy, per‑type performance, daily stats, review forecasts).

---
## Core Features

### Spaced Repetition System (FSRS powered)
- Native integration of `FsrsSharp.Scheduler` (no Python bridge) via a global `SchedulerService`.
- Item state fields: stability, difficulty, step, last & next review, state (New / Learning / Review / Relearning), priority.
- Multiple item types: Cloze, ImageCloze (areas on images), Flashcard, Quiz (MCQ), Text, Exercise, File, PDF, Video, Audio, Executable placeholder.
- Priority aware next‑item selection with controlled randomization among top candidates to avoid pattern priming.
- Review forecast (rolling window) and future due item inspection.

### Rich Item Metadata & AI Assistance
- `LargeLanguageManager` abstraction for LLM usage (Gemini models via `Google_GenerativeAI`).
- AI generated: Title, Author, Publisher, Tags, Summary for files / URLs.
- Automatic text extraction & cleaning for web pages (HTML parsing via HtmlAgilityPack, script/style/nav removal, whitespace normalization).
- File support pipeline: direct file API (code/doc/pdf/etc.) + fallback conversion to temporary `.txt` for convertible formats (md/log/xml/json/yaml/js/css ...).
- Image tag generation (semantic tags for categorization).
- Guardrails: features gated by `Settings.EnableLargeLanguageFeatures`.

### Obsidian Vault Synchronization
- Add one or more Obsidian vault paths: automatic discovery & tracking of `.md` file lifecycle.
- `ObsidianVaultWatcher` uses `FileSystemWatcher` (recursive) to:
	- Add new markdown files as literature sources (parsing YAML front‑matter: tags, aliases, created date).
	- Update metadata on rename (including re‑parsing front‑matter).
	- Remove items when files are deleted or converted away from `.md`.
- Toggle sync globally (`SyncObsidianVaults`). Safe disposal / restart logic built into settings.

### Literature & Source Handling
- Local file sources (`LocalFileSourceItem`) + web sources + potential external types.
- Tag system with query extensions (`TagQueryExtensions`) & per‑tag performance stats.
- Aliases, publication year extraction (front‑matter), auto title fallback.

### Statistics & Analytics
- Central singleton `StatsTracker` with autosave loop (5‑minute interval, JSON persisted in per‑user AppData folder; debug vs release file names).
- Tracks: sessions, daily stats, streaks (current/longest), total study time (seconds), total reviews, overall accuracy (clamped 0‑100), per type & per tag accuracy.
- Automatic rebuild & filtering when items are deleted or tags change (retroactive tag propagation supported).
- Most difficult items list, study time by weekday, review forecast generation.
- Robust recalculation pipeline & defensive filtering for stale item IDs on load.

### Theming & UX
- Light/Dark mode toggle with immediate Avalonia theme application + event hook (`DarkModeChanged`).
- Close‑to‑tray option; always‑on‑top toggle.
- Notifications (platform‑conditional, Windows support hints via manifest & conditional TFMs).

### Persistence & Safety
- Settings stored as JSON under user AppData (`StellaLearning/settings.json`).
- Spaced repetition items saved separately (`./save/space_repetition_items.json`).
- Graceful error handling & logging via NLog.
- Controlled serialization (ignores runtime watchers, uses nullable + defensive null recreation for FSRS Card).

### Extensibility Architecture
- Uses Flecs ECS (`Flecs.NET`) for potential runtime systems/entities integration.
- Compiled bindings default (`AvaloniaUseCompiledBindingsByDefault`) for performance.
- CommunityToolkit.Mvvm source generators for observable state & partial property hooks.
- Modular Util layer (smart URL opener, PDF metadata extraction stub, executable & file pickers, note handlers).

### Large Language Model Strategy
- Client‑side centric (keeps flexibility for local models in future; no enforced backend dependency for enrichment).
- Environment variable `GEMINI_API` required for Gemini features.
- Temporary file sanitation & remote file cleanup after LLM calls.

### Cross‑Platform
- Targets .NET 9; conditionally uses `net9.0-windows10.0.17763.0` on Windows for Windows‑specific features (notifications, etc.).
- Avalonia + Fluent theme + Inter font for consistent UI across Linux, Windows, macOS.

---
## Quick Start

### Prerequisites
- .NET 9 SDK installed.
- (Optional) Gemini API Key exported as environment variable:
	- Linux/macOS: `export GEMINI_API=YOUR_KEY`  
	- Windows (Powershell): `$Env:GEMINI_API = "YOUR_KEY"`

### Build & Run
```bash
dotnet restore
dotnet run --project AyanamisTower.StellaLearning.csproj
```

### First Launch Tips
- Configure Obsidian vault paths in Settings to auto‑ingest notes.
- Toggle Dark Mode if preferred.
- Add a few items (flashcard, cloze, image cloze) and start a study session.
- If AI features enabled, try importing a PDF or pasting a URL for automatic metadata.

---
## Environment & Configuration
| Setting / Variable | Purpose |
|--------------------|---------|
| `GEMINI_API` | API key used by `LargeLanguageManager` for text / image enrichment. |
| `EnableLargeLanguageFeatures` | Runtime toggle (in `Settings`) gating all AI calls. |
| `SyncObsidianVaults` | Turn Obsidian file watchers on/off globally. |
| `EnableCloudSaves` | Placeholder for future remote sync. |
| `EnableNotifications` | Allow desktop notifications. |
| `CloseToTray` | Minimize app to tray instead of quitting. |

---
## Data & Storage Layout
| Path | Description |
|------|-------------|
| `%APPDATA%/StellaLearning/settings.json` (Win) / `$HOME/.config/...` (Linux mapped) | User settings |
| `%APPDATA%/StellaLearning/learning_stats.json` | Study statistics (release) |
| `%APPDATA%/StellaLearning/learning_stats_debug.json` | Stats in debug builds |
| `./save/space_repetition_items.json` | Local spaced repetition items |

---
## Roadmap (Indicative)
- Local LLM backend adapters (Ollama / llama.cpp binding) to complement Gemini.
- Fine‑grained AI permission matrix (title only vs full metadata / content processing).
- More granular scheduling controls (custom FSRS parameters UI).
- Deck/grouping system & filtered review queues (per tag, per source type).
- Import/export bundles (portable learning sets).
- End‑to‑end encryption for any future cloud sync layer.

---
## Development Notes
- Treat warnings as errors (`TreatWarningsAsErrors=true`) and preview analyzers enabled.
- Generated documentation file enabled—public APIs should stay comment‑complete.
- Logging via NLog; configure `NLog.config` for sinks/levels.
- When extending spaced repetition item types, ensure FSRS card reconstruction logic remains consistent.

### Scripts & Hot Reload Commentary
While lightweight scripting/hot‑reload ideas were explored, current C# scripting tooling (diagnostics, type discovery, debugging) is unreliable. Keeping the concept documented may encourage future improvement, but the core app favors compiled workflows for now.

---
## License
GPLv3 (See header in source files). You may redistribute and modify under the terms of the GPLv3 or later.

---
## Contributing
PRs welcome. Please:
1. Keep feature scope small & focused.
2. Add/maintain XML docs on public members.
3. Avoid introducing blocking LLM dependencies (keep opt‑in model).
4. Test cross‑platform UI impacts (Avalonia) before merging.

---
## Attribution / Tech Stack
- Avalonia UI + FluentAvaloniaUI
- CommunityToolkit.Mvvm
- FsrsSharp (FSRS scheduling algorithm)
- Google Generative AI SDK (`Google_GenerativeAI`)
- HtmlAgilityPack (web extraction)
- PDFsharp (PDF metadata / future parsing hooks)
- Flecs.NET (ECS patterns)
- NLog (structured logging)

---
## Short Pitch
Capture everything. Enrich automatically. Review intelligently. Track progress sustainably.

---

