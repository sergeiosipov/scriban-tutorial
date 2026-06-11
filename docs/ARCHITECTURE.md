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
│    ├─ prunes generated files whose sources are gone   │
│    ├─ scans wwwroot/lessons/**/*.md                   │
│    ├─ Markdig + custom :::example renderer            │
│    ├─ TextMateSharp colours fenced code blocks        │
│    ├─ writes *.html siblings + 02-datamodel.html      │
│    ├─ writes 01-theory.toc.json (h2/h3 outline)       │
│    ├─ writes per-exercise bundle.json (description    │
│    │  + dataModel + dataModelHtml + expected +        │
│    │  template + solution + derived hidden-case       │
│    │  outputs in one fetchable blob)                  │
│    └─ writes search-index.json + reference.json       │
│         + sitemap.xml (all under wwwroot/)            │
│  Triggered by a BuildContent MSBuild target;          │
│  mtime-driven staleness check skips fresh files.      │
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
│  Singleton services (per WASM tab)                    │
│    ├─ ContentService   — manifest + lazy lesson load  │
│    ├─ SearchService    — search-index.json → /search  │
│    ├─ ReferenceService — reference.json → /reference  │
│    ├─ PageOrder        — reflects over Pages types,   │
│    │                     sorts by type name, expands  │
│    │                     lesson slot from manifest;   │
│    │                     drives PageNav prev/next     │
│    ├─ ProgressService  — localStorage + in-mem mirror │
│    └─ ThemeService     — light / dark, persisted      │
│                                                       │
│  ExerciseBlock + Playground                           │
│    ├─ CodeMirror 6 editor (pre-bundled module,        │
│    │    lazy-mounted) via CodeEditorHandle            │
│    ├─ ScribanRunner (LoopLimit + in-flight 250 KB     │
│    │    output cap + 2 s render budget)               │
│    ├─ visible check, then every hidden case           │
│    ├─ DiffView (lazy-loaded DiffPlex) on fail         │
│    └─ Show solution / Reset                           │
└───────────────────────────────────────────────────────┘
```

## Build time: ContentBuilder

[`tools/ContentBuilder/`](../tools/ContentBuilder/) is a .NET 10 console tool the
WASM project's `BuildContent` MSBuild target invokes before publish. It runs
seven steps in order — a prune, then six build passes, each with mtime-based
staleness so unchanged files cost nothing:

| Pass | Walks | Emits |
|---|---|---|
| Prune | every generated file under `wwwroot/lessons/` | nothing — deletes `*.html` / `bundle.json` / `01-theory.toc.json` whose source is gone, then sweeps empty directories |
| Markdown → HTML (lessons) | every `*.md` under `wwwroot/lessons/` | `*.html` sibling; theory files also get a `01-theory.toc.json` h2/h3 outline sidecar that feeds the in-lesson TOC |
| Data-model pretty-print | every `02-datamodel.json` | `02-datamodel.html` sibling (syntax-highlighted JSON) |
| Exercise bundling | every `05-solution.txt` (= every exercise dir) | `bundle.json` sibling with all runtime inputs inline. An exercise may also carry an optional `06-cases.json` — a JSON array of alternative data models ("hidden validation cases"); ContentBuilder renders `05-solution.txt` against each one at bundle time and embeds the results as a trailing `"cases": [{dataModel, expected}]` array (omitted entirely when the file is absent) |
| Search index | manifest + each theory `.md` and every exercise description / template / solution | one whole-corpus `wwwroot/search-index.json` the `/search` page fetches once ([SearchIndexBuilder.cs](../tools/ContentBuilder/SearchIndexBuilder.cs)) |
| Reference index | the built-in pipe tables in lessons 10–17's theory, driven by the manifest | one `wwwroot/reference.json` — function / property / specifier entries with section anchors, fetched once by `/reference` ([ReferenceIndexBuilder.cs](../tools/ContentBuilder/ReferenceIndexBuilder.cs)) |
| Sitemap | the manifest | `wwwroot/sitemap.xml` — static routes + one URL per lesson ([SitemapBuilder.cs](../tools/ContentBuilder/SitemapBuilder.cs)) |

Markdown rendering uses [Markdig](https://www.nuget.org/packages/Markdig) with
`UsePipeTables`, `UseAutoLinks`, `UseEmphasisExtras`, `UseCustomContainers`,
`UseAutoIdentifiers` (GitHub style — theory h2/h3 carry deep-linkable ids,
which the sanitiser explicitly allows), `UseGenericAttributes`, plus a custom
`:::example` container renderer in
[MarkdownRenderer.cs](../tools/ContentBuilder/MarkdownRenderer.cs) that emits the
three-pane Data / Template / Output layout. Every `:::example` also gets a
build-time "Try in playground" link — its template + data are
base64url-encoded into a `playground#try=` URL fragment. Code blocks are
syntax-highlighted at build time by
[TextMateHighlighter.cs](../tools/ContentBuilder/TextMateHighlighter.cs)
using TextMateSharp and the hand-written grammar at
[tools/ContentBuilder/grammars/scriban.tmLanguage.json](../tools/ContentBuilder/grammars/scriban.tmLanguage.json).

