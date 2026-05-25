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

No Node, npm, or external bundler. CodeMirror 6 is vendored under
[`src/ScribanTutorial/wwwroot/lib/codemirror/`](src/ScribanTutorial/wwwroot/lib/codemirror/).

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
│  ├─ Pages/                   Routed pages and per-component .razor.css
│  ├─ Layout/                  NavMenu, MainLayout
│  ├─ Services/                ContentService, ProgressService, ThemeService,
│  │                           ScribanRunner, JsonToScriban, helpers
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
[`tools/ContentBuilder/`](tools/ContentBuilder/), which does four
mtime-driven passes:

| Pass | Walks | Emits |
|---|---|---|
| Markdown → HTML | every `*.md` under `wwwroot/lessons/` | `*.html` sibling |
| Data-model pretty-print | every `02-datamodel.json` | `02-datamodel.html` sibling |
| Exercise bundling | every `05-solution.txt` (= every exercise dir) | `bundle.json` with all six inputs |
| Reference docs | top-level repo docs (`SECURITY.md`, `KNOWN_ISSUES.md`, `docs/AUTHORING_LESSONS.md`) | `wwwroot/reference/*.html` for the About and Contribute pages |

All generated artifacts (`*.html`, `bundle.json`, `wwwroot/reference/`)
are gitignored.

ContentBuilder also has a `--verify <exercise-path>` subcommand for
checking a single exercise's canonical solution against its expected
output — used by the lesson-author workflow on the rendered Contribute
page.

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
| `ScribanRunnerTests` | Render path, parse errors, JSON-error message, 250 KB output cap. |
| `ExerciseSolutionTests` | **Data-driven from the manifest** — every exercise's canonical solution must render to its expected output. Add an exercise → it gets a test for free. |
| `ContentBuilderTests` | `MarkdownRenderer` :::example layout, link rewriter, sanitiser; `TextMateHighlighter` snippet correctness; per-edge grammar regression locks. |
| `BuildTargetTest` | Every `.md` has a fresh `.html` sibling, every exercise has a fresh `bundle.json`, every reference doc rendered into `wwwroot/reference/`. Catches the "MSBuild target stopped running" failure mode. |

CI gates the deploy on `dotnet test` going green.

## Project mechanics

Housekeeping notes about how the project itself is wired — read before
opening a PR that touches the embedded editor or the deployment pipeline.

### CodeMirror vendoring

Vendored under [`src/ScribanTutorial/wwwroot/lib/codemirror/`](src/ScribanTutorial/wwwroot/lib/codemirror/)
as 11 ESM files resolved through an importmap in `index.html`. Bumps are
scripted via [`tools/Vendor-CodeMirror.ps1`](tools/Vendor-CodeMirror.ps1)
— edit the `$packages` table with the new version pin, run the script,
update [`wwwroot/lib/codemirror/VERSION.txt`](src/ScribanTutorial/wwwroot/lib/codemirror/VERSION.txt)
to match, and commit the bumped files + VERSION.txt in one PR. The
script reports a SHA-256 prefix per file so two runs on a clean checkout
can be diff-compared.

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

The threat model for the app is in [`docs/SECURITY.md`](docs/SECURITY.md).
If you find a vulnerability that affects deployed users (e.g. an XSS in
the Markdig→HTML pipeline that the sanitiser misses), open a private
disclosure via the GitHub Security tab rather than a public issue.

## Where to read next

- [`README.md`](README.md) — landing-page overview and prerequisites.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — detailed map of services, components, build pipeline, asset layout.
- [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md) — CI pipeline, base href rewrite, SPA routing on GitHub Pages.
- [`docs/SECURITY.md`](docs/SECURITY.md) — threat model.
- [`docs/AUTHORING_LESSONS.md`](docs/AUTHORING_LESSONS.md) — source for the rendered Contribute page; the full lesson-authoring reference.
- [`KNOWN_ISSUES.md`](KNOWN_ISSUES.md) — Scriban grammar edges and course-coverage gaps.
