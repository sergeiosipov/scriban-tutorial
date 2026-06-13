# Scriban Interactive Training

A browser-based, client-side training course for the
[Scriban](https://github.com/scriban/scriban) templating language. Pure Blazor
WebAssembly — no backend, no database, no accounts. The Scriban engine
evaluates the user's templates directly in the browser tab. Course content is
plain `.md` / `.txt` / `.json` under `wwwroot/lessons/`; non-developers can add
lessons by editing files only.

[![Deploy to GitHub Pages](https://github.com/sergeiosipov/scriban-tutorial/actions/workflows/deploy.yml/badge.svg)](https://github.com/sergeiosipov/scriban-tutorial/actions/workflows/deploy.yml) 
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/sergeiosipov/scriban-tutorial)  
[Live site](https://sergeiosipov.github.io/scriban-tutorial/)  


## Prerequisites

- Windows 11 (PowerShell 7+) or any OS with .NET 10
- .NET 10.0.x SDK — `winget install Microsoft.DotNet.SDK.10` on Windows
- `wasm-tools` workload — `dotnet workload install wasm-tools`

No Node, npm, or bundler needed to build or run. CodeMirror 6 is vendored
under `src/ScribanTutorial/wwwroot/lib/codemirror/`, and the editor ships as
a single pre-bundled module (`wwwroot/js/editor.bundle.min.js`) committed to
the repo — re-bundling is only needed when the editor sources change, via a
standalone esbuild binary (procedure in `wwwroot/lib/codemirror/VERSION.txt`).

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
│    ├─ prunes generated files whose sources are gone   │
│    ├─ scans wwwroot/lessons/**/*.md                   │
│    ├─ Markdig + custom :::example renderer            │
│    ├─ TextMateSharp colours fenced code blocks        │
│    ├─ writes *.html siblings + 02-datamodel.html      │
│    ├─ writes 01-theory.toc.json (h2/h3 outline)       │
│    ├─ writes per-exercise bundle.json (all runtime    │
│    │    inputs + hidden-case outputs derived from     │
│    │    the solution, in one fetchable blob)          │
│    └─ writes search-index.json + reference.json       │
│         + sitemap.xml (all under wwwroot/)            │
│  Triggered by a BuildContent MSBuild target           │
└──────────────────────────┬────────────────────────────┘
                           │ pre-rendered .html + bundle/index sidecars
                           ▼
┌──────────────── Runtime (browser) ────────────────────┐
│  App.razor → <Router>                                 │
│      ├─ "/"                  → 000_home               │
│      ├─ "/about"             → 001_about              │
│      ├─ "/playground"        → 002_playground         │
│      ├─ "/search"            → 003_search             │
│      ├─ "/reference"         → 004_reference          │
│      ├─ "/lesson/{LessonId}" → 010_lesson             │
│      └─ "/contribute"        → 999_contribute-a-lesson│
│                                                       │
│  Singletons                                           │
│    ├─ ContentService   — manifest + lazy lesson load  │
│    ├─ SearchService    — search-index.json → /search  │
│    ├─ ReferenceService — reference.json → /reference  │
│    ├─ PageOrder        — auto prev/next from page     │
│    │                     file-name prefixes + manifest│
│    ├─ ProgressService  — localStorage + in-mem mirror │
│    └─ ThemeService     — light / dark, persisted      │
│                                                       │
│  ExerciseBlock + Playground                           │
│    ├─ CodeMirror 6 editor — one pre-bundled module,   │
│    │    mounted lazily, read at submit time           │
│    ├─ ScribanRunner (LoopLimit + in-flight 250 KB     │
│    │    output cap + 2 s render budget)               │
│    ├─ Submit runs visible check + every hidden case   │
│    ├─ DiffView (lazy-loaded DiffPlex) on fail         │
│    └─ Show solution / Reset buttons                   │
└───────────────────────────────────────────────────────┘
```

Boot and caching, briefly: `index.html` uses fingerprint placeholders the
build rewrites to hashed asset names, and `js/boot.js` starts Blazor with a
`loadBootResource` that fetches the `.br` precompressed assets and decodes
them with a vendored Brotli decoder (dev builds fall back automatically). A
hand-rolled service worker plus `site.webmanifest` make the site installable
and offline-capable. Details in [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md).

Publish ships only generated content (`.html`, `bundle.json`, `toc.json`,
indexes) plus the editor bundle — lesson source files (`.md` / `.txt` /
`02-datamodel.json` / `06-cases.json`) and the unbundled editor sources stay
repo-only.

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the detailed map —
services, components, file layout, and where each piece lives.

## Tests

```powershell
dotnet test
```

xUnit project under `tests/ScribanTutorial.Tests/`. Nine test classes (476 cases — most are the per-exercise and per-example solution checks):

- `ContentNormalizeTests` — CRLF / trailing-newline normalisation.
- `JsonToScribanTests` — JSON → ScriptObject conversion (incl. the int-vs-float fix).
- `ScribanRunnerTests` — render path, parse errors, JSON-error friendly message, the in-flight 250 KB output cap (runaway templates are stopped mid-render).
- `ExerciseSolutionTests` — data-driven from the manifest; every exercise's canonical solution runs against its data model and is compared to expected, and every hidden validation case in an optional `06-cases.json` must render cleanly too. Add an exercise → it gets a test for free.
- `ExampleSolutionTests` — data-driven from the theory files; every `:::example` with an Output panel is re-rendered and compared. Add an example → same deal.
- `ContentBuilderTests` — MarkdownRenderer's `:::example` blocks emit the right three-panel layout plus the "Try in playground" link; theory headings get GitHub-style ids that survive the sanitiser; the sanitiser strips `<script>`, `on*=`, `javascript:`, `<iframe>`; per-edge grammar regression locks; TextMateHighlighter colours a known Scriban snippet correctly.
- `SearchIndexQueryTests` — the pure search ranking: AND across terms, hits inside solution code, title boosts, snippet/highlight correctness.
- `ReferenceIndexBuilderTests` — the built-in tables in lessons 10–17 parse into the right function / property / specifier entries, with section slugs matching Markdig's GitHub auto-identifiers.
- `BuildTargetTest` — every lesson `.md` has a fresh `.html` sibling and `01-theory.toc.json` sidecar; every exercise has a fresh `bundle.json` (an exercise's optional `06-cases.json` counts as a staleness source); `search-index.json`, `reference.json`, and `sitemap.xml` are no staler than their sources. Catches "BuildContent MSBuild target stopped running" without a full publish.

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
- [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md) — GitHub Pages deployment, base href, SPA routing.

Two more references live as rendered pages on the live site rather than
standalone `.md` files — they're contributor-facing reading, not
standalone docs:

- **Authoring lessons + non-programmer walkthrough** → [Contribute a lesson](https://sergeiosipov.github.io/scriban-tutorial/contribute).
- **Security threat model + known issues + course coverage** → [About](https://sergeiosipov.github.io/scriban-tutorial/about).
