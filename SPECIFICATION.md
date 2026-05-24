# Scriban Interactive Training Platform — Build Specification (v3, Windows 11 + GitHub Pages)

> **For:** Claude Code agent running on **Windows 11** with filesystem + PowerShell access.
> **Goal:** Implement the application end-to-end until `dotnet build` produces a working app, the dev server runs, the GitHub Actions workflow deploys to GitHub Pages on push, and all acceptance checks in §14 pass.
> **Working directory:** `C:\Users\senio\agents_space\scriban-tutorial\` — this directory IS the project root. Do NOT create a nested subfolder.
> **GitHub remote:** `https://github.com/sergeiosipov/scriban-tutorial.git` (already created, empty).
> **Deployed URL:** `https://sergeiosipov.github.io/scriban-tutorial/`
> **Shell:** PowerShell 7+ (`pwsh`) preferred. No Unix-isms in local commands. GitHub Actions runs on `ubuntu-latest` with bash — that's separate and OK.
> **Deployment target:** GitHub Pages, served from `https://sergeiosipov.github.io/scriban-tutorial/`. The repo-name subpath constraint reaches into base href, routing, asset paths, and the build workflow — see §0.7.
> **Versions verified:** May 22, 2026.

---

## 0. Mission

A client-side, zero-installation, browser-based interactive training course for the [Scriban](https://github.com/scriban/scriban) templating language. Pure Blazor WebAssembly — no backend, no database, no user accounts. The Scriban engine executes user templates in the browser. Content is manifest-driven; non-developers add lessons by editing files only.

---

## 0.5. Implementation stages

> **Spec update (2026-05-24):** The .NET 10 SDK's `dotnet new sln` now produces a
> **`.slnx`** file (modern XML solution format) by default, not the legacy `.sln`.
> Everywhere this spec refers to `ScribanTutorial.sln` (§4 file tree, §0.7 publish
> command, prose), substitute `ScribanTutorial.slnx`. `dotnet build`,
> `dotnet sln add`, and `dotnet publish` accept either. The project uses
> `ScribanTutorial.slnx`.

**Project root and Git setup (do this before Stage 1):**

This project is built **directly in the current working directory** (not a nested subfolder). The directory is already initialized as a Git working tree pointing at `https://github.com/sergeiosipov/scriban-tutorial.git` — verify with `git remote -v` before starting. If the remote is missing, add it:

```powershell
git remote add origin https://github.com/sergeiosipov/scriban-tutorial.git
```

The first push will trigger Git Credential Manager (browser popup → Authorize) and create the `main` branch on GitHub.

**After each stage:**

1. Run that stage's **gate check** (a small subset of the §14 acceptance criteria).
2. **Commit** the stage with a clear message: `stage N: <name>` (e.g., `stage 1: skeleton`).
3. **Push** to `origin/main`:
   ```powershell
   git push origin main
   ```
   First push: GCM prompts the browser. Subsequent pushes: silent.
4. Report status to the user, including the GitHub URL of the pushed commit.
5. **Stop and wait if a gate fails** — don't paper over it by moving on. Don't push a broken stage.

Pushing per stage gives off-machine backup, a visible commit graph, and a clean rollback point if a later stage corrupts something. If a stage's gate fails, commit `WIP:` work locally but do NOT push it — push only when the gate passes.

The order below is chosen so each stage delivers a working artifact you can inspect, and so risky pieces (TextMate grammar, CodeMirror integration) come early enough that problems surface before too much is built around them.

### Stage 1 — Skeleton (≈30 min)

Build the empty solution and project structure, no features yet.

- Pre-flight checks (§2): confirm .NET 10 SDK present, `wasm-tools` workload installed.
- Create directory tree per §4.
- Create solution + empty Blazor WASM project + empty ContentBuilder console project.
- `.gitignore`, `.gitattributes` (LF enforcement per §9).
- Minimal `Program.cs`, `App.razor`, `_Imports.razor`, `MainLayout.razor`, placeholder `Home.razor` saying "Hello".
- `wwwroot/index.html` with the loading shell (§13).

**Gate check:** `dotnet build` succeeds with zero warnings. `dotnet run` serves the placeholder at `http://localhost:xxxx`. Loading shell renders within 100 ms. Open it in Edge to confirm.

> **Spec update (2026-05-24, Stage 2 ↔ Stage 3 manifest sync):** This spec wants
> the Stage 2 manifest to list **all four lessons** *and* the full Stage-4 exercise
> roster up front (§5 example), while `ContentService.LoadLessonAsync` (Stage 3)
> fetches all exercise files for a lesson eagerly. If the manifest references
> exercises that don't exist on disk yet, Stage 3's lesson page 404s on lesson 01
> the moment any of its other exercises are unauthored.
>
> The build follows the spec exception: list only exercises that exist. Stage 3
> ships `01-basics` with `[hello]`; the other lessons keep their entry with an
> empty `exercises: []` plus a placeholder `01-theory.md`. The full manifest is
> restored when Stage 4 (content authoring) runs.
>
> A future spec revision could either (a) make `FetchLessonAsync` tolerant of
> missing exercise files, or (b) defer authoring the manifest entries until the
> matching files land. Option (b) is what the current build does.

### Stage 2 — Manifest + ContentService + NavMenu (≈45 min)

Wire up content discovery, no rendering of lesson content yet.

- Author `wwwroot/manifest.json` with all four lessons (no content files yet, just paths).
- Create the four lesson directories + empty exercise subdirs per §4.
- Implement `Services/Models.cs` (records).
- Implement `Services/ContentService.cs` (manifest eager, `LoadLessonAsync` stub returning empty).
- Implement `Layout/NavMenu.razor` reading from `ContentService`.
- Implement `Pages/Home.razor` as the course landing page (list of lessons, no content).

**Gate check:** Sidebar shows all four lessons. Network tab shows exactly one `manifest.json` request. No content files fetched yet. Clicking a lesson does nothing or shows "not implemented" — that's fine for now.

### Stage 3 — Plain-text lesson content end-to-end (≈45 min)

Get one lesson rendering without any highlighting or fancy markup.

- Author `01-basics/01-theory.md` (plain markdown, no `:::example` yet).
- Author the `hello` exercise: all five files (`01-description.md`, `02-datamodel.json`, `03-expected.txt`, `04-template.txt`, `05-solution.txt`).
- Add `LessonPage.razor` with `@page "/lesson/{LessonId}"`.
- Add `TheoryBlock.razor` rendering raw markdown for now (use Markdig with minimal pipeline as an interim — will move to build-time in Stage 6).
- Add `ExerciseBlock.razor` with a plain `<textarea>` (CodeMirror comes in Stage 7).
- Implement the Scriban runner per §9 with `LoopLimit`/`RecursiveLimit` caps, **but no diff view yet** — just pass/fail with a green/red badge.
- Implement `ContentService.LoadLessonAsync` for real.

**Gate check:** Navigate to `/lesson/01-basics`. Theory renders. The `hello` exercise loads its starter. Typing `Hello, {{ name }}!` and submitting shows green. Typing nonsense shows red. URL routing works (back/forward buttons).

### Stage 4 — Full course content (≈1 hour)

> **Spec update (2026-05-24, stage order):** Stage 4 is **deferred until after
> Stage 8** in the current build, on user direction. Reasons:
>
> 1. Stage 6 ships `ContentBuilder --verify`. With that available, every
>    `05-solution.txt` can be machine-checked against `03-expected.txt`
>    before being committed — much higher confidence than hand-verification.
> 2. Stage 7 ships the CodeMirror editor. Authoring exercises against the final
>    UX (real syntax highlighting, real submission flow) catches papercuts that
>    a plain textarea hides.
> 3. The Stage 3 placeholder content (`01-basics/01-theory.md` plus the `hello`
>    exercise) contains a **Liquid-vs-Scriban confusion**: it claims Scriban
>    statement tags are `{% ... %}`, which is Liquid syntax. In Scriban,
>    **both expressions and statement blocks use `{{ ... }}`** (e.g.,
>    `{{ if x }}...{{ end }}`). The Stage 4-deferred pass will rewrite the
>    theory against the authoritative reference at
>    <https://scriban.github.io/docs/language/>.
>
> Effective stage order: **1 → 2 → 3 → 5 → 6 → 7 → 8 → 4 → 9 → 10**.

Author the remaining seven exercises and three theory files. Pure content work, no code changes.

- `01-basics/member-access/` (all five files)
- `02-filters/01-theory.md` + `upcase/` + `math/`
- `03-control-flow/01-theory.md` + `list-loop/` + `conditional/`
- `04-assembly/01-theory.md` + `invoice/`
- For each: verify the `05-solution.txt` produces the `03-expected.txt` by hand (we don't have the `--verify` tool yet — that's Stage 6). At minimum, run each solution in the running app to confirm green.

**Gate check:** Every one of the seven exercises passes when the agent enters the intended solution. No CRLF in any content file (§14 check #14). Manifest entries match all directories.

### Stage 5 — ProgressService + persistence + DiffPlex + Show solution (≈1 hour)

Add the UX features around the existing exercise loop.

- Implement `Services/ProgressService.cs` with `wwwroot/js/progress.js` for localStorage.
- Subscribe `NavMenu` to `ProgressService.Changed`; render per-lesson progress indicators (○ ◐ ●).
- In `ExerciseBlock`: persist code + pass/fail + attempts on every Submit. Restore on mount.
- Add **DiffPlex** dependency. On fail, render an inline diff between actual and expected.
- Add **Show solution** button (always visible per §D1). Inline reveal of `05-solution.txt`.
- Add **Reset** button restoring `04-template.txt`.

**Gate check:** Pass an exercise → indicator turns ● in sidebar. Refresh page → editor restores last code, pass/fail state preserved. Fail an exercise → diff view shows the differing characters. Show solution reveals the solution text.

### Stage 6 — ContentBuilder + TextMateSharp + Markdig custom container (≈2 hours, most-risky)

Move markdown rendering and syntax highlighting from runtime to build time.

- Implement `tools/ContentBuilder/`:
  - `Program.cs` with CLI argument parsing.
  - `MarkdownRenderer.cs` using Markdig with `UsePipeTables`, `UseAutoLinks`, `UseEmphasisExtras`, `UseCustomContainers`, `UseGenericAttributes`.
  - `TextMateHighlighter.cs` using TextMateSharp.
  - Custom Markdig renderer for `:::example` containers, emitting the side-by-side HTML per §12.
  - File mtime staleness check.
  - `--verify` subcommand for §15 of the spec.
- **Write `tools/ContentBuilder/grammars/scriban.tmLanguage.json`** — the full grammar covering the language surface from https://scriban.github.io/docs/language/. This is the riskiest single artifact in the build.
- Add MSBuild target per §11 to invoke ContentBuilder before main build.
- Update `wwwroot/manifest.json` `theoryPath` entries to omit the `.md` extension.
- Update `TheoryBlock.razor` to fetch `.html` instead of `.md` and remove runtime Markdig.
- Update `ContentService` to fetch `01-description.html` (pre-rendered) instead of `01-description.md`.
- Update at least one theory file (`01-basics/01-theory.md`) to use the `:::example` syntax as a smoke test.

**Gate check:** `dotnet build` regenerates `.html` files. Theory blocks now show **colored** code, not plain. The `:::example` block in `01-basics/01-theory.md` renders as a side-by-side panel. `dotnet run --project tools\ContentBuilder -- --verify <exercise-path>` confirms a solution matches expected, fails on a deliberately broken expected. The browser receives `.html` files, no `.md` requests.

> **If the TextMate grammar produces wrong colors on edge cases:** that's expected on first iteration. Fix the most visible problems (string escaping, multi-line expressions, comment handling). Document remaining issues in a `KNOWN_ISSUES.md` for follow-up — don't block the stage on grammar perfection. The grammar is text under `tools/ContentBuilder/grammars/` that can be improved over time without touching code.

### Stage 7 — CodeMirror 6 editor (≈1.5 hours)

Replace `<textarea>` with a real editor.

- Download and vendor CodeMirror 6 + the language/highlight packages to `wwwroot/lib/codemirror/`. Commit them. Add a `VERSION.txt` file.
- Write `wwwroot/js/scriban-language.js` (stream parser per §10).
- Write `wwwroot/js/editor.js` (mount/destroy/setValue per §10).
- Update `ExerciseBlock.razor` to mount the editor in `OnAfterRenderAsync(firstRender)`, destroy in `Dispose`, sync changes via `DotNetObjectReference`.
- Hook the editor's color scheme to the current theme (light/dark — stub the toggle for now if theme work hasn't started).

**Gate check:** Open any exercise. The editor renders with line numbers, Scriban keywords colored, brackets paired. Typing updates the model on the .NET side. Reset and Show solution both update the editor view correctly. No JS console errors. Editor instances are destroyed when navigating away (no memory leaks across lesson switches — verify by clicking through all lessons and checking the editors Map in DevTools stays small).

### Stage 8 — Theme toggle + final polish (≈45 min)

- Add `theme-light.css` and `theme-dark.css`. Toggle via a sidebar button. Persist choice to localStorage. Re-mount CodeMirror with the matching highlight style on theme change.
- Update the loading-shell CSS to respect the active theme on cold boot.
- Tighten spacing, fonts (Segoe UI Variable, Cascadia Code), visual hierarchy per §12 of the original design notes.
- Make sure the editor is the visual focal point of each exercise.

**Gate check:** Toggle works, persists across refresh, both static theory blocks and live editor switch colors consistently.

### Stage 9 — Documentation + local acceptance (≈45 min)

- Write `docs/AUTHORING_LESSONS.md` per §15.
- Write `docs/SECURITY.md` per §16.
- Write `docs/SCRIBAN_BEST_PRACTICES.md` per §17.
- Write `docs/DEPLOYMENT.md` per §0.7 — how the GitHub Pages pipeline works, how to set the repo name, troubleshooting.
- Write project root `README.md` (one-paragraph description, prereqs, `dotnet run` command, links to the four guides, brief architecture summary, deploy status badge).
- Run **every** §14 acceptance criterion locally. Report results.
- Fresh-clone test: copy the project to a new directory, run `dotnet build` + `dotnet run`, confirm everything works with no manual setup.

**Gate check:** All §14 local acceptance criteria pass. Fresh-clone test passes. The four guides exist and are non-empty.

### Stage 10 — GitHub Pages deployment (≈1 hour)

The GitHub repo already exists (https://github.com/sergeiosipov/scriban-tutorial) and earlier stages have been pushing to it. This stage adds the GitHub Pages-specific files and verifies the deployment works.

**Agent work:**

- Create `.github/workflows/deploy.yml` per §0.7.
- Add `wwwroot/.nojekyll` (empty file) so GitHub Pages doesn't strip files starting with `_` (Blazor's `_framework/`).
- Add `wwwroot/404.html` — SPA-redirect bounce script (see §0.7) so direct links to `/lesson/02-filters` work.
- Add the SPA-redirect companion snippet to `index.html` (handles the redirect bounce).
- Pre-flight: `dotnet publish -c Release -o ./publish` locally. Confirm `./publish/wwwroot/` contains `index.html`, `404.html`, `.nojekyll`, `_framework/`, and the `lessons/**/*.html` content. The local published bundle is informational only — don't commit `publish/`. Add it to `.gitignore`.
- Commit and push.

**User work (one-time GitHub configuration):**

1. Go to https://github.com/sergeiosipov/scriban-tutorial/settings/pages
2. Under **Build and deployment**, set **Source** to **GitHub Actions**.
3. Save (the page may not have a save button — the choice is sticky as soon as you click).

That's the only manual GitHub step. Earlier pushes already populated the repo; this just authorizes the workflow to deploy.

**After Pages is enabled and the agent's commit lands:**

- GitHub Actions automatically runs `.github/workflows/deploy.yml`.
- Watch progress at https://github.com/sergeiosipov/scriban-tutorial/actions
- First run takes ~3–5 minutes (cold workflow + first WASM publish).
- Successful run: green checkmark, deployed URL shown in the Actions log.

**Gate check:** The workflow succeeds (green checkmark). The deployed site loads at https://sergeiosipov.github.io/scriban-tutorial/. All exercises pass with their solutions when run in the deployed app. Direct link to https://sergeiosipov.github.io/scriban-tutorial/lesson/01-basics works (no 404). Refreshing on a lesson page works (no 404). Browser console shows no errors.

> **Typical first-deploy failure modes:**
> - Wrong base href → assets 404, blank page. Check `<base href>` in deployed `index.html`. Should be `/scriban-tutorial/` not `/`.
> - Missing `.nojekyll` → `_framework/` 404s. Verify the file exists in the published output.
> - SPA routing 404 → 404.html missing the redirect script. Verify by visiting a deep link.
> Each is easily fixed with a follow-up commit. Don't panic on first failure.

### Time estimates and total

Rough total: **9–10 hours of agent work** assuming no major roadblocks. Realistic with debugging and grammar iteration: **a full day plus a few hours for deployment**. The risky stages are 6 (TextMate grammar), 7 (CodeMirror JS interop), and 10 (first GitHub Pages deploy — usually one or two iterations to get base-href and SPA routing right). Budget extra time for those.

### Roadblock handling

If a stage fails its gate check and the fix isn't obvious within ~15 minutes:

1. Commit the partial work with a `WIP:` prefix.
2. Document the problem in a `BLOCKED.md` at the project root with: what was tried, the error or unexpected output, and a hypothesis.
3. Stop and report to the user. Don't escalate the workaround into something elaborate.

This is especially important for Stage 6 — TextMate grammar bugs can look like rendering bugs elsewhere. Isolate the failure before generalizing the fix.

---

## 0.7. GitHub Pages deployment constraints

GitHub Pages serves the site at `https://<username>.github.io/<repo>/` (a project page under a user-scoped subpath). This subpath reaches into multiple parts of the build:

### Constraint 1: Base href must match the subpath at deploy time, not at build time

`wwwroot/index.html` ships with `<base href="/" />` so the dev server works. The GitHub Actions workflow **rewrites it to `<base href="/<repo-name>/" />`** before publishing. This way the same source works in dev (`/`) and in production (`/<repo-name>/`).

Use the published-and-maintained action `stevesandersonms/ghaction-rewrite-base-href` for this. It's specifically built for Blazor WASM on GitHub Pages. Pin to a SHA, not a tag, for supply-chain safety:

```yaml
- name: Rewrite base href
  uses: SteveSandersonMS/ghaction-rewrite-base-href@v1
  with:
    html_path: publish/wwwroot/index.html
    base_href: /${{ github.event.repository.name }}/
```

Do the same for `publish/wwwroot/404.html`.

### Constraint 2: `.nojekyll` file is mandatory

GitHub Pages uses Jekyll by default. Jekyll **strips any file or folder starting with `_`** — which would delete Blazor's `_framework/` directory containing the .NET runtime and all assemblies. The app would 404 on boot.

Solution: commit an empty file at `wwwroot/.nojekyll`. It's copied into the publish output and tells GitHub Pages to skip Jekyll.

### Constraint 3: SPA routing on a static host needs a 404.html bounce

GitHub Pages serves `index.html` for `/`, but for any other URL like `/lesson/01-basics` it looks for a file at that path, doesn't find one, and serves `404.html`. Blazor's client-side router never gets a chance to handle the URL.

Standard fix: a `404.html` that captures the requested path, redirects to `index.html` with the original path encoded in the query string, and a small inline script in `index.html` that decodes the query and pushes the original path into history so Blazor's router sees it.

`wwwroot/404.html` (this exact pattern, MIT-licensed from rafgraph/spa-github-pages):

```html
<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <title>Redirecting…</title>
  <script>
    // Single Page Apps for GitHub Pages — https://github.com/rafgraph/spa-github-pages
    // This script bounces unknown paths back through index.html, preserving the
    // original URL in the query string so the SPA router can pick it up.
    var pathSegmentsToKeep = 1; // = repo name segment
    var l = window.location;
    l.replace(
      l.protocol + '//' + l.hostname + (l.port ? ':' + l.port : '') +
      l.pathname.split('/').slice(0, 1 + pathSegmentsToKeep).join('/') + '/?/' +
      l.pathname.slice(1).split('/').slice(pathSegmentsToKeep).join('/').replace(/&/g, '~and~') +
      (l.search ? '&' + l.search.slice(1).replace(/&/g, '~and~') : '') +
      l.hash
    );
  </script>
</head>
<body></body>
</html>
```

Add this **inside `<head>` of `wwwroot/index.html`, before the `<base>` tag**:

```html
<script>
  // Single Page Apps for GitHub Pages — companion to 404.html
  (function(l) {
    if (l.search[1] === '/') {
      var decoded = l.search.slice(1).split('&').map(function(s) {
        return s.replace(/~and~/g, '&');
      }).join('?');
      window.history.replaceState(null, null, l.pathname.slice(0, -1) + decoded + l.hash);
    }
  }(window.location));
</script>
```

`pathSegmentsToKeep = 1` because the repo name is the one segment to preserve at the top of the URL.

### Constraint 4: All asset paths must be relative or use the rewritten base

Don't write `/css/app.css` or `/js/editor.js` anywhere — those become absolute and break on the subpath. Use `css/app.css` (relative) or `<base>`-relative paths. Blazor's `_framework/` references and CSS isolation bundles already do this correctly. The custom JS interop in `editor.js` and `progress.js` must follow the same rule.

When the CodeMirror ES modules import each other (e.g., `import { … } from "../lib/codemirror/state.js"`), use relative paths from the importing file. Never absolute.

### Constraint 5: Trimming + Brotli + AOT-off

Publish profile:

- `dotnet publish -c Release` (Release config enables Blazor WASM compression — Brotli + Gzip variants are emitted as `_framework/*.br` and `*.gz` and GitHub Pages serves them with correct `Content-Encoding`).
- Trimming default-on for Blazor WASM in .NET 10. Verify no `<PublishTrimmed>false</PublishTrimmed>` slipped into the csproj.
- AOT stays off (`<RunAOTCompilation>` absent or false) per the §C1 decision — the size increase isn't worth it.

### Constraint 6: GitHub Pages build cache

GitHub Pages CDN caches aggressively. After a deploy, the new version may take a few minutes to propagate or require a hard refresh (Ctrl+F5). This is documented in the `docs/DEPLOYMENT.md` guide so the user doesn't think their deploy is broken.

### Constraint 7: Workflow file location and permissions

`.github/workflows/deploy.yml` — full file the agent generates:

```yaml
name: Deploy to GitHub Pages

on:
  push:
    branches: [main]
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: "pages"
  cancel-in-progress: false

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Install wasm-tools workload
        run: dotnet workload install wasm-tools

      - name: Publish
        run: dotnet publish src/ScribanTutorial/ScribanTutorial.csproj -c Release -o publish

      - name: Rewrite base href in index.html
        uses: SteveSandersonMS/ghaction-rewrite-base-href@v1
        with:
          html_path: publish/wwwroot/index.html
          base_href: /${{ github.event.repository.name }}/

      - name: Rewrite base href in 404.html
        uses: SteveSandersonMS/ghaction-rewrite-base-href@v1
        with:
          html_path: publish/wwwroot/404.html
          base_href: /${{ github.event.repository.name }}/

      - name: Upload Pages artifact
        uses: actions/upload-pages-artifact@v3
        with:
          path: publish/wwwroot

  deploy:
    needs: build
    runs-on: ubuntu-latest
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    steps:
      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v4
```

The user enables this once by going to repo **Settings → Pages → Source: GitHub Actions**.

### Constraint 8: Repo settings the user must configure once

The agent documents these in `docs/DEPLOYMENT.md`:

1. Settings → Pages → Source = **GitHub Actions**
2. Settings → Actions → General → Workflow permissions = **Read and write** (already covered by the workflow's `permissions:` block but check the org-level default isn't more restrictive)
3. Settings → Actions → General → "Allow GitHub Actions to create and approve pull requests" = optional, not needed

### Local pre-flight test (in Stage 10 before pushing)

```powershell
cd src\ScribanTutorial
dotnet publish -c Release -o ..\..\publish

# Simulate the base href rewrite manually for local testing:
(Get-Content ..\..\publish\wwwroot\index.html) -replace '<base href="/" />', '<base href="/scriban-tutorial/" />' |
    Set-Content ..\..\publish\wwwroot\index.html -Encoding utf8NoBOM

# Serve it under a matching path:
cd ..\..
python -m http.server 8080
# Then open: http://localhost:8080/publish/wwwroot/  -- though this isn't quite right
# Better: use 'dotnet serve' or 'npx serve' configured to serve from a subpath
```

A more reliable local pre-flight: use `dotnet serve` with the `--path-base` flag (install with `dotnet tool install --global dotnet-serve`) or just trust the dev server (`dotnet run`) for local testing and the workflow for deploy-time verification.

---

## 1. Tech stack (versions verified May 22, 2026)

| Concern | Package | Version | Notes |
|---|---|---|---|
| OS / shell | Windows 11 / PowerShell 7+ | — | |
| Framework | Blazor WebAssembly standalone | **.NET 10.0 LTS** | Supported until Nov 2028 |
| Blazor WASM | `Microsoft.AspNetCore.Components.WebAssembly` | `10.0.8` | |
| Blazor DevServer | `Microsoft.AspNetCore.Components.WebAssembly.DevServer` | `10.0.8` | `PrivateAssets="all"` |
| Template engine | `Scriban` | `7.2.0` | |
| Markdown | `Markdig` | `1.2.0` | |
| Diff library | `DiffPlex` | `1.7.2` (verify on NuGet) | Diff view on failure |
| Syntax highlighting (build) | `TextMateSharp` | latest stable | Verify on NuGet — used only in the ContentBuilder tool |
| Syntax highlighting (runtime) | CodeMirror 6 | `6.0.2` + lang/state packages | Loaded as ES modules, no npm build pipeline |
| Routing | Built-in Blazor `<Router>` | — | One parameterized lesson page |
| Styling | Plain CSS + scoped `.razor.css` | — | |
| Build tool | `dotnet` SDK | `10.0.x` | |

Before pinning the agent verifies latest stable on https://www.nuget.org/packages/{name}. Do not adopt prerelease.

---

## 2. Pre-flight checks (Windows 11)

```powershell
dotnet --version          # must report 10.x
dotnet --list-sdks
$PSVersionTable.PSVersion
```

If .NET 10 SDK missing, prefer `winget install --id Microsoft.DotNet.SDK.10 --source winget`, else manual installer from https://dotnet.microsoft.com/download/dotnet/10.0. Open a new PowerShell window after install. Never run elevated installers without asking.

```powershell
dotnet workload install wasm-tools
```

No npm/Node required — all browser-side JS dependencies (CodeMirror) are vendored as ES modules to `wwwroot/lib/codemirror/` once and committed.

---

## 3. Final architecture overview

```
┌────────────── Build time (dotnet build) ──────────────┐
│                                                       │
│  tools/ContentBuilder/  (.NET console)                │
│    ├─ scans wwwroot/lessons/**/*.md                   │
│    ├─ Markdig parse with :::example + {#id} extensions│
│    ├─ TextMateSharp colors fenced code blocks         │
│    └─ writes *.html sibling files                     │
│                                                       │
│  Triggered by MSBuild target with file-mtime check    │
│                                                       │
└──────────────────────────┬────────────────────────────┘
                           │ pre-rendered .html
                           ▼
┌──────────────── Runtime (browser) ────────────────────┐
│                                                       │
│  App.razor → <Router>                                 │
│      ├─ "/"                  → Home (course index)    │
│      └─ "/lesson/{LessonId}" → LessonPage             │
│                                                       │
│  Singleton ContentService                             │
│    ├─ InitializeAsync()        → loads manifest.json  │
│    └─ LoadLessonAsync(id)      → lazy, per lesson     │
│                                                       │
│  Singleton ProgressService                            │
│    └─ localStorage CRUD via JS interop                │
│                                                       │
│  Components                                           │
│    ├─ NavMenu (with per-lesson progress indicators)   │
│    ├─ TheoryBlock (renders pre-built .html)           │
│    └─ ExerciseBlock                                   │
│         ├─ CodeMirror 6 editor (Scriban grammar)      │
│         ├─ Scriban runner (cached parsed solutions)   │
│         ├─ DiffPlex diff view on fail                 │
│         └─ Show solution button                       │
│                                                       │
└───────────────────────────────────────────────────────┘
```

---

## 4. File & folder structure

The current working directory (`C:\Users\senio\agents_space\scriban-tutorial\`) IS the project root. There is no enclosing folder. The `.sln` lives directly in this directory.

```
.  (= C:\Users\senio\agents_space\scriban-tutorial\)
├── ScribanTutorial.sln
├── README.md                             ← agent-generated, user-facing
├── SPECIFICATION.md                      ← already present, gitignored or kept committed
├── .gitignore
├── .gitattributes                        ← LF enforcement
├── .github/
│   └── workflows/
│       └── deploy.yml                    ← GitHub Actions, see §0.7
├── docs/
│   ├── AUTHORING_LESSONS.md              ← §15
│   ├── SECURITY.md                       ← §16
│   ├── SCRIBAN_BEST_PRACTICES.md         ← §17
│   └── DEPLOYMENT.md                     ← §0.7 user-facing
├── tools/
│   └── ContentBuilder/
│       ├── ContentBuilder.csproj
│       ├── Program.cs
│       ├── MarkdownRenderer.cs
│       ├── TextMateHighlighter.cs
│       └── grammars/
│           └── scriban.tmLanguage.json   ← full Scriban grammar
└── src/
    └── ScribanTutorial/
        ├── ScribanTutorial.csproj
        ├── Program.cs
        ├── App.razor
        ├── _Imports.razor
        ├── Layout/
        │   ├── MainLayout.razor
        │   ├── MainLayout.razor.css
        │   ├── NavMenu.razor
        │   └── NavMenu.razor.css
        ├── Pages/
        │   ├── Home.razor                ← course index, "/"
        │   ├── Home.razor.css
        │   ├── LessonPage.razor          ← "/lesson/{LessonId}"
        │   ├── LessonPage.razor.css
        │   ├── TheoryBlock.razor
        │   ├── TheoryBlock.razor.css
        │   ├── ExerciseBlock.razor
        │   └── ExerciseBlock.razor.css
        ├── Services/
        │   ├── Models.cs                 ← record DTOs
        │   ├── ContentService.cs         ← singleton, lazy loading
        │   ├── ProgressService.cs        ← localStorage via JS interop
        │   └── TemplateCache.cs          ← pre-parsed Scriban templates
        └── wwwroot/
            ├── index.html                ← skeleton shell + SPA-redirect script, §13 + §0.7
            ├── 404.html                  ← SPA-redirect bounce, §0.7
            ├── .nojekyll                 ← empty, mandatory for GitHub Pages
            ├── css/
            │   ├── app.css
            │   ├── theme-light.css
            │   └── theme-dark.css
            ├── js/
            │   ├── progress.js           ← localStorage helpers
            │   ├── editor.js             ← CodeMirror mount/destroy
            │   └── scriban-language.js   ← CodeMirror stream parser
            ├── lib/
            │   └── codemirror/           ← vendored ES modules
            ├── manifest.json
            └── lessons/
                ├── 01-basics/
                │   ├── 01-theory.md
                │   ├── 01-theory.html    ← generated, gitignored
                │   └── 02-exercises/
                │       ├── hello/
                │       │   ├── 01-description.md
                │       │   ├── 01-description.html  ← generated
                │       │   ├── 02-datamodel.json
                │       │   ├── 03-expected.txt
                │       │   ├── 04-template.txt
                │       │   └── 05-solution.txt
                │       └── member-access/  { same six files }
                ├── 02-filters/   …
                ├── 03-control-flow/  …
                └── 04-assembly/  …
```

Generated `.html` files are **gitignored**. The ContentBuilder regenerates them on each build.

Inside `manifest.json` and any code that references content paths: **forward slashes** (these are URLs).

---

## 5. Manifest schema

`wwwroot/manifest.json` (UTF-8 LF no BOM):

```json
{
  "courseTitle": "Scriban Interactive Training",
  "courseSubtitle": "A hands-on tour of the Scriban templating language",
  "lessons": [
    {
      "id": "01-basics",
      "title": "Basics",
      "theoryPath": "lessons/01-basics/01-theory",
      "exercises": [
        { "id": "hello",         "path": "lessons/01-basics/02-exercises/hello" },
        { "id": "member-access", "path": "lessons/01-basics/02-exercises/member-access" }
      ]
    }
  ]
}
```

Notes:
- `theoryPath` omits the extension — runtime fetches `{theoryPath}.html` (pre-rendered).
- Exercise `path` is a directory; the four/five files inside it have fixed names.

---

## 6. Services contracts

### `Services/Models.cs`

```csharp
namespace ScribanTutorial.Services;

public sealed record Manifest(
    string CourseTitle,
    string CourseSubtitle,
    IReadOnlyList<LessonEntry> Lessons);

public sealed record LessonEntry(
    string Id,
    string Title,
    string TheoryPath,
    IReadOnlyList<ExerciseEntry> Exercises);

public sealed record ExerciseEntry(string Id, string Path);

public sealed record ExerciseContent(
    string DescriptionHtml,
    string DataModelJson,
    string Expected,
    string StarterTemplate,
    string Solution);

public sealed record LessonContent(
    LessonEntry Entry,
    string TheoryHtml,
    IReadOnlyDictionary<string, ExerciseContent> Exercises);

public sealed record ExerciseProgress(
    string ExerciseId,
    bool Passed,
    string LastCode,
    int Attempts,
    DateTimeOffset UpdatedUtc);
```

### `Services/ContentService.cs`

Singleton. Loads manifest eagerly on `InitializeAsync()`, lazy-loads each lesson's content on first request, caches by lesson ID. Idempotent. Errors logged to `Console.Error.WriteLine`.

```csharp
public sealed class ContentService
{
    private readonly HttpClient _http;
    private Task<Manifest>? _manifestTask;
    private readonly Dictionary<string, Task<LessonContent>> _lessonTasks = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ContentService(HttpClient http) => _http = http;

    public Manifest? Manifest { get; private set; }
    public bool IsLoaded => Manifest is not null;

    public Task<Manifest> InitializeAsync() => _manifestTask ??= LoadManifestAsync();

    private async Task<Manifest> LoadManifestAsync()
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        Manifest = await _http.GetFromJsonAsync<Manifest>("manifest.json", opts)
                  ?? throw new InvalidOperationException("manifest.json failed to load");
        return Manifest;
    }

    public async Task<LessonContent> LoadLessonAsync(string lessonId)
    {
        await InitializeAsync();
        await _gate.WaitAsync();
        try
        {
            if (_lessonTasks.TryGetValue(lessonId, out var existing)) return await existing;
            var entry = Manifest!.Lessons.FirstOrDefault(l => l.Id == lessonId)
                ?? throw new KeyNotFoundException($"lesson not found: {lessonId}");
            var task = FetchLessonAsync(entry);
            _lessonTasks[lessonId] = task;
            return await task;
        }
        finally { _gate.Release(); }
    }

    private async Task<LessonContent> FetchLessonAsync(LessonEntry entry)
    {
        var theoryTask = _http.GetStringAsync($"{entry.TheoryPath}.html");
        var exerciseTasks = entry.Exercises.Select(async ex =>
        {
            var basePath = ex.Path;
            var t = await Task.WhenAll(
                _http.GetStringAsync($"{basePath}/01-description.html"),
                _http.GetStringAsync($"{basePath}/02-datamodel.json"),
                _http.GetStringAsync($"{basePath}/03-expected.txt"),
                _http.GetStringAsync($"{basePath}/04-template.txt"),
                _http.GetStringAsync($"{basePath}/05-solution.txt"));
            return (ex.Id, content: new ExerciseContent(t[0], t[1], t[2], t[3], t[4]));
        });
        var theory = await theoryTask;
        var exercises = await Task.WhenAll(exerciseTasks);
        return new LessonContent(
            entry,
            theory,
            exercises.ToDictionary(e => e.Id, e => e.content));
    }
}
```

### `Services/ProgressService.cs`

Singleton. Wraps `localStorage` via JS interop. Exposes:

```csharp
public sealed class ProgressService
{
    private readonly IJSRuntime _js;
    public event Action? Changed;     // raised after every save; NavMenu subscribes

    public ProgressService(IJSRuntime js) => _js = js;

    public ValueTask<ExerciseProgress?> GetAsync(string lessonId, string exerciseId);
    public ValueTask<IReadOnlyDictionary<string, ExerciseProgress>> GetAllForLessonAsync(string lessonId);
    public ValueTask SaveAsync(ExerciseProgress p, string lessonId);
    public ValueTask ResetAsync(string lessonId, string exerciseId);
    public ValueTask ResetAllAsync();
}
```

Storage key: `scriban-tutorial:progress:{lessonId}:{exerciseId}`. After every write the service fires `Changed`. `NavMenu` subscribes and re-renders to update per-lesson indicators.

### `Services/TemplateCache.cs`

Pre-parses known-good solution templates lazily (first time they're requested), caches the parsed `Scriban.Template` object. The user's edited template is parsed fresh on each Submit — only solutions and starter templates benefit from caching.

```csharp
public sealed class TemplateCache
{
    private readonly ConcurrentDictionary<string, Template> _cache = new();
    public Template GetOrParse(string key, string source) =>
        _cache.GetOrAdd(key, _ => Template.Parse(source));
}
```

---

## 7. Program.cs

> **Spec update (2026-05-24, DI scopes):** This snippet registers `HttpClient` as
> **scoped** and `ContentService` as **singleton**. .NET 10's DI container has
> strict scope validation on by default and refuses to construct a singleton that
> consumes a scoped dependency (`ScopedInSingletonException`). Blazor WASM only
> has a single scope per tab anyway, so the build registers `HttpClient` as
> **singleton** instead. Replace `AddScoped(sp => new HttpClient { ... })` with
> `AddSingleton(sp => new HttpClient { ... })`.

```csharp
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ScribanTutorial;
using ScribanTutorial.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddSingleton<ContentService>();
builder.Services.AddSingleton<ProgressService>();
builder.Services.AddSingleton<TemplateCache>();

// Display-only JSON encoder for the data-model panel: passes Unicode through literally
builder.Services.AddSingleton(new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
});

