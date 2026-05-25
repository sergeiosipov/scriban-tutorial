# Architecture

Current-state map of how this app is wired. Skim this before making non-trivial
changes — it links to the runtime-relevant code and the docs that explain each
piece in depth.

- [Two halves: build time and run time](#two-halves-build-time-and-run-time)
- [Build time: ContentBuilder](#build-time-contentbuilder)
- [Run time: Blazor WASM SPA](#run-time-blazor-wasm-spa)
- [Routing](#routing)
- [Services](#services)
- [Pages and components](#pages-and-components)
- [Static asset layout](#static-asset-layout)
- [Tests](#tests)
- [Deployment](#deployment)
- [Where to read next](#where-to-read-next)

## Two halves: build time and run time

```
┌────────────── Build time (dotnet build) ──────────────┐
│  tools/ContentBuilder/  (.NET console)                │
│    ├─ scans wwwroot/lessons/**/*.md                   │
│    ├─ Markdig + custom :::example renderer            │
│    ├─ TextMateSharp colours fenced code blocks        │
│    ├─ writes *.html siblings + 02-datamodel.html      │
│    └─ writes per-exercise bundle.json (description    │
│       + dataModel + dataModelHtml + expected +        │
│       template + solution in one fetchable blob)      │
│  Triggered by a BuildContent MSBuild target;          │
│  mtime-driven staleness check skips fresh files.      │
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
│  Singleton services (per WASM tab)                    │
│    ├─ ContentService  — manifest + lazy lesson load   │
│    │                  + reference-doc fetch (memoised)│
│    ├─ PageOrder       — reflects over Pages namespace,│
│    │                    sorts by type name, expands   │
│    │                    lesson slot from manifest;    │
│    │                    drives PageNav prev/next      │
│    ├─ ProgressService — localStorage + in-mem mirror  │
│    └─ ThemeService    — light / dark, persisted       │
│                                                       │
│  ExerciseBlock + Playground                           │
│    ├─ CodeMirror 6 editor via CodeEditorHandle        │
│    ├─ ScribanRunner (LoopLimit + 250 KB output cap)   │
│    ├─ DiffView (DiffPlex) on fail                     │
│    └─ Show solution / Reset                           │
└───────────────────────────────────────────────────────┘
```

## Build time: ContentBuilder

[`tools/ContentBuilder/`](../tools/ContentBuilder/) is a .NET 10 console tool the
WASM project's `BuildContent` MSBuild target invokes before publish. It does
four passes, each with mtime-based staleness so unchanged files cost nothing:

| Pass | Walks | Emits |
|---|---|---|
| Markdown → HTML (lessons) | every `*.md` under `wwwroot/lessons/` | `*.html` sibling |
| Data-model pretty-print | every `02-datamodel.json` | `02-datamodel.html` sibling (syntax-highlighted JSON) |
| Exercise bundling | every `05-solution.txt` (= every exercise dir) | `bundle.json` sibling with all six runtime inputs inline |
| Reference docs | top-level repo docs (`docs/SECURITY.md`, `KNOWN_ISSUES.md`, `docs/AUTHORING_LESSONS.md`) | `wwwroot/reference/*.html` consumed by the About and Contribute pages |

Markdown rendering uses [Markdig](https://www.nuget.org/packages/Markdig) with
`UsePipeTables`, `UseAutoLinks`, `UseEmphasisExtras`, `UseCustomContainers`,
`UseGenericAttributes`, plus a custom `:::example` container renderer in
[MarkdownRenderer.cs](../tools/ContentBuilder/MarkdownRenderer.cs) that emits the
three-pane Data / Template / Output layout. Code blocks are syntax-highlighted
at build time by [TextMateHighlighter.cs](../tools/ContentBuilder/TextMateHighlighter.cs)
using TextMateSharp and the hand-written grammar at
[tools/ContentBuilder/grammars/scriban.tmLanguage.json](../tools/ContentBuilder/grammars/scriban.tmLanguage.json).

The reference-doc pass also runs through `MarkdownRenderer`, but with the
opt-in link-rewriter enabled: any relative href in the source that resolves
to a real file under the repo root is rewritten to a
`github.com/sergeiosipov/scriban-tutorial/blob/main/<path>` URL so the
reference doc still navigates from the deployed SPA, where the source file
isn't shipped.

The tool also has a `--verify <exercise-path>` subcommand
([SolutionVerifier.cs](../tools/ContentBuilder/SolutionVerifier.cs)) that runs the
canonical solution against the data model and compares to the expected file —
the per-exercise feedback loop documented in [AUTHORING_LESSONS.md](AUTHORING_LESSONS.md#verifying-your-work).

All generated artifacts (`*.html`, `bundle.json`, `wwwroot/reference/`) are gitignored.

## Run time: Blazor WASM SPA

Pure client-side Blazor WebAssembly on .NET 10 — no backend, no database, no
accounts. The Scriban engine evaluates user templates directly in the browser
tab. Single page; SPA routing via `<Router>`.

Boot sequence:

1. `index.html` ships an inline boot shell so the user sees structure within
   ~100 ms.
2. `js/spa-redirect.js` (synchronous, before `<base>`) decodes the
   `?/<path>` query that GitHub Pages' `404.html` bounce produces, so deep
   links resolve to the right Blazor route.
3. `js/theme-boot.js` (synchronous) applies the saved light/dark theme to
   `<html data-theme>` before any CSS paints — no flash.
4. `_framework/blazor.webassembly.js` loads the .NET runtime and
   `ScribanTutorial.dll`; Blazor takes over the `#app` root.

## Routing

[`App.razor`](../src/ScribanTutorial/App.razor) wires the router. Routes are
discovered by assembly scan over the `ScribanTutorial.Pages` namespace; page
files use numeric prefixes so their generated class names sort alphabetically
into the navigation order (see [Linear page order](#linear-page-order) below):

- `/` → [`000_home.razor`](../src/ScribanTutorial/Pages/000_home.razor) (course index)
- `/about` → [`001_about.razor`](../src/ScribanTutorial/Pages/001_about.razor) (rendered SECURITY + KNOWN_ISSUES)
- `/playground` → [`002_playground.razor`](../src/ScribanTutorial/Pages/002_playground.razor) (free-form Scriban editor)
- `/lesson/{LessonId}` → [`010_lesson.razor`](../src/ScribanTutorial/Pages/010_lesson.razor) (theory + N exercises)
- `/contribute` → [`999_contribute-a-lesson.razor`](../src/ScribanTutorial/Pages/999_contribute-a-lesson.razor) (rendered AUTHORING_LESSONS + non-programmer walkthrough)

`010_lesson` `@key`s each `<ExerciseBlock>` by `(LessonId, ex.Id)` so Blazor
creates fresh component instances on navigation instead of reusing the
previous lesson's first ExerciseBlock for the new lesson's first slot.

## Linear page order

Pages prev/next is automatic, not hand-wired. The
[`PageOrder`](../src/ScribanTutorial/Services/PageOrder.cs) service reflects
over every type in the `ScribanTutorial.Pages` namespace that has a
`[Route]` attribute, sorts by **type name** (which encodes the file's
numeric prefix after Razor's identifier escaping — `000_home.razor`
becomes class `_000_home`), and expands the dynamic
`/lesson/{LessonId}` slot into one entry per manifest lesson at the
position the file (`010_lesson.razor`) sorts to.

Each page just writes `<PageNav />` at the bottom. The
[`PageNav`](../src/ScribanTutorial/Pages/PageNav.razor) component reads
the current route from `NavigationManager`, asks `PageOrder` for
prev/next, and re-resolves on `LocationChanged`. To change the
navigation order, rename the file's numeric prefix — there's no central
list to update.

## Services

All registered as singletons in [`Program.cs`](../src/ScribanTutorial/Program.cs).
WASM is single-threaded (single scope per tab), so no synchronisation is needed.

| Service | What it owns |
|---|---|
| [`ContentService`](../src/ScribanTutorial/Services/ContentService.cs) | Manifest (memoised). Per-lesson `LessonContent` cache (`ConcurrentDictionary<lessonId, Task<LessonContent>>`). Lazy-fetches each lesson's `theory.html` + one `bundle.json` per exercise. Also memoises reference-doc fetches (`reference/security.html`, `reference/known-issues.html`, `reference/authoring-lessons.html`) for the About and Contribute pages. Cancellation tokens guard the page-level await; inner fetches run uncancelled so the cache never holds a faulted task. |
| [`PageOrder`](../src/ScribanTutorial/Services/PageOrder.cs) | Computes the linear page order via reflection over the `ScribanTutorial.Pages` namespace. Sorts by type name (the numeric file prefixes survive into the generated identifier), expands the lesson slot from the manifest, and exposes `GetPrevNextAsync(route)`. |
| [`ProgressService`](../src/ScribanTutorial/Services/ProgressService.cs) | localStorage wrapper + in-memory mirror. `GetAllForLessonAsync` hydrates the mirror once per lesson via one `listKeysWithPrefix + N gets` JS roundtrip, then serves from memory. `SaveAsync` / `ResetAsync` write both stores in lockstep and raise `Changed`. NavMenu subscribes for indicator updates. |
| [`ThemeService`](../src/ScribanTutorial/Services/ThemeService.cs) | Reads the `<html data-theme>` set by `theme-boot.js`, persists toggles. Raises `Changed` so listeners (NavMenu) re-render. |

Stateless helpers shared with `ContentBuilder` via `<Compile Link…>`:

| Helper | Purpose |
|---|---|
| [`ScribanRunner`](../src/ScribanTutorial/Services/ScribanRunner.cs) | One source of truth for parse + ScriptObject + render. `LoopLimit=100_000`, `RecursiveLimit=100`, output capped at 250 KB. Friendly "Data model isn't valid JSON: …" for `JsonException`. Used by ExerciseBlock, Playground, and `--verify`. |
| [`JsonToScriban`](../src/ScribanTutorial/Services/JsonToScriban.cs) | JSON `Element` → Scriban `ScriptObject`. Distinguishes long from double (the bug that previously turned all integers into floats is locked down by [JsonToScribanTests](../tests/ScribanTutorial.Tests/JsonToScribanTests.cs)). |
| [`ContentNormalize`](../src/ScribanTutorial/Services/ContentNormalize.cs) | CRLF→LF + trailing-newline trim before output comparison. Both the runtime and `--verify` use this. |
| [`CodeEditorHandle`](../src/ScribanTutorial/Services/CodeEditorHandle.cs) | Per-page wrapper around `js/editor.js`. Imports the module once, tracks mounted element IDs, tears them all down in `DisposeAsync`. |

## Pages and components

Page-level (routed) components carry numeric prefixes so their alphabetised
generated class names match the linear navigation order (see
[Linear page order](#linear-page-order)):

- `000_home.razor` (`/`) — course index. Lists About / Playground /
  lessons / Contribute as cards.
- `001_about.razor` (`/about`) — renders the build-time
  `reference/security.html` + `reference/known-issues.html` produced by
  ContentBuilder's reference-doc pass.
- `002_playground.razor` (`/playground`) — free-form Scriban editor.
- `010_lesson.razor` (`/lesson/{LessonId}`) — theory + N exercises.
- `999_contribute-a-lesson.razor` (`/contribute`) — non-programmer
  walkthrough + rendered `reference/authoring-lessons.html`.

Shared (non-routed) components live in the same folder without numeric
prefixes — `PageOrder`'s reflection filter only picks up types with a
`[Route]` attribute, so these are skipped:

- [`ExerciseBlock.razor`](../src/ScribanTutorial/Pages/ExerciseBlock.razor) — the
  card per exercise. Owns OnInitialized progress restore, the editor mount via
  `CodeEditorHandle`, Submit/Reset/Show solution buttons, persist-on-Submit. The
  fail-with-diff path delegates rendering to [`DiffView.razor`](../src/ScribanTutorial/Pages/DiffView.razor).
- [`DiffView.razor`](../src/ScribanTutorial/Pages/DiffView.razor) — takes a
  `DiffPaneModel` and renders the inline diff in its own `<pre class="diff">`
  with scoped CSS.
- [`TheoryBlock.razor`](../src/ScribanTutorial/Pages/TheoryBlock.razor) — renders
  pre-built theory HTML via `@((MarkupString)Html)`. Source is author-controlled
  Markdown from the repo (see [SECURITY.md](SECURITY.md#2-cross-site-scripting-xss)).
  Each lesson's title comes from the manifest and is rendered by `010_lesson.razor`
  above the theory body — so theory `.md` files do not start with a `# Title`
  heading (that would render the title twice).
- [`PageNav.razor`](../src/ScribanTutorial/Pages/PageNav.razor) — self-resolving
  prev/next pager. Reads its own route from `NavigationManager`, queries
  `PageOrder`, re-renders on `LocationChanged`. No parameters.
- [`NavMenu.razor`](../src/ScribanTutorial/Layout/NavMenu.razor) — sidebar with
  course title, theme toggle, Home / About / Playground / lesson list with
  progress indicators / Contribute / reset-all.

## CodeMirror 6 — vendored, slimmed

ESM modules under [`wwwroot/lib/codemirror/`](../src/ScribanTutorial/wwwroot/lib/codemirror/),
resolved via the importmap in `index.html`. We compose extensions by hand
([`js/editor.js`](../src/ScribanTutorial/wwwroot/js/editor.js)) instead of
pulling `basicSetup` from the `codemirror` umbrella — `basicSetup` transitively
imports `@codemirror/search`, `@codemirror/autocomplete`, and `@codemirror/lint`,
none of which this app uses (no search bar, completion popup, or lint UI).
Saving ~37 KB Brotli.

Two custom languages, both `StreamLanguage` parsers:
- [`js/scriban-language.js`](../src/ScribanTutorial/wwwroot/js/scriban-language.js)
- [`js/json-language.js`](../src/ScribanTutorial/wwwroot/js/json-language.js)

Highlight tags map to `.hl-*` CSS classes so light/dark switching is a single
`<html data-theme>` flip — no editor re-mount.

Re-vendoring is by hand; pinned versions in
[`wwwroot/lib/codemirror/VERSION.txt`](../src/ScribanTutorial/wwwroot/lib/codemirror/VERSION.txt).

## Static asset layout

```
src/ScribanTutorial/wwwroot/
  index.html
  404.html
  .nojekyll                    # GitHub Pages: don't run Jekyll, keep _framework/
  manifest.json                # course manifest (lessons + exercises)
  css/app.css                  # tokens, layout, .hl-* highlight palette
  js/
    spa-redirect.js            # GitHub Pages 404 → SPA route
    theme-boot.js              # apply <html data-theme> before paint
    theme.js                   # ThemeService JS interop
    progress.js                # ProgressService JS interop
    editor.js                  # CodeMirror mount/destroy/setValue
    scriban-language.js        # Scriban StreamLanguage
    json-language.js           # JSON StreamLanguage
  lib/codemirror/              # vendored ESM modules
  lessons/<id>/
    01-theory.md               # author-edited markdown (no leading # Title)
    01-theory.html             # generated, gitignored
    02-exercises/<slug>/
      01-description.md        # author-edited
      01-description.html      # generated
      02-datamodel.json        # author-edited
      02-datamodel.html        # generated
      03-expected.txt          # author-edited
      04-template.txt          # author-edited starter
      05-solution.txt          # author-edited canonical solution
      bundle.json              # generated, all six inputs in one fetch
  reference/                   # generated by ContentBuilder, gitignored
    security.html              # ← docs/SECURITY.md
    known-issues.html          # ← KNOWN_ISSUES.md
    authoring-lessons.html     # ← docs/AUTHORING_LESSONS.md
```

## Tests

xUnit, under [`tests/ScribanTutorial.Tests/`](../tests/ScribanTutorial.Tests/).
Helpers from the WASM project (ContentNormalize, JsonToScriban, ScribanRunner,
Models) are linked via `<Compile Include="…\src\…" Link="Shared\…" />` so the
test assembly compiles the same source the app runs.

| Test class | Covers |
|---|---|
| `ContentNormalizeTests` | CRLF / trailing-newline normalisation. |
| `JsonToScribanTests` | JSON → ScriptObject converter — including the bug that previously turned all integers into doubles. |
| `ScribanRunnerTests` | Render path, parse-error reporting, the "Data model isn't valid JSON" friendly message, the 250 KB output cap. |
| `ExerciseSolutionTests` | Data-driven: every exercise's canonical solution rendered against its data model must match expected output. Add an exercise → it gets a test free. Plus structural smoke tests on the manifest. |
| `ContentBuilderTests` | `MarkdownRenderer` :::example block emits three panels in the right order with the right language- classes; the relative-link rewriter turns real-repo paths into GitHub blob URLs and leaves anchors / absolutes / unresolvable paths alone; the sanitiser strips `<script>`, `on*=`, `javascript:`, `<iframe>`; per-edge grammar regression locks; `TextMateHighlighter` produces `.hl-brace`, `.hl-variable`, `.hl-operator`, `.hl-type` spans for a known snippet. |
| `BuildTargetTest` | Every lesson `.md` has a fresh `.html` sibling, every exercise has a fresh `bundle.json`, every reference doc rendered into `wwwroot/reference/`. Catches the "BuildContent MSBuild target stopped running" failure mode without a full publish. |

Run: `dotnet test` from the repo root.

## Deployment

[GitHub Pages via Actions](DEPLOYMENT.md). Push to `main` runs
[`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml): build the
solution, run the tests, `dotnet publish`, rewrite the base href to
`/scriban-tutorial/`, upload to Pages.

## Where to read next

- [README](../README.md) — landing-page overview and prerequisites.
- [AUTHORING_LESSONS.md](AUTHORING_LESSONS.md) — non-developer flow for adding or editing content.
- [SECURITY.md](SECURITY.md) — threat model for running user templates in the browser.
- [SCRIBAN_BEST_PRACTICES.md](SCRIBAN_BEST_PRACTICES.md) — Scriban patterns the lessons teach.
- [DEPLOYMENT.md](DEPLOYMENT.md) — GitHub Pages pipeline, base href, SPA routing.
- [KNOWN_ISSUES.md](../KNOWN_ISSUES.md) — Scriban TextMate grammar edge cases and other rough edges.
