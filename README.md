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
│    ├─ writes *.html siblings + 02-datamodel.html      │
│    ├─ writes per-exercise bundle.json (all six        │
│    │    runtime inputs in one fetchable blob)         │
│    └─ renders top-level repo docs (SECURITY,          │
│         KNOWN_ISSUES, AUTHORING_LESSONS) into         │
│         wwwroot/reference/ for the About + Contribute │
│         pages                                         │
│  Triggered by a BuildContent MSBuild target           │
└──────────────────────────┬────────────────────────────┘
                           │ pre-rendered .html + bundle.json
                           ▼
┌──────────────── Runtime (browser) ────────────────────┐
│  App.razor → <Router>                                 │
│      ├─ "/"                  → 000_home               │
│      ├─ "/about"             → 001_about              │
│      ├─ "/playground"        → 002_playground         │
│      ├─ "/lesson/{LessonId}" → 010_lesson             │
│      └─ "/contribute"        → 999_contribute-a-lesson│
│                                                       │
│  Singletons                                           │
│    ├─ ContentService  — manifest + lazy lesson load   │
│    │                  + reference-doc fetch           │
│    ├─ PageOrder       — auto prev/next from page      │
│    │                    file-name prefixes + manifest │
│    ├─ ProgressService — localStorage + in-mem mirror  │
│    └─ ThemeService    — light / dark, persisted       │
│                                                       │
│  ExerciseBlock + Playground                           │
│    ├─ CodeMirror 6 editor (Scriban / JSON grammars)   │
│    ├─ ScribanRunner (LoopLimit + 250 KB output cap)   │
│    ├─ DiffView (DiffPlex) on fail                     │
│    └─ Show solution / Reset buttons                   │
└───────────────────────────────────────────────────────┘
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the detailed map —
services, components, file layout, and where each piece lives.

## Tests

```powershell
dotnet test
```

xUnit project under `tests/ScribanTutorial.Tests/`. Six test classes (83 cases — most are the per-exercise solution checks):

- `ContentNormalizeTests` — CRLF / trailing-newline normalisation.
- `JsonToScribanTests` — JSON → ScriptObject conversion (incl. the int-vs-float fix).
- `ScribanRunnerTests` — render path, parse errors, JSON-error friendly message, the 250 KB output cap.
- `ExerciseSolutionTests` — data-driven from the manifest; every exercise's canonical solution runs against its data model and is compared to expected. Add an exercise → it gets a test for free.
- `ContentBuilderTests` — MarkdownRenderer's `:::example` blocks emit the right three-panel layout; the link rewriter resolves repo-relative hrefs to GitHub blob URLs; the sanitiser strips `<script>`, `on*=`, `javascript:`, `<iframe>`; per-edge grammar regression locks; TextMateHighlighter colours a known Scriban snippet correctly.
- `BuildTargetTest` — every lesson `.md` has a fresh `.html` sibling; every exercise has a fresh `bundle.json`; every reference doc rendered into `wwwroot/reference/`. Catches "BuildContent MSBuild target stopped running" without a full publish.

CI gates the deploy on `dotnet test` going green.

## Contributing

- **Lesson, exercise, or theory edits** — open the rendered
  [Contribute a lesson](https://sergeiosipov.github.io/scriban-tutorial/contribute)
  page on the live site. No dev setup required; the page walks you
  through the GitHub web-UI flow.
- **App code, build, CI, or infrastructure** — see
  [`CONTRIBUTING.md`](CONTRIBUTING.md) for prerequisites, the test
  suite, the build pipeline, and project mechanics.

## Documentation

- [`CONTRIBUTING.md`](CONTRIBUTING.md) — developer onboarding: prerequisites, test suite, build pipeline, project mechanics, PR flow.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — current-state map of services, components, build pipeline, and asset layout. Skim this first for non-trivial changes.
- [`docs/AUTHORING_LESSONS.md`](docs/AUTHORING_LESSONS.md) — source of the rendered Contribute page; the full lesson-authoring reference.
- [`docs/SECURITY.md`](docs/SECURITY.md) — threat model for running user-supplied templates in the browser (also rendered on the [About page](https://sergeiosipov.github.io/scriban-tutorial/about)).
- [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md) — GitHub Pages deployment, base href, SPA routing.
- [`KNOWN_ISSUES.md`](KNOWN_ISSUES.md) — Scriban TextMate grammar edge cases and course-coverage gaps (also rendered on the [About page](https://sergeiosipov.github.io/scriban-tutorial/about)).