await builder.Build().RunAsync();
```

---

## 8. Routing

`App.razor`:

```razor
<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
        <FocusOnNavigate RouteData="@routeData" Selector="h1" />
    </Found>
    <NotFound>
        <LayoutView Layout="@typeof(MainLayout)">
            <p>Lesson not found.</p>
        </LayoutView>
    </NotFound>
</Router>
```

`Pages/LessonPage.razor`:

```razor
@page "/lesson/{LessonId}"
@inject ContentService Content
@implements IDisposable

@if (_lesson is null) { <div class="loading">Loading…</div> }
else {
    <article class="lesson">
        <header><h1>@_lesson.Entry.Title</h1></header>
        <TheoryBlock Html="@_lesson.TheoryHtml" />
        @foreach (var ex in _lesson.Entry.Exercises) {
            <ExerciseBlock LessonId="@LessonId"
                           ExerciseId="@ex.Id"
                           Content="@_lesson.Exercises[ex.Id]" />
        }
    </article>
}

@code {
    [Parameter] public string LessonId { get; set; } = "";
    private LessonContent? _lesson;

    protected override async Task OnParametersSetAsync()
    {
        _lesson = null;
        StateHasChanged();
        _lesson = await Content.LoadLessonAsync(LessonId);
    }

    public void Dispose() { }
}
```

`Pages/Home.razor` shows the course landing page — title, subtitle, list of lessons with progress indicators, and a "Start" or "Continue" CTA pointing to the first unfinished lesson.

---

## 9. Components

### `TheoryBlock.razor`

Simply renders pre-built HTML:

```razor
<section class="theory">
    <div class="theory-badge">Theory</div>
    @((MarkupString)Html)
