# Contributing

Two kinds of contributions land in this repo, and they each have their own
entry point.

| You want to… | Read this |
|---|---|
| Add or edit a **lesson, exercise, or theory text** | The non-programmer-friendly walkthrough at <https://sergeiosipov.github.io/scriban-tutorial/contribute> — no dev setup required. |
| Touch **C#, JS, build, CI, or infrastructure** | Continue with this file. |

Most lesson-content edits don't need anything below this line. Everything
in this file is for changes to the app itself — the WASM project, the
build-time tools, the test suite, the deployment pipeline.

- [Prerequisites](#prerequisites)
- [Running locally](#running-locally)
- [Repository layout](#repository-layout)
- [Build pipeline](#build-pipeline)
- [Test suite](#test-suite)
- [Project mechanics](#project-mechanics)
- [Code conventions](#code-conventions)
- [Submitting a pull request](#submitting-a-pull-request)
- [Security disclosures](#security-disclosures)
- [Where to read next](#where-to-read-next)

---

## Prerequisites

- Windows 11 (PowerShell 7+) or any OS with .NET 10.
- .NET 10.0.x SDK — `winget install Microsoft.DotNet.SDK.10` on Windows.
- `wasm-tools` workload — `dotnet workload install wasm-tools`.

No Node, npm, or bundler needed to build or run. CodeMirror 6 is vendored
under [`src/ScribanTutorial/wwwroot/lib/codemirror/`](src/ScribanTutorial/wwwroot/lib/codemirror/)
and the editor ships pre-bundled (see [CodeMirror vendoring](#codemirror-vendoring-and-the-editor-bundle));
re-bundling is only needed when editor sources change and uses a standalone
esbuild binary, no Node install.

## Running locally

```powershell
cd src\ScribanTutorial
dotnet run
```

The dev server prints a `http://localhost:<port>` URL. From the repo
root, `dotnet build ScribanTutorial.slnx` also works — that's what CI
runs.

## Repository layout

```
scriban-tutorial/
├─ src/ScribanTutorial/        Blazor WebAssembly app (the runtime)
│  ├─ Pages/                   Routed pages (NNN_name.razor — numeric
│  │                           prefix encodes prev/next order) and shared
│  │                           components (ExerciseBlock, TheoryBlock,
│  │                           DiffView, PageNav)
│  ├─ Layout/                  NavMenu, MainLayout
│  ├─ Services/                ContentService, PageOrder, ProgressService,
│  │                           ThemeService, ScribanRunner, JsonToScriban,
│  │                           CodeEditorHandle, helpers
│  └─ wwwroot/                 Static assets (CSS, JS, lessons/, manifest.json)
├─ tools/ContentBuilder/       .NET console tool: Markdown → HTML, bundle.json,
│                              syntax-highlighter, --verify subcommand
├─ tests/ScribanTutorial.Tests/ xUnit suite
├─ docs/                       Architecture, deployment, authoring guide,
│                              security, best practices
└─ .github/workflows/          CI: build + test + publish + deploy
```

[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) has the detailed map —
which service owns what, how the build pipeline is wired, where each
piece of state lives.

## Build pipeline

`dotnet build` runs the `BuildContent` MSBuild target before computing
static web assets. That target invokes
[`tools/ContentBuilder/`](tools/ContentBuilder/), which runs a prune step and
then six mtime-driven build passes — the core three are:

| Pass | Walks | Emits |
|---|---|---|
| Markdown → HTML | every `*.md` under `wwwroot/lessons/` | `*.html` sibling (theory files also get a `01-theory.toc.json` outline sidecar) |
| Data-model pretty-print | every `02-datamodel.json` | `02-datamodel.html` sibling |
| Exercise bundling | every `05-solution.txt` (= every exercise dir) | `bundle.json` with all runtime inputs; when the optional `06-cases.json` (hidden validation cases) is present, each case's expected output is derived from the solution and embedded as a `cases` array |

The remaining passes emit `search-index.json`, `reference.json`, and
`sitemap.xml` — the full pass table is in
[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

All generated artifacts (`*.html`, `bundle.json`, `01-theory.toc.json`,
the indexes, `sitemap.xml`) are gitignored. Lesson source files and the
unbundled editor sources are also excluded from publish — the deployed
site ships only the generated content plus the editor bundle.

The About and Contribute pages' bodies are authored directly in their
respective `.razor` files (`Pages/001_about.razor`,
`Pages/999_contribute-a-lesson.razor`) — not generated from external
`.md` sources. Edits to that content go through Razor.

ContentBuilder also has a `--verify <exercise-path>` subcommand for
checking a single exercise's canonical solution against its expected
output — used by the lesson-author workflow on the rendered Contribute
page. It also validates the optional `06-cases.json` (must be a JSON
array of objects, and the solution must render cleanly against every
case).

## Test suite

```powershell
dotnet test
```

Under [`tests/ScribanTutorial.Tests/`](tests/ScribanTutorial.Tests/).
Helpers from the WASM project are linked via `<Compile Include="…" Link="Shared\…" />`
in [`ScribanTutorial.Tests.csproj`](tests/ScribanTutorial.Tests/ScribanTutorial.Tests.csproj)
so the test assembly compiles the same source the app runs.

| Test class | What it covers |
|---|---|
| `ContentNormalizeTests` | CRLF / trailing-newline normalisation. |
| `JsonToScribanTests` | JSON → ScriptObject conversion (incl. the int-vs-float fix). |
| `ScribanRunnerTests` | Render path, parse errors, JSON-error message, the in-flight 250 KB output cap. |
| `ExerciseSolutionTests` | **Data-driven from the manifest** — every exercise's canonical solution must render to its expected output, plus every hidden validation case in an optional `06-cases.json`. Add an exercise → it gets a test for free. |
| `ExampleSolutionTests` | **Data-driven from the theory files** — every `:::example` with an Output panel re-renders and must match. |
| `ContentBuilderTests` | `MarkdownRenderer` :::example layout, "Try in playground" link, sanitiser; heading ids; `TextMateHighlighter` snippet correctness; per-edge grammar regression locks. |
| `SearchIndexQueryTests` | The pure search ranking — AND across terms, solution-code hits, title boosts, snippet/highlight correctness. |
| `ReferenceIndexBuilderTests` | The built-in table parser behind `reference.json` — row classification, section anchors, slugs. |
| `BuildTargetTest` | Every `.md` has a fresh `.html` sibling (+ `01-theory.toc.json` for theory), every exercise a fresh `bundle.json` (`06-cases.json` counts as a staleness source), and `search-index.json` / `reference.json` / `sitemap.xml` are no staler than their sources. Catches the "MSBuild target stopped running" failure mode. |

CI gates the deploy on `dotnet test` going green.

## Project mechanics

Housekeeping notes about how the project itself is wired — read before
opening a PR that touches the embedded editor or the deployment pipeline.

### CodeMirror vendoring and the editor bundle

Vendored under [`src/ScribanTutorial/wwwroot/lib/codemirror/`](src/ScribanTutorial/wwwroot/lib/codemirror/)
as 11 ESM files. They are *bundling inputs only*: at runtime the app
imports a single pre-bundled, minified module,
[`wwwroot/js/editor.bundle.min.js`](src/ScribanTutorial/wwwroot/js/editor.bundle.min.js),
built offline with a standalone esbuild 0.25.5 binary (no Node install —
the procedure, alias flags, and sanity checks are in
[`wwwroot/lib/codemirror/VERSION.txt`](src/ScribanTutorial/wwwroot/lib/codemirror/VERSION.txt)).
There is no importmap in `index.html`, and the unbundled sources
(`js/editor.js`, the two language modules, the `lib/codemirror/` tree)
stay in the repo but are excluded from publish via `Content Remove`
entries in the csproj.

Bumps are scripted via [`tools/Vendor-CodeMirror.ps1`](tools/Vendor-CodeMirror.ps1)
— edit the `$packages` table with the new version pin, run the script,
update [`wwwroot/lib/codemirror/VERSION.txt`](src/ScribanTutorial/wwwroot/lib/codemirror/VERSION.txt)
to match, re-run the bundling step from VERSION.txt, and commit the
bumped files + VERSION.txt + the regenerated `editor.bundle.min.js` in
one PR. The script reports a SHA-256 prefix per file so two runs on a
clean checkout can be diff-compared. The same rule applies to any edit
of `js/editor.js` or the language modules: re-bundle and commit the
bundle together with the source change.

The `codemirror` umbrella package is intentionally *not* vendored: its
`basicSetup` would drag in `@codemirror/search`,
`@codemirror/autocomplete`, and `@codemirror/lint`, none of which this
app uses. Extensions are composed by hand in
[`wwwroot/js/editor.js`](src/ScribanTutorial/wwwroot/js/editor.js).

### GitHub Pages CDN cache

Pages serves through a CDN that occasionally takes 2–5 minutes after a
successful deploy to propagate. If the workflow run is green but the
site still shows old content, hard-refresh (Ctrl+F5) and wait. Full
deployment pipeline details in [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md).

## Code conventions

- **Treat warnings as errors.** `TreatWarningsAsErrors=true` is set on
  the WASM project; don't silence a warning with `#pragma` unless you
  add a code comment explaining why.
- **Razor scoped CSS.** Per-component styles live next to the component
  (`Foo.razor.css`). Use `::deep` to reach descendant elements the
  component itself doesn't render (e.g. content injected via
  `@((MarkupString)...)`). See
  [`Pages/TheoryBlock.razor.css`](src/ScribanTutorial/Pages/TheoryBlock.razor.css)
  for the canonical example.
- **Page filenames carry their nav order.** Routed pages live under
  [`src/ScribanTutorial/Pages/`](src/ScribanTutorial/Pages/) and are named
  `NNN_name.razor` — e.g. `000_home.razor`, `001_about.razor`,
  `002_playground.razor`, `010_lesson.razor`, `999_contribute-a-lesson.razor`.
  Razor escapes the leading digit in the generated class name (`_000_home`,
  etc.), so the alphabetical sort of type names matches the numeric order
  on disk. [`Services/PageOrder`](src/ScribanTutorial/Services/PageOrder.cs)
  reflects over routed components, sorts by type name, expands the
  `lesson/{LessonId}` slot from the manifest, and drives
  [`PageNav`](src/ScribanTutorial/Pages/PageNav.razor)'s prev/next links.
  To insert a new page between two existing ones, pick a numeric prefix
  that sorts between them — there's no central list to update.
- **Shared (non-routed) components in `Pages/`** don't take a numeric
  prefix — `ExerciseBlock.razor`, `TheoryBlock.razor`, `DiffView.razor`,
  `PageNav.razor`. `PageOrder`'s reflection filter skips types without a
  `[Route]` attribute, so they're invisible to the prev/next chain.
- **No emojis in source files** unless the user explicitly asks.
- **Comments explain WHY, not WHAT.** Named identifiers should already
  cover the "what" — only add a comment when there's a non-obvious
  constraint, invariant, or workaround worth recording.
- **One coherent change per commit.** The site auto-deploys on push to
  `main`; CI runs on PRs but publish/deploy is gated to push-to-main.

## Submitting a pull request

1. Fork the repo (one-click on github.com) and create a branch.
2. Push your changes; open a PR against `main`.
3. CI runs build + tests + `dotnet list package --vulnerable --include-transitive`
   automatically. Watch for the green checks at the bottom of the PR
   page.
4. The maintainer reviews. Small comments → just edit the file in your
   branch (the PR updates automatically). Larger changes → push new
   commits to the same branch.
5. On merge, the deploy workflow runs and the site updates within a
   few minutes.

The full deploy pipeline (`.github/workflows/deploy.yml`) is documented
in [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md).

## Security disclosures

The threat model for the app is rendered on the
[About page](https://sergeiosipov.github.io/scriban-tutorial/about) (or
read it as Razor in
[`Pages/001_about.razor`](src/ScribanTutorial/Pages/001_about.razor)).
If you find a vulnerability that affects deployed users (e.g. an XSS in
the Markdig→HTML pipeline that the sanitiser misses), open a private
disclosure via the GitHub Security tab rather than a public issue.

## Where to read next

- [`README.md`](README.md) — landing-page overview and prerequisites.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — detailed map of services, components, build pipeline, asset layout.
- [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md) — CI pipeline, base href rewrite, SPA routing on GitHub Pages.

The threat model, known issues, course-coverage notes, and the full
lesson-authoring reference all live as rendered pages on the live site
([About](https://sergeiosipov.github.io/scriban-tutorial/about) and
[Contribute a lesson](https://sergeiosipov.github.io/scriban-tutorial/contribute))
rather than as standalone `.md` files in this repo. Their source is
Razor under [`Pages/`](src/ScribanTutorial/Pages/).