The About and Contribute pages' bodies are authored directly in their
respective `.razor` files (`Pages/001_about.razor`,
`Pages/999_contribute-a-lesson.razor`) as Razor markup, not generated
from external `.md` sources — so they're outside the ContentBuilder
pipeline entirely.

The tool also has a `--verify <exercise-path>` subcommand
([SolutionVerifier.cs](../tools/ContentBuilder/SolutionVerifier.cs)) that runs the
canonical solution against the data model and compares to the expected file.
When the exercise has a `06-cases.json`, `--verify` also validates the file's
shape (a JSON array of objects) and that the solution renders cleanly against
every case.

The search index combines theory prose with each exercise's template and
solution code, so a query for a built-in like `regex.replace` surfaces both the
lesson that explains it and every exercise whose solution calls it. Because it's
one whole-corpus file derived from the manifest plus every lesson source, *any*
lessons edit can stale it — not just a sibling edit.

All generated artifacts (`*.html`, `bundle.json`, `01-theory.toc.json`,
`search-index.json`, `reference.json`, `sitemap.xml`) are gitignored.

## Run time: Blazor WASM SPA

Pure client-side Blazor WebAssembly on .NET 10 — no backend, no database, no
accounts. The Scriban engine evaluates user templates directly in the browser
tab. Single page; SPA routing via `<Router>`.

Boot sequence:

1. `index.html` ships an inline boot shell so the user sees structure within
   ~100 ms. Framework asset references use fingerprint placeholders
   (`OverrideHtmlAssetPlaceholders` in the csproj rewrites them to the hashed
   names), plus a `dotnet.js` modulepreload and a course-manifest preload.
2. `js/spa-redirect.js` (synchronous, before `<base>`) decodes the
   `?/<path>` query that GitHub Pages' `404.html` bounce produces, so deep
   links resolve to the right Blazor route.
3. `js/theme-boot.js` (synchronous) applies the saved light/dark theme to
   `<html data-theme>` before any CSS paints — no flash.
4. `_framework/blazor.webassembly….js` loads with `autostart="false"`; the
   `js/boot.js` module then calls `Blazor.start` with a `loadBootResource`
   that fetches the `.br` precompressed framework assets and decodes them
   client-side with the vendored google/brotli decoder
   (`js/brotli-decode.min.js`) — GitHub Pages stores `.br` files but never
   serves them content-encoded. Dev builds have no `.br` siblings; a one-shot
   probe detects that and falls back to default loading automatically.
5. `js/boot.js` also registers the hand-rolled `wwwroot/service-worker.js`
   (deployed site only, not localhost): three cache tiers — cache-first for
   fingerprinted assets, stale-while-revalidate for content, network-first
   for navigations with an offline shell fallback. Together with
   `site.webmanifest` this makes the site installable and offline-capable.
   Details in [DEPLOYMENT.md](DEPLOYMENT.md).

Blazor then takes over the `#app` root.

## Routing

