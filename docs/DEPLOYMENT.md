# Deployment

The site is hosted on **GitHub Pages** at
<https://sergeiosipov.github.io/scriban-tutorial/>. CI builds and publishes on
every push to `main`. Pull requests run the same build + test + vulnerability
gate but skip the publish / deploy steps, so a broken PR is caught before merge
without spending the publish budget.

## How the pipeline works

`.github/workflows/deploy.yml` does:

1. `actions/checkout` — clone the repo.
2. `actions/setup-dotnet` — install the .NET 10 SDK.
3. `dotnet workload install wasm-tools` — required for the WASM publish.
4. `dotnet restore ScribanTutorial.slnx` — restore the whole solution so the
   ContentBuilder project's `obj/` is populated before publish triggers the
   `BuildContent` MSBuild target.
5. `dotnet build src/ScribanTutorial/...` — fires `BuildContent` so the
   `.html` and `bundle.json` siblings exist before tests run.
6. `dotnet test tests/ScribanTutorial.Tests/...` — gates the deploy on
   `ExerciseSolutionTests` (every canonical solution renders to expected),
   plus the build-target and content-builder smoke tests.
7. `dotnet publish src/ScribanTutorial/ScribanTutorial.csproj -c Release -o publish`
   — produces `publish/wwwroot/` with the WASM bundle, the `_framework/`
   directory, the static lesson assets (including the pre-rendered
   `lessons/**/*.html`), `index.html`, `404.html`, and `.nojekyll`.
8. **`SteveSandersonMS/ghaction-rewrite-base-href`** rewrites
   `<base href="/" />` to `<base href="/scriban-tutorial/" />` in both
   `index.html` and `404.html`. This is the linchpin: GitHub Pages serves the
   site under a subpath, and Blazor's runtime, asset paths, and SPA routing
   all need the base href to match.
9. `actions/upload-pages-artifact` packages `publish/wwwroot/`.
10. `actions/deploy-pages` publishes.

Every third-party action above is pinned to a full commit SHA (with the
human-readable `# v4` tag in a comment). [Dependabot](../.github/dependabot.yml)
watches the actions and opens a PR when an upstream tag moves to a new SHA.

Total run time on a clean cache: ~3–5 minutes. With warm package cache: 1–2.

## One-time GitHub configuration

1. Go to
   <https://github.com/sergeiosipov/scriban-tutorial/settings/pages>.
2. Under **Build and deployment**, set **Source** to **GitHub Actions**.
3. The choice is sticky as soon as you click it; there's no Save button.

You also want **Settings → Actions → General → Workflow permissions** at "Read
and write". The workflow's `permissions:` block grants what's needed, but an
org-level restriction more strict than that would block the deploy.

## Why each weird file exists

| File | Why |
|---|---|
| `wwwroot/.nojekyll` | GitHub Pages runs Jekyll by default, which strips folders starting with `_`. Without this file, `_framework/` (the entire .NET runtime + assemblies) gets deleted and the deployed app 404s on boot. |
| `wwwroot/404.html` | GitHub Pages serves `404.html` for any unknown path. The script inside reads the requested URL, redirects to `index.html?/<path>`, and lets the SPA companion script in `index.html` push the original path into history. Without this, a direct link to `/lesson/01-blocks` returns a real 404. |
| SPA-redirect script in `<head>` of `index.html` | Companion to the 404 bounce. Decodes the `?/<path>` query and `history.replaceState` it back to the original URL so the Blazor router sees the right path. |
| Rewritten base href | At dev time the base is `/`. On Pages it has to be `/scriban-tutorial/` so `<base>`-relative URLs resolve correctly under the subpath. The workflow rewrites this rather than us hardcoding it. |

## Boot pipeline: Brotli, service worker, PWA

Three additions in `wwwroot/` speed up boot on GitHub Pages. They live entirely
on the client side — the workflow doesn't change.

### Serving the `.br` files via `loadBootResource`

GitHub Pages stores the publish output's `*.br` siblings but never serves them
(no `Content-Encoding` negotiation), so by default they're dead weight. The
Blazor script tag in `index.html` carries `autostart="false"`, and `js/boot.js`
starts Blazor with a `loadBootResource` callback that fetches `<asset>.br` for
every boot resource except the runtime's own JS modules and decodes it
client-side with `js/brotli-decode.min.js` (Google's decoder, vendored — the
exact source commit and MIT licence note are in the file header). This is the
official "host Blazor WebAssembly on GitHub Pages" pattern.

Dev builds have no `.br` siblings: the first `.br` fetch doubles as a one-shot
probe, and when it misses, everything falls back to default loading. So a local
Debug run boots normally with at most one failed request in the network tab.