</section>

@code {
    [Parameter, EditorRequired] public string Html { get; set; } = "";
}
```

No Markdig at runtime. The HTML already has syntax-highlighted `<span>` tokens from TextMateSharp.

### `ExerciseBlock.razor`

- Header with exercise ID + pass/fail badge.
- Description (pre-rendered HTML).
- Side-by-side panels: **Data model** (JSON shown via `<pre>` with the runtime encoder) and **Expected output** (collapsed behind "Reveal" button).
- **CodeMirror 6 editor** (replaces `<textarea>`), mounted via JS interop on `OnAfterRenderAsync(firstRender: true)`, destroyed in `Dispose`.
- **Submit code** (primary), **Reset**, **Show solution** buttons — always visible.
- Result panel: green on pass, red on fail. On fail, a DiffPlex inline diff between actual and expected.

Runner logic (Scriban 7):

```csharp
using Scriban;
using Scriban.Runtime;
using System.Text.Json;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

private async Task RunTemplate()
{
    _status = Status.Idle;
    _errors = null;
    _output = "";
    _diff = null;
    _attempts++;

    try
    {
        var template = Template.Parse(_userTemplate);
        if (template.HasErrors)
        {
            _errors = string.Join("\n", template.Messages.Select(m => m.ToString()));
            _status = Status.Fail;
            await PersistAsync();
            return;
        }

        using var doc = JsonDocument.Parse(Content.DataModelJson);
        var scriptObject = new ScriptObject();
        ImportJson(doc.RootElement, scriptObject);

        var context = new TemplateContext
        {
            MemberRenamer = member => member.Name,
            // Hard caps to keep the WASM tab responsive — see SECURITY.md
            LoopLimit  = 100_000,
            RecursiveLimit = 100,
        };
        context.PushGlobal(scriptObject);

        _output = await template.RenderAsync(context);

        var actual   = Normalize(_output);
        var expected = Normalize(Content.Expected);
        _status = string.Equals(actual, expected, StringComparison.Ordinal)
            ? Status.Pass : Status.Fail;

        if (_status == Status.Fail)
            _diff = InlineDiffBuilder.Diff(expected, actual);
    }
    catch (Exception ex)
    {
        _errors = ex.Message;
        _status = Status.Fail;
    }
    finally
    {
        await PersistAsync();
    }
}