[`App.razor`](../src/ScribanTutorial/App.razor) wires the router. Routes are
discovered by assembly scan over the `ScribanTutorial.Pages` namespace; page
files use numeric prefixes so their generated class names sort alphabetically
into the navigation order (see [Linear page order](#linear-page-order) below):

- `/` → [`000_home.razor`](../src/ScribanTutorial/Pages/000_home.razor) (course index)
- `/about` → [`001_about.razor`](../src/ScribanTutorial/Pages/001_about.razor) (security threat model + known issues, authored inline)
- `/playground` → [`002_playground.razor`](../src/ScribanTutorial/Pages/002_playground.razor) (free-form Scriban editor)
- `/search` → [`003_search.razor`](../src/ScribanTutorial/Pages/003_search.razor) (full-text search over lessons + exercises; routable but kept out of the linear prev/next walk)
- `/reference` → [`004_reference.razor`](../src/ScribanTutorial/Pages/004_reference.razor) (searchable function reference grouped by module, deep-linking into lesson sections; like `/search`, kept out of the linear prev/next walk)
- `/lesson/{LessonId}` → [`010_lesson.razor`](../src/ScribanTutorial/Pages/010_lesson.razor) (theory + N exercises)
- `/contribute` → [`999_contribute-a-lesson.razor`](../src/ScribanTutorial/Pages/999_contribute-a-lesson.razor) (non-programmer walkthrough + full authoring reference, authored inline)

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

A page can be routable yet opt out of this linear walk: `PageOrder` keeps an
`_excludedFromLinearOrder` set (currently `search` and `reference`), so those
utility pages never show up as Previous/Next targets even though their files
(`003_search.razor`, `004_reference.razor`) would otherwise sort between
Playground and the lessons.

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
| [`ContentService`](../src/ScribanTutorial/Services/ContentService.cs) | Manifest (memoised). Per-lesson `LessonContent` cache (`ConcurrentDictionary<lessonId, Task<LessonContent>>`). Lazy-fetches each lesson's `theory.html` + `01-theory.toc.json` heading sidecar (tolerant of its absence) + one `bundle.json` per exercise. Cancellation tokens guard the page-level await; inner fetches run uncancelled so the cache never holds a faulted task. |
| [`SearchService`](../src/ScribanTutorial/Services/SearchService.cs) | Fetches the build-time `search-index.json` once (memoised, same shape as the manifest), runs it through `SearchIndexQuery.Prepare` (fields pre-lowered once at load), then answers `/search` queries in memory through the pure `SearchIndexQuery` ranking — no per-keystroke re-lowering. |
| [`ReferenceService`](../src/ScribanTutorial/Services/ReferenceService.cs) | Fetches the build-time `reference.json` once (memoised, same pattern as `SearchService`) and hands the deserialised modules to the `/reference` page, which filters them in memory per keystroke. |
| [`PageOrder`](../src/ScribanTutorial/Services/PageOrder.cs) | Computes the linear page order via reflection over the `ScribanTutorial.Pages` namespace. Sorts by type name (the numeric file prefixes survive into the generated identifier), expands the lesson slot from the manifest, and exposes `GetPrevNextAsync(route)`. |
| [`ProgressService`](../src/ScribanTutorial/Services/ProgressService.cs) | localStorage wrapper + in-memory mirror. The whole store is hydrated once in a single `exportWithPrefix` interop round-trip (the store is tiny — one global hydration beats per-lesson scans); after that all reads are pure memory. `SaveAsync` / `ResetAsync` write both stores in lockstep and raise `Changed`. NavMenu subscribes for indicator updates. `GetMostRecentAsync` backs Home's "Continue where you left off" card; `ExportAllAsync` / `ImportAsync` back the About page's progress download / upload. |
| [`ThemeService`](../src/ScribanTutorial/Services/ThemeService.cs) | Reads the `<html data-theme>` set by `theme-boot.js`, persists toggles. Raises `Changed` so listeners (NavMenu) re-render. |

Stateless helpers shared with `ContentBuilder` via `<Compile Link…>`:

| Helper | Purpose |
|---|---|
| [`ScribanRunner`](../src/ScribanTutorial/Services/ScribanRunner.cs) | One source of truth for parse + ScriptObject + render. `LoopLimit=100_000`, `RecursiveLimit=100`, plus two in-flight guards: a 250 KB output cap enforced on every write (`GuardedOutput`, an `IScriptOutput`) and a 2 s render budget polled from an `OnStepLoop` override — WASM is single-threaded, so the deadline must be checked from inside the render. Runaway templates abort with a friendly error instead of freezing or OOMing the tab. Friendly "Data model isn't valid JSON: …" for `JsonException`. Used by ExerciseBlock, Playground, and `--verify`. |
| [`JsonToScriban`](../src/ScribanTutorial/Services/JsonToScriban.cs) | JSON `Element` → Scriban `ScriptObject`. Distinguishes long from double (the bug that previously turned all integers into floats is locked down by [JsonToScribanTests](../tests/ScribanTutorial.Tests/JsonToScribanTests.cs)). |
| [`ContentNormalize`](../src/ScribanTutorial/Services/ContentNormalize.cs) | CRLF→LF + trailing-newline trim before output comparison. Both the runtime and `--verify` use this. |
| [`SearchIndexQuery`](../src/ScribanTutorial/Services/SearchIndex.cs) | The `SearchDoc` record + pure ranking / snippet / highlight logic, plus a `Prepare` step that pre-lowers every doc's searchable fields once at index load so per-keystroke queries allocate no lowered strings. `ContentBuilder`'s `SearchIndexBuilder` emits the index, `SearchService` queries it, `SearchIndexQueryTests` exercises it — one shared source. |
| [`CodeEditorHandle`](../src/ScribanTutorial/Services/CodeEditorHandle.cs) | Per-page wrapper around the pre-bundled `js/editor.bundle.min.js` (source of truth: `js/editor.js`). Imports the module once, tracks mounted element IDs, tears them all down in `DisposeAsync`. The editor is pull-based — no per-keystroke JS→.NET push; consumers call `GetValueAsync` at submit / share / persist time. `MountAsync(lazy: true)` defers creating the real CodeMirror view behind a placeholder until the element scrolls into view (IntersectionObserver) or is clicked/focused. |

## Pages and components

Page-level (routed) components carry numeric prefixes so their alphabetised
generated class names match the linear navigation order (see
[Linear page order](#linear-page-order)):

- `000_home.razor` (`/`) — course index. "Continue where you left off"
  card, About / Playground / Search / Reference cards, then lessons
  grouped under section headers with descriptions and per-lesson
  passed counts.
- `001_about.razor` (`/about`) — security threat model, dependency
  pins, known issues, course coverage, plus a "Your data" section with
  progress export / import — all authored inline as Razor markup.
- `002_playground.razor` (`/playground`) — free-form Scriban editor.
  Accepts `#try=` deep links from theory examples, persists work to
  localStorage, "Copy share link" / "Reset to example" buttons.
- `003_search.razor` (`/search`) — full-text search over lesson theory
  and exercise code; results deep-link to `lesson/{id}#exercise-{slug}`,
  and `010_lesson` scrolls the target exercise into view after its async
  content renders (`js/nav-scroll.js`).
- `004_reference.razor` (`/reference`) — searchable function reference
  grouped by module; each entry deep-links to the lesson section that
  teaches it.
- `010_lesson.razor` (`/lesson/{LessonId}`) — theory + N exercises.
  Breadcrumb ("All lessons · Lesson NN of MM · x/y passed"), "In this
  lesson" TOC over theory headings + exercise links, a top `<PageNav>`,
  and an "Edit this lesson on GitHub" link.
- `999_contribute-a-lesson.razor` (`/contribute`) — non-programmer
  walkthrough + full authoring reference, authored inline as Razor
  markup.

Shared (non-routed) components live in the same folder without numeric
prefixes — `PageOrder`'s reflection filter only picks up types with a
`[Route]` attribute, so these are skipped:

- [`ExerciseBlock.razor`](../src/ScribanTutorial/Pages/ExerciseBlock.razor) — the
  card per exercise, titled from the manifest (slug fallback). Owns
  OnInitialized progress restore, the (lazy) editor mount via
  `CodeEditorHandle`, Submit/Reset/Show solution buttons, persist-on-Submit.
  Submit runs the visible check first, then every hidden validation case from
  the bundle — all must pass; the first failing case surfaces its data model
  and diff. Grossly oversized outputs short-circuit to a one-line "much longer
  than expected" note instead of a giant diff. The fail-with-diff path
  delegates rendering to [`DiffView.razor`](../src/ScribanTutorial/Pages/DiffView.razor).
- [`DiffView.razor`](../src/ScribanTutorial/Pages/DiffView.razor) — takes a
  `DiffPaneModel` and renders the inline diff in its own `<pre class="diff">`
  with scoped CSS, capped at 200 lines (a truncation summary row covers the
  rest). DiffPlex itself is lazy-loaded — see the csproj's
  `BlazorWebAssemblyLazyLoad` entry and `App.razor`'s `OnNavigateAsync`,
  which fetches the assembly on the first lesson navigation.
- [`TheoryBlock.razor`](../src/ScribanTutorial/Pages/TheoryBlock.razor) — renders
  pre-built theory HTML via `@((MarkupString)Html)`. Source is author-controlled
  Markdown from the repo (see the Cross-site scripting section of the
  rendered [About page](https://sergeiosipov.github.io/scriban-tutorial/about)
  for the sanitiser threat model).
  Each lesson's title comes from the manifest and is rendered by `010_lesson.razor`
  above the theory body — so theory `.md` files do not start with a `# Title`
  heading (that would render the title twice).
- [`PageNav.razor`](../src/ScribanTutorial/Pages/PageNav.razor) — self-resolving
  prev/next pager. Reads its own route from `NavigationManager`, queries
  `PageOrder`, re-renders on `LocationChanged`. No parameters.
- [`NavMenu.razor`](../src/ScribanTutorial/Layout/NavMenu.razor) — sidebar with
  course title, theme toggle, Home / About / Playground / Search / Reference /
  lesson list with progress indicators / Contribute / reset-all. Collapses
  behind a disclosure toggle at ≤860 px.

## CodeMirror 6 — vendored, slimmed

ESM modules under [`wwwroot/lib/codemirror/`](../src/ScribanTutorial/wwwroot/lib/codemirror/).
At runtime the app loads exactly one editor module: the pre-bundled, minified
[`js/editor.bundle.min.js`](../src/ScribanTutorial/wwwroot/js/editor.bundle.min.js)
(imported by `CodeEditorHandle`). It is built offline with a standalone
esbuild 0.25.5 binary — the project stays Node-free — which resolves the bare
`@codemirror/*` specifiers to the vendored files via `--alias` flags; the full
procedure lives in
[`wwwroot/lib/codemirror/VERSION.txt`](../src/ScribanTutorial/wwwroot/lib/codemirror/VERSION.txt).
There is no importmap in `index.html` anymore (which also let the CSP
`script-src` drop `'unsafe-inline'`), and the unbundled sources —
`js/editor.js`, the two language modules, and the `lib/codemirror/` tree —
stay in the repo as bundling inputs but are excluded from publish via
`Content Remove` entries in the csproj.

We compose extensions by hand
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

Editor mounts can be lazy: exercise editors render a lightweight placeholder
and only create the real `EditorView` when the element scrolls into view
(IntersectionObserver) or is clicked/focused. The editor is pull-based —
no per-keystroke callbacks into .NET; the document is read via `getValue`
when needed.

Re-vendoring is by hand; pinned versions in
[`wwwroot/lib/codemirror/VERSION.txt`](../src/ScribanTutorial/wwwroot/lib/codemirror/VERSION.txt).
After any re-vendor or editor-source change, re-run the bundling step there
and commit the regenerated `editor.bundle.min.js` with it.

## Static asset layout

```
src/ScribanTutorial/wwwroot/
  index.html                   # boot shell, fingerprint placeholders, CSP, SEO/OG meta tags
  404.html
  .nojekyll                    # GitHub Pages: don't run Jekyll, keep _framework/
  robots.txt                   # allow-all + sitemap pointer
  manifest.json                # course manifest (lessons + exercises)
  site.webmanifest             # web app manifest (installable PWA)
  service-worker.js            # hand-rolled, three cache tiers (offline support)
  search-index.json            # whole-corpus search index (built, gitignored)
  reference.json               # function-reference index (built, gitignored)
  sitemap.xml                  # manifest-derived sitemap (built, gitignored)
  css/app.css                  # tokens, layout, .hl-* highlight palette
  js/
    spa-redirect.js            # GitHub Pages 404 → SPA route
    theme-boot.js              # apply <html data-theme> before paint
    theme.js                   # ThemeService JS interop
    progress.js                # ProgressService JS interop
    boot.js                    # starts Blazor (.br-aware loadBootResource),
                               #   registers the service worker
    brotli-decode.min.js       # vendored google/brotli decoder used by boot.js
    editor.bundle.min.js       # THE editor module the app loads (pre-bundled)
    editor.js                  # editor source — repo-only, publish-excluded
    nav-scroll.js              # scroll a deep-linked #exercise into view
    scriban-language.js        # Scriban StreamLanguage — repo-only, publish-excluded
    json-language.js           # JSON StreamLanguage — repo-only, publish-excluded
  lib/codemirror/              # vendored ESM modules — bundling inputs only,
                               #   repo-only, publish-excluded
  lessons/<id>/
    01-theory.md               # author-edited markdown (no leading # Title)
    01-theory.html             # generated, gitignored
    01-theory.toc.json         # generated h2/h3 outline (in-lesson TOC), gitignored
    02-exercises/<slug>/
      01-description.md        # author-edited
      01-description.html      # generated
      02-datamodel.json        # author-edited
      02-datamodel.html        # generated
      03-expected.txt          # author-edited
      04-template.txt          # author-edited starter
      05-solution.txt          # author-edited canonical solution
      06-cases.json            # author-edited, OPTIONAL hidden validation cases
      bundle.json              # generated, all inputs (+ derived case
                               #   outputs) in one fetch
```

Publish ships only what the app fetches: the generated `.html` /
`bundle.json` / `toc.json` / index files plus the editor bundle. Lesson
source files (`*.md`, `03-expected.txt`, `04-template.txt`,
`05-solution.txt`, `02-datamodel.json`, `06-cases.json`) and the unbundled
editor sources are removed from static web assets in the csproj.

## Tests

xUnit, under [`tests/ScribanTutorial.Tests/`](../tests/ScribanTutorial.Tests/).
Helpers from the WASM project (ContentNormalize, JsonToScriban, ScribanRunner,
Models) are linked via `<Compile Include="…\src\…" Link="Shared\…" />` so the
test assembly compiles the same source the app runs.

| Test class | Covers |
|---|---|
| `ContentNormalizeTests` | CRLF / trailing-newline normalisation. |
| `JsonToScribanTests` | JSON → ScriptObject converter — including the bug that previously turned all integers into doubles. |
| `ScribanRunnerTests` | Render path, parse-error reporting, the "Data model isn't valid JSON" friendly message, the in-flight 250 KB output cap (runaway output is stopped mid-render; output just under the cap still succeeds). |
| `ExerciseSolutionTests` | Data-driven: every exercise's canonical solution rendered against its data model must match expected output, and every hidden validation case in an optional `06-cases.json` (shape-checked: array of objects) must render cleanly. Add an exercise → it gets a test free. Plus structural smoke tests on the manifest. |
| `ExampleSolutionTests` | Data-driven sibling for theory: every `:::example` with an Output panel re-renders its Template against its Data and must match. Examples without an Output panel are skipped (illustrative snippets). |
| `ContentBuilderTests` | `MarkdownRenderer` :::example block emits three panels in the right order with the right language- classes plus a "Try in playground" link carrying the base64url-encoded template + data; headings get GitHub-style ids that survive the sanitiser; the sanitiser strips `<script>`, `on*=`, `javascript:`, `<iframe>`; per-edge grammar regression locks; `TextMateHighlighter` produces `.hl-brace`, `.hl-variable`, `.hl-operator`, `.hl-type` spans for a known snippet. |
| `SearchIndexQueryTests` | The pure search ranking: AND across terms, function references found inside solution code, title hits outranking body-only hits, snippet/highlight correctness. |
| `ReferenceIndexBuilderTests` | The built-in table parser: columns map by header name across the lessons' different layouts, rows classify as function / property / specifier, entries anchor to the nearest preceding `h2`, slugs match Markdig's GitHub auto-identifiers, and the emitted `reference.json` is camelCase + skipped when fresh. |
| `BuildTargetTest` | Every lesson `.md` has a fresh `.html` sibling, every theory a fresh `01-theory.toc.json` sidecar, every exercise a fresh `bundle.json` (with the optional `06-cases.json` counted among the bundle's staleness sources), and `search-index.json` / `reference.json` / `sitemap.xml` are no older than their sources. Catches the "BuildContent MSBuild target stopped running" failure mode without a full publish. |

Run: `dotnet test` from the repo root.

## Deployment

[GitHub Pages via Actions](DEPLOYMENT.md). Push to `main` runs
[`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml): build the
solution, run the tests, `dotnet publish`, rewrite the base href to
`/scriban-tutorial/`, upload to Pages.

## Where to read next

- [README](../README.md) — landing-page overview and prerequisites.
- [CONTRIBUTING.md](../CONTRIBUTING.md) — developer onboarding (prerequisites, test suite, build pipeline, project mechanics).
- [DEPLOYMENT.md](DEPLOYMENT.md) — GitHub Pages pipeline, base href, SPA routing.

The threat model, known issues, course-coverage notes, and the full
lesson-authoring reference live as rendered pages rather than `.md`
files in this repo:

- [About page](https://sergeiosipov.github.io/scriban-tutorial/about) — source in [`Pages/001_about.razor`](../src/ScribanTutorial/Pages/001_about.razor).
- [Contribute a lesson page](https://sergeiosipov.github.io/scriban-tutorial/contribute) — source in [`Pages/999_contribute-a-lesson.razor`](../src/ScribanTutorial/Pages/999_contribute-a-lesson.razor).
