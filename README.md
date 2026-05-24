# Scriban Interactive Training

A browser-based, client-side training course for the
[Scriban](https://github.com/scriban/scriban) templating language. Pure Blazor
WebAssembly — no backend, no database, no accounts. The Scriban engine
evaluates the user's templates directly in the browser tab. Course content is
plain `.md` / `.txt` / `.json` under `wwwroot/lessons/`; non-developers can add
lessons by editing files only.

[![Deploy to GitHub Pages](https://github.com/sergeiosipov/scriban-tutorial/actions/workflows/deploy.yml/badge.svg)](https://github.com/sergeiosipov/scriban-tutorial/actions/workflows/deploy.yml)
&nbsp;
[Live site](https://sergeiosipov.github.io/scriban-tutorial/)

## Prerequisites

- Windows 11 (PowerShell 7+) or any OS with .NET 10
- .NET 10.0.x SDK — `winget install Microsoft.DotNet.SDK.10` on Windows
- `wasm-tools` workload — `dotnet workload install wasm-tools`

No Node, npm, or external bundler needed. CodeMirror 6 is vendored under
`src/ScribanTutorial/wwwroot/lib/codemirror/`.

## Run it locally

```powershell
cd src\ScribanTutorial
dotnet run
```

The dev server prints a `http://localhost:<port>` URL. Open it in Edge / Chrome
/ Firefox. The boot shell renders within ~100 ms, and Blazor takes over.

A full `dotnet build ScribanTutorial.slnx` from the repo root also works and is
what CI runs.

## How it's wired

```
┌────────────── Build time (dotnet build) ──────────────┐
│  tools/ContentBuilder/                                │
│    ├─ scans wwwroot/lessons/**/*.md                   │
│    ├─ Markdig + custom :::example renderer            │
│    ├─ TextMateSharp colours fenced code blocks        │
│    └─ writes *.html siblings (gitignored)             │
│  Triggered by a BuildContent MSBuild target           │
└──────────────────────────┬────────────────────────────┘
                           │ pre-rendered .html
                           ▼
┌──────────────── Runtime (browser) ────────────────────┐
│  App.razor → <Router>                                 │
│      ├─ "/"                  → Home (course index)    │
│      └─ "/lesson/{LessonId}" → LessonPage             │
│                                                       │
│  Singletons                                           │
│    ├─ ContentService  — manifest + lazy lesson load   │
│    ├─ ProgressService — localStorage via JS interop   │
│    ├─ ThemeService    — light / dark, persisted       │
│    └─ TemplateCache   — pre-parsed Scriban templates  │
│                                                       │
│  ExerciseBlock                                        │
│    ├─ CodeMirror 6 editor (Scriban stream parser)     │
│    ├─ Scriban runner (LoopLimit + RecursiveLimit)     │
│    ├─ DiffPlex diff view on fail                      │
│    └─ Show solution / Reset buttons                   │
└───────────────────────────────────────────────────────┘
```

## Documentation

- [`docs/AUTHORING_LESSONS.md`](docs/AUTHORING_LESSONS.md) — how non-developers add or edit a lesson.
- [`docs/SECURITY.md`](docs/SECURITY.md) — threat model for running user-supplied templates in the browser.
- [`docs/SCRIBAN_BEST_PRACTICES.md`](docs/SCRIBAN_BEST_PRACTICES.md) — the patterns lesson content should teach.
- [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md) — GitHub Pages deployment, base href, SPA routing.

## Specification

The full build specification (annotated with where the implementation deviated
and why) lives at [`SPECIFICATION.md`](SPECIFICATION.md). Look there before
making non-trivial changes.
