# Security

## Threat model

This app **evaluates user-supplied Scriban templates in the user's own
browser**. The "attacker" is the user; the victim is the same user's browser
tab. Standard server-side template-injection threats don't apply — there's no
server. But the local execution model has its own concerns.

## 1. CPU / memory denial of service via malicious templates

A user can deliberately or accidentally write a template that consumes
unbounded resources:

```scriban
{{ for i in 1..999999999 }}{{ i }}{{ end }}
```

```scriban
{{ func loop; loop; ret; end; loop }}
```

```scriban
{{ "x" | string.append "x" | string.append "x" | ... }}   # produces 2^N bytes
```

**Mitigations baked into the runner** (see
[`Services/ScribanRunner.cs`](../src/ScribanTutorial/Services/ScribanRunner.cs),
the single source of truth used by ExerciseBlock, Playground, and the
build-time `--verify` tool):

- `TemplateContext.LoopLimit = 100_000` — Scriban throws after this many
  loop iterations.
- `TemplateContext.RecursiveLimit = 100` — caps recursion depth.
- Output capped at 250 KB per render. `LoopLimit` stops a runaway counter
  but doesn't stop a template that emits 50 KB per iteration; the post-render
  truncation keeps the worst case bounded.
- Total render time is naturally capped by the browser tab's CPU budget. A
  runaway template freezes only that tab; other tabs and the OS are fine.

### Known upstream limitation

Scriban's recursive-descent parser can throw `StackOverflowException` on
deeply nested expressions (e.g. `((((((…))))))` thousands deep). On .NET, a
`StackOverflowException` is **not catchable** — it tears down the WASM
runtime. The user has to reload the tab. This is a Scriban limitation, not
something we can mitigate. Acceptable because this is single-user
local-browser execution.

## 2. Cross-site scripting (XSS)

The app puts two kinds of content into the DOM:

- **Theory HTML** — rendered through `@((MarkupString)Html)`. Markdig output
  is HTML; left to itself, a `.md` file containing `<script>` would execute.
  **Mitigations** (defence-in-depth):
  1. Every `.md` is author-controlled and PR-reviewed.
  2. [`MarkdownRenderer.Render`](../tools/ContentBuilder/MarkdownRenderer.cs)
     passes the Markdig output through `Ganss.Xss.HtmlSanitizer` at build
     time, stripping `<script>`, `<iframe>`, `<object>`, `<embed>`, `on*=`
     handlers, and `javascript:` URLs. So even if a malicious snippet slipped
     past review, it never reaches the deployed `.html`.
     [`ContentBuilderTests.MarkdownRenderer_strips_dangerous_html_from_author_content`](../tests/ScribanTutorial.Tests/ContentBuilderTests.cs)
     gates this.
- **JSON data-model display** — rendered as text inside `<pre><code>`. Blazor's
  `@expression` HTML-escapes by default. Safe.
- **The user's template** — never inserted into the DOM. Only fed to Scriban
  and to the CodeMirror editor.

The `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` we use to pretty-print the
data-model panel is safe **in this context**: the JSON becomes text inside a
`<pre>` element, not embedded in `<script>` or HTML attributes. If a future
change moves JSON into an HTML attribute or a `<script>` block, switch back to
the default encoder.

## 3. `localStorage` privacy

User progress is stored under keys `scriban-tutorial:progress:*` and the theme
under `scriban-tutorial:theme`. These are visible to any other JavaScript on
the same origin. Acceptable because:

- No PII, no credentials.
- The app is the only thing served from this origin.
- A user can clear progress with `localStorage.clear()` from DevTools, or via
  the standard browser "clear site data" flow.

## 4. Third-party JavaScript (CodeMirror)

CodeMirror 6 and its transitive dependencies are vendored locally under
`wwwroot/lib/codemirror/`, not loaded from a CDN. There's no supply-chain risk
at runtime. Updates require a deliberate re-vendoring step — see the
"CodeMirror vendoring" section of [`../KNOWN_ISSUES.md`](../KNOWN_ISSUES.md)
and the pinned versions in
[`wwwroot/lib/codemirror/VERSION.txt`](../src/ScribanTutorial/wwwroot/lib/codemirror/VERSION.txt).

## 5. Dependency hygiene

CI fails on any vulnerable NuGet (direct or transitive) via a
`dotnet list package --vulnerable --include-transitive` step in
[`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml). The same
gate runs on every PR before merge.
[Dependabot](../.github/dependabot.yml) watches the github-actions ecosystem
weekly so action SHA bumps land as PRs; NuGet bumps stay manual so a
breaking-API jump can't auto-merge.

Current pins:

| Package | Version | Where |
|---|---|---|
| Scriban | 7.2.0 | WASM runtime + ContentBuilder + tests |
| DiffPlex | 1.9.0 | WASM runtime |
| Markdig | 1.2.0 | ContentBuilder + tests (build-time only) |
| HtmlSanitizer (Ganss.Xss) | 9.0.892 | ContentBuilder + tests (build-time only) |
| TextMateSharp | 2.0.3 | ContentBuilder + tests (build-time only) |
| TextMateSharp.Grammars | 2.0.3 | ContentBuilder + tests (build-time only) |

## 6. If you ever deploy this as a multi-user service

Don't, without these changes:

- Render Scriban server-side or in a sandboxed worker with hard time and
  memory limits enforced by the host (not by Scriban).
- Cap input size at the gateway (e.g. 4 KB templates, 8 KB data models).
- Rate-limit per IP.
- Disable `EnableRelaxedTargetAccess`, `EnableRelaxedMemberAccess`,
  `EnableRelaxedFunctionAccess`, `EnableRelaxedIndexerAccess` on
  `TemplateContext`.
- Be careful about which built-in modules you push into the context — `fs`
  and `regex` in particular are attack surface if you ever evaluate untrusted
  templates with broader context.

The current configuration is appropriate **only** for the single-user
local-browser execution model.