private static string Normalize(string s) =>
    s.Replace("\r\n", "\n").TrimEnd('\n');

private async Task PersistAsync() =>
    await Progress.SaveAsync(new ExerciseProgress(
        ExerciseId, _status == Status.Pass, _userTemplate, _attempts, DateTimeOffset.UtcNow),
        LessonId);
```

JSON import helpers (`ImportJson`, `ConvertJson`, `BuildObject`) identical to the previous spec — see prior version. Use `JsonDocument` `using` blocks; do not retain large strings beyond the call.

### `NavMenu.razor`

Sidebar with course title, subtitle, list of lesson buttons. Each lesson button shows:

- `01` (number)
- Title
- Indicator: `○` not started · `◐` in progress · `●` complete

Indicator computed by reading `ProgressService.GetAllForLessonAsync(lesson.Id)` once per render. NavMenu subscribes to `ProgressService.Changed` and re-renders on save.

---

## 10. CodeMirror 6 integration

### Vendoring (one-time setup, the agent does this)

Download CodeMirror 6 ES module bundles into `wwwroot/lib/codemirror/`. Two options:

**Option A (preferred):** download a pre-bundled IIFE from https://github.com/codemirror/dev releases or a CDN like esm.sh. One file, no transitive deps.

**Option B:** use a CDN at runtime via `<script type="module" src="https://esm.sh/codemirror@6.0.2">`. Simpler but adds an external dependency the user's machine must reach.

Spec mandates **Option A** for reproducibility. Vendored files committed to the repo.

### `wwwroot/js/scriban-language.js` — stream parser

```javascript
import { StreamLanguage } from "../lib/codemirror/index.js";