One subtlety worth knowing when touching `index.html`: with
`OverrideHtmlAssetPlaceholders` enabled (csproj), the publish output ships
`_framework` files **only under fingerprinted names** — there is no physical
`blazor.webassembly.js` or `dotnet.js`. The `#[.{fingerprint}]`-style
placeholders in `index.html` cover the Blazor script tag, and the
`dotnet.js` `<link rel="modulepreload" id="dotnet-js-preload">` does double
duty: `boot.js` reads its substituted href and returns it from
`loadBootResource` for the `dotnetjs` resource type, because the loader's
built-in fallback imports the stable `./dotnet.js` name (it normally relies
on a build-injected import map, which our no-inline-scripts CSP forbids).
The SDK substitutes placeholders only for `.js`/`.mjs` assets — a `.wasm` or
`.css` placeholder is silently stripped, so don't add one.

### The hand-rolled service worker

Pages serves everything with `Cache-Control: max-age=600`, so each revisit
after ten minutes re-validates ~50 boot requests. `wwwroot/service-worker.js`
(registered by `boot.js`; skipped on `localhost` so dev refreshes are never
stale) caches in three tiers:

1. **cache-first** — fingerprinted URLs (`_framework/*` content hashes and
   the `.br` variants `boot.js` fetches). Content-addressed, so effectively
   immutable.
2. **stale-while-revalidate** — the content tier: `manifest.json`,
   `search-index.json`, `reference.json`, `ScribanTutorial.styles.css`,
   `lessons/**`, `css/js/lib`, and any stable-named `_framework` files.
   Instant from cache, refreshed in the background; at most one deploy
   behind.
3. **network-first** — navigations only, with the cached shell as offline
   fallback. Responses pass through untouched, so the `404.html` → `?/path`
   bounce keeps working. After a first visit the app works offline.

To bust it, bump the `CACHE_VERSION` constant at the top of
`service-worker.js`: the byte change triggers a reinstall, and the activate
handler (`skipWaiting` + `clients.claim`) deletes the old versioned caches on
the next load.

### PWA manifest

`wwwroot/site.webmanifest` (+ `<link rel="manifest">` in `index.html`) makes
the site installable. It's deliberately *not* named `manifest.json` — that
name is taken by the course manifest the app fetches at startup.

## Cache behaviour after a deploy

GitHub Pages' CDN can hold the previous version for a few minutes after a
successful deploy. If you push, the workflow goes green, and the live site
still shows old content: hard-refresh (Ctrl+F5), and wait 1–3 minutes if it's
still old. This is not a code bug — it's CDN propagation.

## Troubleshooting

**"The deployed app shows the boot shell and never loads."**
Usually a base-href issue. Open DevTools → Network on the deployed site and
look for 404s under `_framework/`. If they're at `/dotnet.runtime.js` instead
of `/scriban-tutorial/dotnet.runtime.js`, the rewrite step didn't run. Check
the workflow run logs.

**"A deep link 404s."**
The 404.html bounce isn't in place. Make sure `wwwroot/404.html` exists and
contains the SPA-redirect script, and that the companion snippet lives in
`<head>` of `index.html` *before* `<base>`.

**"`_framework/` returns 404 for everything."**
Missing `.nojekyll`. The file must exist at `wwwroot/.nojekyll` (zero bytes is
fine).

**"Workflow can't write to Pages."**
Pages source not set to "GitHub Actions" in Settings → Pages, or org-level
workflow permissions are read-only. Both fixable from repo settings.

## Local pre-flight test

Before pushing a deployment-affecting change, build the publish bundle and
poke at it:

```powershell
dotnet publish src\ScribanTutorial\ScribanTutorial.csproj -c Release -o publish
Get-ChildItem publish\wwwroot | Select-Object Name, Length
Get-ChildItem publish\wwwroot\_framework -Filter "*.br" | Measure-Object -Sum Length
```

You want:

- `index.html`, `404.html`, `.nojekyll`, `_framework/`, `lessons/**/*.html`
  all present.
- Brotli (`*.br`) variants of the framework files exist and are roughly half
  the size of the uncompressed versions. GitHub Pages does *not* serve them
  via `Content-Encoding` negotiation — `js/boot.js` fetches and decodes them
  explicitly (see "Boot pipeline" above).

The local publish output uses `<base href="/" />`. The workflow step rewrites
it to `/scriban-tutorial/` only on the CI side, so don't expect the local
`publish/wwwroot/` to work when served from a `/scriban-tutorial/` subpath —
that's CI-only by design.

## Repo settings checklist

- [x] **Settings → Pages → Source: GitHub Actions** (one-time)
- [x] **Settings → Actions → General → Workflow permissions: Read and write**
- [ ] **Settings → Actions → Allow GitHub Actions to create and approve pull requests** — not needed for this workflow; leave unchecked.

## No secrets needed

The workflow uses the default `GITHUB_TOKEN` (granted via the `permissions:`
block). There are no PATs, OAuth tokens, or third-party credentials to manage.