const KEYWORDS = new Set([
  "if","else","else if","end","for","in","while","break","continue",
  "func","ret","case","when","with","do","wrap","include","import",
  "readonly","capture","tablerow","this"
]);
const ATOMS = new Set(["true","false","null","empty"]);
const OPERATORS_RE = /^(\?\?|\?\.|==|!=|<=|>=|<|>|&&|\|\||\.\.|[|=+\-*/%^!?:])/;
const BUILTIN_MODULES = new Set([
  "string","array","object","math","date","regex","html","fs","timespan"
]);

export const scribanLanguage = StreamLanguage.define({
  startState: () => ({ inExpr: false, inString: null, inRaw: false }),
  token(stream, state) {
    if (state.inRaw) {
      if (stream.match("}%}")) { state.inRaw = false; return "brace"; }
      stream.next(); return null;
    }
    if (!state.inExpr) {
      if (stream.match("{%{")) { state.inRaw = true; return "brace"; }
      if (stream.match("{{-") || stream.match("{{")) { state.inExpr = "expr"; return "brace"; }
      if (stream.match("{%-") || stream.match("{%")) { state.inExpr = "stmt"; return "brace"; }
      stream.next(); return null;
    }
    // inside delimiter
    if (state.inString) {
      while (!stream.eol()) {
        const c = stream.next();
        if (c === "\\") { stream.next(); continue; }
        if (c === state.inString) { state.inString = null; return "string"; }
      }
      return "string";
    }
    if (stream.match(/^-?\}\}/) || stream.match(/^-?%\}/)) { state.inExpr = false; return "brace"; }
    if (stream.match(/^#[^\r\n]*/)) return "comment";
    if (stream.match(/^"|^'/)) { state.inString = stream.string[stream.pos - 1]; return "string"; }
    if (stream.match(/^`[^`]*`/)) return "string"; // verbatim
    if (stream.match(/^\d+(\.\d+)?([eE][+-]?\d+)?/)) return "number";
    if (stream.match(OPERATORS_RE)) return "operator";
    if (stream.match(/^[(){}\[\],;]/)) return "punctuation";
    if (stream.match(/^\./)) return "punctuation";
    const word = stream.match(/^[A-Za-z_][A-Za-z_0-9]*/);
    if (word) {
      const w = word[0];
      if (KEYWORDS.has(w)) return "keyword";
      if (ATOMS.has(w)) return "atom";
      if (BUILTIN_MODULES.has(w)) return "typeName";
      return "variableName";
    }
    stream.next(); return null;
  },
});
```

### `wwwroot/js/editor.js` — JS interop module

```javascript
import { EditorView, basicSetup } from "../lib/codemirror/index.js";
import { keymap } from "../lib/codemirror/view.js";
import { syntaxHighlighting, HighlightStyle, indentUnit } from "../lib/codemirror/language.js";
import { tags as t } from "../lib/codemirror/highlight.js";
import { scribanLanguage } from "./scriban-language.js";

const editors = new Map();

const lightStyle = HighlightStyle.define([
  { tag: t.keyword,      color: "#af00db" },
  { tag: t.atom,         color: "#0000ff" },
  { tag: t.string,       color: "#a31515" },
  { tag: t.number,       color: "#098658" },
  { tag: t.comment,      color: "#008000", fontStyle: "italic" },
  { tag: t.operator,     color: "#000000" },
  { tag: t.brace,        color: "#a31515", fontWeight: "bold" },
  { tag: t.punctuation,  color: "#000000" },
  { tag: t.variableName, color: "#001080" },
  { tag: t.typeName,     color: "#267f99" },
]);
const darkStyle = HighlightStyle.define([
  { tag: t.keyword,      color: "#c586c0" },
  { tag: t.atom,         color: "#569cd6" },
  { tag: t.string,       color: "#ce9178" },
  { tag: t.number,       color: "#b5cea8" },
  { tag: t.comment,      color: "#6a9955", fontStyle: "italic" },
  { tag: t.operator,     color: "#d4d4d4" },
  { tag: t.brace,        color: "#dcdcaa", fontWeight: "bold" },
  { tag: t.punctuation,  color: "#d4d4d4" },
  { tag: t.variableName, color: "#9cdcfe" },
  { tag: t.typeName,     color: "#4ec9b0" },
]);

export function mount(elementId, initial, dotnetRef, isDark) {
  const view = new EditorView({
    doc: initial,
    parent: document.getElementById(elementId),
    extensions: [
      basicSetup,
      scribanLanguage,
      syntaxHighlighting(isDark ? darkStyle : lightStyle),
      indentUnit.of("  "),
      EditorView.updateListener.of(u => {
        if (u.docChanged) dotnetRef.invokeMethodAsync("OnEditorChange", u.state.doc.toString());
      }),
    ],
  });
  editors.set(elementId, view);
}

export function setValue(elementId, value) {
  const v = editors.get(elementId);
  if (!v) return;
  v.dispatch({ changes: { from: 0, to: v.state.doc.length, insert: value } });
}

export function destroy(elementId) {
  editors.get(elementId)?.destroy();
  editors.delete(elementId);
}
```

`ExerciseBlock.razor.cs` calls these via `IJSRuntime.InvokeAsync`. Editor mounts in `OnAfterRenderAsync(firstRender: true)`, destroys in `Dispose`. The .NET ↔ JS callback uses `DotNetObjectReference<ExerciseBlock>` so the editor pushes content updates back to C# state.

---

## 11. The ContentBuilder tool

### `tools/ContentBuilder/ContentBuilder.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Markdig" Version="1.2.0" />
    <PackageReference Include="TextMateSharp" Version="LATEST_STABLE" />
  </ItemGroup>
</Project>
```

### Responsibilities

1. Accepts arguments: `--input <wwwroot/lessons>` `--grammar <path/to/scriban.tmLanguage.json>` `--theme <light|dark>`.
2. Recursively finds `*.md` files.
3. For each, parses with a configured Markdig pipeline:
   ```csharp
   var pipeline = new MarkdownPipelineBuilder()
       .UsePipeTables()
       .UseAutoLinks()
       .UseEmphasisExtras()
       .UseCustomContainers()       // for :::example
       .UseGenericAttributes()      // for {#id} on headings
       .Build();
   ```
4. Walks the AST. For each `FencedCodeBlock`, invokes TextMateSharp with the language hint (`scriban`, `json`, `text`, etc.). Replaces the raw block text with `<span>`-tokenized HTML.
5. For each `CustomContainer` with class `example`, emits the side-by-side layout HTML (see §12).
6. Writes the result next to the source as `*.html` (e.g., `01-theory.md` → `01-theory.html`).
7. Uses file mtime comparison to skip unchanged files (`File.GetLastWriteTimeUtc`).

### MSBuild integration

In `src/ScribanTutorial/ScribanTutorial.csproj`:

```xml
<Target Name="BuildContent" BeforeTargets="Build">
  <Exec Command="dotnet run --project ..\..\tools\ContentBuilder -- --input wwwroot\lessons --grammar ..\..\tools\ContentBuilder\grammars\scriban.tmLanguage.json --theme light" />
</Target>
```

The ContentBuilder itself does the staleness check. The MSBuild target always invokes it; the tool decides what to regenerate.

---

## 12. `:::example` custom container

### Markdown source

````markdown
:::example
```scriban
Hello, {{ name | string.upcase }}!
```
```text
Hello, SERGEI!
```
:::
````

Optional third block (data model):

````markdown
:::example
```scriban
{{ user.first_name }} {{ user.last_name }}
```
```json
{ "user": { "first_name": "Ada", "last_name": "Lovelace" } }
```
```text
Ada Lovelace
```
:::
````

### Output HTML

```html
<div class="example">
  <div class="example__col example__col--in">
    <div class="example__label">Template</div>
    <pre><code class="language-scriban"><span class="…">…highlighted spans…</span></code></pre>
  </div>
  <div class="example__col example__col--data">
    <div class="example__label">Data</div>
    <pre><code class="language-json">…</code></pre>
  </div>
  <div class="example__col example__col--out">
    <div class="example__label">Output</div>
    <pre><code class="language-text">…</code></pre>
  </div>
</div>
```

CSS lays out the panels horizontally on wide screens, vertically below 720px. ContentBuilder is responsible for emitting this layout from a `CustomContainer` block whose info string is `example`.

---

## 13. `wwwroot/index.html` skeleton shell

The visible loading state must show structure, not a bare "Loading…" line. The shell renders the sidebar + main column placeholders so users see the layout within 100 ms:

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width" />
  <title>Scriban Interactive Training</title>
  <link rel="stylesheet" href="css/app.css" />
  <link rel="stylesheet" href="ScribanTutorial.styles.css" />
</head>
<body>
  <div id="app">
    <div class="boot-shell">
      <aside class="boot-shell__nav">
        <div class="boot-shell__brand-skel"></div>
        <div class="boot-shell__nav-skel"></div>
        <div class="boot-shell__nav-skel"></div>
        <div class="boot-shell__nav-skel"></div>
      </aside>
      <main class="boot-shell__main">
        <div class="boot-shell__title-skel"></div>
        <div class="boot-shell__body-skel"></div>
        <div class="boot-shell__body-skel"></div>
      </main>
    </div>
  </div>
  <script src="_framework/blazor.webassembly.js"></script>
</body>
</html>
```

The `.boot-shell__*` skeleton elements use a subtle shimmer animation. They get removed automatically when Blazor renders into `#app`.

---

## 14. Acceptance criteria

The build is complete when **all** are true. Report status on each.

1. `dotnet build` from the solution root completes with **zero errors and zero warnings**. The ContentBuilder runs as part of this and generates `.html` siblings.
2. `dotnet run --project src\ScribanTutorial` starts the dev server and prints a `http://localhost:xxxx` URL.
3. Opening that URL in Edge shows the loading shell within 100 ms and the rendered course shortly after.
4. The sidebar lists all four lessons with progress indicators (all `○` initially).
5. Clicking a lesson navigates to `/lesson/{id}` and renders theory + exercises. URL bar updates.
6. DevTools Network tab on first load shows **one** `manifest.json` request. Navigating to a lesson triggers fetches for that lesson's files only — no eager fetching of other lessons.
7. Theory blocks show **syntax-highlighted** code (TextMateSharp tokens). `:::example` containers render as side-by-side panels.
8. Exercise editors are CodeMirror 6, with Scriban syntax highlighting as the user types.
9. For each exercise:
   - Starter template loads.
   - Correct solution → green "Passed" + progress indicator updates to `●` (or `◐` if other exercises remain).
   - Wrong output → red "Failed" + DiffPlex diff view shown.
   - Parse error → red "Failed" + parser messages shown.
   - "Show solution" button reveals `05-solution.txt` inline.
   - "Reset" restores `04-template.txt`.
10. Refreshing the page restores: last code in the editor, pass/fail state, attempt count. (`localStorage` persistence works.)
11. Theme toggle (light/dark) flips theme; choice persists across refreshes.
12. UTF-8 / Unicode: a data model with Cyrillic content renders the JSON panel readably (not as `\u04XX` escapes).
13. PowerShell content-discipline check finds no lesson prose in source files:
    ```powershell
    Select-String -Path "src\ScribanTutorial\**\*.razor","src\ScribanTutorial\**\*.cs" `
                  -Pattern "Hello, Sergei|Ada Lovelace|WIDGET|ADMIN|Order #42" -SimpleMatch
    # → empty
    ```
14. No CRLF in content files:
    ```powershell
    Get-ChildItem src\ScribanTutorial\wwwroot\lessons -Recurse -Include *.md,*.json,*.txt |
        Where-Object { (Get-Content $_ -Raw -AsByteStream) -contains 13 }
    # → empty
    ```
15. Fresh-clone test: `git clone` (or copy) to a new directory, `dotnet build` + `dotnet run` work with no manual steps beyond the SDK.
16. `dotnet publish -c Release -o publish` succeeds. `publish/wwwroot/` contains `index.html`, `404.html`, `.nojekyll`, `_framework/`, and all `lessons/**/*.html` pre-rendered content. The size of `_framework/` is reasonable (under ~15 MB uncompressed, ~5 MB Brotli).
17. `.github/workflows/deploy.yml` exists with the workflow per §0.7. `.nojekyll` and `404.html` exist in `wwwroot/`. `index.html` contains the SPA-redirect script in `<head>`.
18. (Post-deploy, in Stage 10) The deployed site loads at `https://<username>.github.io/<repo>/`. Direct link to a lesson URL (`/lesson/01-basics`) works on first hit (no 404). Browser console is free of errors. All exercises pass their solutions on the deployed version.

---

## 15. Authoring guide (the agent generates `docs/AUTHORING_LESSONS.md`)

The guide explains, for someone who is not a developer:

### Quick start: add a new exercise

1. Pick a lesson folder under `src/ScribanTutorial/wwwroot/lessons/`, e.g., `02-filters/`.
2. Create a new directory inside `02-exercises/`: e.g., `02-filters/02-exercises/strip-whitespace/`.
3. Create five files in that directory (UTF-8, LF endings, no BOM):
   - `01-description.md` — what the exercise asks. Includes expected output in a fenced ```text``` block.
   - `02-datamodel.json` — the data the template receives.
   - `03-expected.txt` — exact expected output, byte-for-byte.
   - `04-template.txt` — starter template with `???` placeholders.
   - `05-solution.txt` — the known-good solution.
4. Add an entry to `wwwroot/manifest.json` under the lesson's `exercises` array:
   ```json
   { "id": "strip-whitespace", "path": "lessons/02-filters/02-exercises/strip-whitespace" }
   ```
5. Run `dotnet build` — the content builder picks up the new files.
6. Reload the app — the new exercise appears.

### Adding a whole new lesson

1. Create the lesson directory: `wwwroot/lessons/05-functions/`.
2. Create `01-theory.md` and an empty `02-exercises/` subdirectory.
3. Add at least one exercise (above).
4. Add the lesson to `manifest.json` `lessons` array (preserving order).
5. Build. Reload.

### Theory markdown conventions

- Headings: `# Top`, `## Section`, `### Subsection`. Anchored automatically.
- Code examples: prefer the `:::example` block (template + output side by side):

  ````markdown
  :::example
  ```scriban
  {{ user.name | string.upcase }}
  ```
  ```text
  ADA
  ```
  :::
  ````

- Optional middle data block (template + data + output):

  ````markdown
  :::example
  ```scriban
  {{ user.first_name }} {{ user.last_name }}
  ```
  ```json
  { "user": { "first_name": "Ada", "last_name": "Lovelace" } }
  ```
  ```text
  Ada Lovelace
  ```
  :::
  ````

- Standalone fenced blocks (without the container) also render highlighted — use them for inline references inside prose.

### File rules (critical)

- **Encoding:** UTF-8 with no BOM. Line endings: LF.
- **JSON:** must parse cleanly. Quote keys.
- **Expected output:** byte-exact. The runner normalizes CRLF→LF and trims one trailing newline before comparison, but otherwise everything counts (spaces, tabs, trailing whitespace).
- **Template files:** plain UTF-8 text, Scriban syntax. Multiline OK.

### Editing with VS Code on Windows

VS Code settings to enforce the file format:
```json
{
  "files.encoding": "utf8",
  "files.eol": "\n",
  "files.insertFinalNewline": false,
  "files.trimTrailingWhitespace": false
}
```

(Trim trailing whitespace is off because `03-expected.txt` may legitimately contain trailing spaces in some exercises.)

### Verifying a new exercise

Before committing, the agent or author runs the verification harness:

```powershell
cd tools\ContentBuilder
dotnet run -- --verify ..\..\src\ScribanTutorial\wwwroot\lessons\02-filters\02-exercises\strip-whitespace
```

The tool parses `05-solution.txt` with the configured data model and confirms the output equals `03-expected.txt` byte-for-byte. If they differ, it prints a diff. Fix the expected output (or the solution) and retry.

The `--verify` subcommand is a required feature of ContentBuilder. The agent implements it during this build.

### Manifest field reference

| Field | Type | Required | Description |
|---|---|---|---|
| `courseTitle` | string | yes | Top of sidebar |
| `courseSubtitle` | string | yes | Below title |
| `lessons[]` | array | yes | Order matters |
| `lessons[].id` | string | yes | URL slug, must match folder name |
| `lessons[].title` | string | yes | Display name |
| `lessons[].theoryPath` | string | yes | Path without extension; runtime fetches `.html` |
| `lessons[].exercises[]` | array | yes | Order matters |
| `lessons[].exercises[].id` | string | yes | Stable ID for progress tracking — avoid renaming |
| `lessons[].exercises[].path` | string | yes | Path to exercise directory (no trailing slash) |

---

## 16. Security guide (the agent generates `docs/SECURITY.md`)

### Threat model

This app **evaluates user-supplied Scriban templates in the user's own browser**. The "attacker" is the user, and the victim is the user's own browser tab. Standard server-side template-injection threats do not apply. But the local execution model has real concerns:

### 1. CPU/memory denial of service via malicious templates

A user can deliberately or accidentally write a template that consumes unbounded resources:

```scriban
{{ for i in 1..999999999 }}{{ i }}{{ end }}
```

```scriban
{{ func loop; loop; ret; end; loop }}
```

```scriban
{{ "x" | string.append "x" | string.append "x" | … }}   # produces 2^N bytes
```

**Mitigations baked into the runner (§9):**

- `TemplateContext.LoopLimit = 100_000` — Scriban aborts loops past this iteration count.
- `TemplateContext.RecursiveLimit = 100` — caps recursion depth.
- Total render time is naturally capped by browser tab CPU budget; a runaway template will freeze only that tab.

**Known upstream advisory:** Scriban's recursive-descent parser can throw `StackOverflowException` on deeply nested expressions (e.g., `((((((…))))))` thousands deep). In .NET, stack overflow is **not catchable** — it tears down the WASM runtime. The browser tab will need a reload. This is a Scriban limitation, not fixable in our code. Mitigation: we don't deploy this as a multi-tenant service; one user's bad template only crashes their own tab.

### 2. Cross-site scripting (XSS)

The app renders Markdown that we control (lesson content) and JSON that we control (data models). Both go into the DOM. Risks:

- **Theory HTML:** rendered via `@((MarkupString)Html)`. Markdig output is HTML; if a theory `.md` file contained `<script>`, it would execute. **Mitigation:** all `.md` files are author-controlled and committed to the repo; PRs are reviewed. We are not rendering user input.
- **JSON data model display:** rendered as text inside `<pre><code>`, not as HTML. Blazor's `@expression` HTML-escapes by default. Safe.
- **User template:** never inserted into the DOM. Only fed to Scriban and the editor.

The `UnsafeRelaxedJsonEscaping` encoder we use for the data-model panel is safe in this context (see §D8 in the design notes): the JSON becomes text inside a `<pre>` element, not embedded in `<script>` or HTML attributes. If a future change moves JSON into an HTML attribute or a `<script>` block, switch to the default encoder.

### 3. `localStorage` privacy

User progress is stored under keys `scriban-tutorial:progress:*`. Visible to any other JS on the same origin. Acceptable because:
- No PII, no credentials.
- The app is the only thing served from this origin.
- A "Reset all progress" button is available in settings.

### 4. Third-party JS (CodeMirror)

CodeMirror is vendored locally (`wwwroot/lib/codemirror/`), not loaded from a CDN. No supply-chain risk at runtime. Updates require a deliberate vendoring step. Pin version in `wwwroot/lib/codemirror/VERSION.txt` for auditability.

### 5. Dependencies

Monitor `dotnet list package --vulnerable` quarterly. Current pins:
- Scriban 7.2.0
- Markdig 1.2.0
- DiffPlex 1.7.x
- TextMateSharp (latest stable)

### 6. If you ever deploy this as a multi-user service

Don't, without these changes:
- Render Scriban server-side or in a sandboxed worker with hard time and memory limits enforced by the host (not by Scriban).
- Cap input size at the gateway (e.g., 4 KB templates, 8 KB data models).
- Rate-limit per IP.
- Disable `EnableRelaxedTargetAccess`, `EnableRelaxedMemberAccess`, `EnableRelaxedFunctionAccess`, `EnableRelaxedIndexerAccess` on `TemplateContext`.
- Restrict which built-in modules are imported into the context (don't push `fs` or `regex` globally — they're attack surface).

The current configuration is appropriate **only** for the single-user local-browser execution model.

---

## 17. Scriban best practices guide (the agent generates `docs/SCRIBAN_BEST_PRACTICES.md`)

This is a reference for the **course authors**, not the agent. It teaches what to teach. Sections to write:

### Naming and casing

- Scriban does case-sensitive identifier matching by default but renames .NET members to snake_case unless the host changes `MemberRenamer`. Our host preserves original names — but learners may use other Scriban hosts where renaming differs. Teach the safe pattern: in JSON data models, use snake_case keys (`first_name`, not `firstName`); in templates, match those keys exactly.

### Whitespace control

- `{{- expr -}}` strips whitespace and newlines on the indicated side. Without it, every block tag (`{{ for }}`, `{{ end }}`) leaves a blank line in output. Always teach learners to plan for whitespace explicitly in any non-trivial template.
- Common pattern for clean list rendering:
  ```scriban
  {{- for item in items -}}
  - {{ item }}
  {{ end -}}
  ```

### `if`/`for` patterns

- Use `else if` not nested `if ... end ... if`.
- `for x in list` exposes `for.index`, `for.first`, `for.last`, `for.changed` inside the loop. Don't reinvent loop counters.
- Prefer `for.last` for conditional trailing separators:
  ```scriban
  {{- for x in items -}}{{ x }}{{ if !for.last }}, {{ end }}{{- end -}}
  ```

### Filters

- Pipe filters left-to-right: `{{ s | string.strip | string.upcase }}`.
- Filter modules: `string.*`, `array.*`, `object.*`, `math.*`, `date.*`, `regex.*`, `html.*`.
- `array.size`, `string.size`, `object.keys` — common, teach them early.
- `math.format` for number formatting; `date.to_string` for dates.

### Functions

- Define with `func name; ...; end`. Returns last expression or explicit `ret value`.
- Closures capture surrounding scope. Use sparingly in templates — easier to read with a flatter structure.
- Anonymous functions via `do ... end` blocks for passing to higher-order filters.

### Includes and partials

- `{{ include "header.scriban" }}` — only works if a template loader is configured on the host. Our tutorial host doesn't configure one; learners can read about includes but exercises won't use them.

### Common gotchas to warn learners about

1. **`if x` is truthy for any non-null, non-false, non-empty-string value.** Empty arrays are still truthy in Scriban — use `if array.size x > 0`.
2. **`for` over a null variable silently does nothing.** No error, no output. If an exercise expects output, missing data won't fail loudly. Teach defensive checks.
3. **Pipe chains break on newlines unless wrapped in parens or expressed on one line.** Multi-line filter chains are valid but visually surprising.
4. **`string.capitalize` capitalizes only the first letter** — does not title-case multi-word strings. Use `string.capitalizewords` for that.
5. **Numbers vs strings:** `{{ "5" + 3 }}` is `"53"` (string concat). `{{ 5 + 3 }}` is `8`. JSON `"qty": "4"` vs `"qty": 4` matters.
6. **Member access on missing properties returns null** by default, which renders as empty string. Bugs in data models silently produce empty output instead of errors. Teach: if a template renders nothing, check the data model first.

### Designing exercises

- Each exercise should test exactly one concept.
- Starter templates should be *close enough* to correct that the missing piece is unambiguous. Use `???` for the blank.
- Expected outputs should be short and visually distinctive — long expected outputs hide off-by-one whitespace bugs.
- Always include a `05-solution.txt`. Authors verify with the `--verify` subcommand before committing.
- Avoid exercises that depend on Scriban host configuration (renamers, custom functions, includes) — they don't transfer to learners' future projects.

### Advanced topics for later lessons

- `tablerow` for grid layouts (cycles `for.index` across columns).
- `wrap` for content wrappers (template-as-function pattern).
- `capture name; ...; end` for storing rendered output in a variable.
- `case x; when 1; ...; when 2; ...; else; ...; end` — pattern matching.
- `with object; ...; end` — implicit member access without dotting.
- `regex.match`, `regex.replace` — be careful with greedy matches in lesson examples.

### What the official docs cover that exercises should also cover (mapping to https://scriban.github.io/docs/language/)

The agent reads the official language doc and ensures the course skeleton in §8 of the build covers at least: comments, escapes, expressions, variables, properties, indexers, function calls, math, comparison, logic, string concat, range, array init, object init, pipe operators, assignments, blocks, `if`, `case`/`when`, `for`, `while`, `break`/`continue`, `capture`, `func`, `with`, `wrap`, `include`, whitespace control. Each gets at least one example in a theory block and at least one exercise where applicable. Existing four lessons cover the basics; the agent extends the manifest with stub lessons to cover the rest if asked, but the V2 build can ship with the original four and an issue list of follow-up lessons.

---

## 18. What the agent should **not** do

- Don't add authentication, accounts, or a backend.
- Don't add a database.
- Don't pull in a CSS framework, Tailwind, or Bootstrap.
- Don't pull in a JS SPA framework or bundler (no npm, no webpack, no vite).
- Don't hardcode lesson prose in `.cs` / `.razor`.
- Don't skip verification — the ContentBuilder `--verify` step runs against every `05-solution.txt` before the build is declared complete.
- Don't `sudo` or run elevated installers without asking.
- Don't enable AOT compilation.
- Don't downgrade to .NET 8 or earlier.
- Don't adopt prerelease NuGet versions.
- Don't fetch CodeMirror or other JS from a CDN at runtime — vendor locally.
- Don't use `>` or default `Out-File` in PowerShell to write content files — encoding/BOM will break comparison. Use `[System.IO.File]::WriteAllText(path, content, [System.Text.UTF8Encoding]::new($false))` or PowerShell 7's `Set-Content -Encoding utf8NoBOM` with explicit LF content.
- Don't use Windows backslash paths inside `manifest.json` or any URL/path field in code — those are URLs.
- Don't hardcode absolute paths (starting with `/`) anywhere — they break the GitHub Pages subpath. Always relative, or via `<base>`.
- Don't omit `.nojekyll` from `wwwroot/` — without it, Jekyll deletes `_framework/` and the deployed app 404s.
- Don't hardcode the repo name anywhere — the workflow injects it via `${{ github.event.repository.name }}` at deploy time.
- Don't put deployment secrets (PAT, OAuth tokens) in the repo. GitHub Actions has the right permissions via `GITHUB_TOKEN`. None needed beyond that.

---

## 19. Reporting back

Final message includes:
1. URLs to open: `http://localhost:xxxx/` (local course index) and the GitHub Pages URL (post-deploy).
2. One-line status of each acceptance criterion in §14.
3. Any deviations from this spec and why.
4. Confirmation that the four guides (`AUTHORING_LESSONS.md`, `SECURITY.md`, `SCRIBAN_BEST_PRACTICES.md`, `DEPLOYMENT.md`) are written.
5. The PowerShell command to re-run locally: `cd src\ScribanTutorial; dotnet run`.
6. Steps the user needs to take to deploy: create repo, push, enable GitHub Pages in Settings → Pages → Source: GitHub Actions.
