# Authoring lessons

Everything you need to add or edit course content. No C# required — only how
the file layout works, the conventions, and the verification tools.

> Reading this on GitHub? The same content is rendered with a
> non-programmer walkthrough on the live site:
> [**Contribute a lesson**](https://sergeiosipov.github.io/scriban-tutorial/contribute).
>
> Working on the app itself (C#, JS, build, CI) rather than lesson
> content? See [`CONTRIBUTING.md`](../CONTRIBUTING.md) instead.

- [File layout](#file-layout)
- [Add a new exercise (quick)](#add-a-new-exercise-quick)
- [Add a new lesson (start to finish)](#add-a-new-lesson-start-to-finish)
- [File format rules](#file-format-rules-critical)
- [Theory markdown conventions](#theory-markdown-conventions)
- [Exercise design](#exercise-design)
- [Verifying your work](#verifying-your-work)
- [Running the test suite](#running-the-test-suite)
- [Editing on Windows / VS Code](#editing-on-windows--vs-code)
- [Manifest field reference](#manifest-field-reference)
- [Common authoring pitfalls](#common-authoring-pitfalls)

---

## File layout

```
src/ScribanTutorial/wwwroot/
  manifest.json
  lessons/
    01-basics/
      01-theory.md                 ← the read-aloud part of the lesson
      02-exercises/
        hello/
          01-description.md        ← what the exercise asks
          02-datamodel.json        ← JSON the template will see
          03-expected.txt          ← byte-exact expected output
          04-template.txt          ← starter (with ??? placeholders)
          05-solution.txt          ← known-good solution
        member-access/
          ...
    02-filters/
      ...
```

Numeric prefixes are part of the filename. They:

- keep a directory listing in the right order,
- make the first build-step's output match the source order, and
- let the manifest reference exercise dirs by their stable slug
  (`hello`, not `01-hello`).

Don't rename them.

ContentBuilder also writes generated siblings into each lesson and exercise
directory at build time: `*.html` for every `.md`, `02-datamodel.html`,
and a `bundle.json` collecting all six runtime inputs in one fetch. These
are gitignored — don't commit them.

---

## Add a new exercise (quick)

1. Pick a lesson folder under `src/ScribanTutorial/wwwroot/lessons/`,
   e.g. `02-filters/`.
2. Inside its `02-exercises/`, create a new directory named after the
   exercise's stable slug (kebab-case, ASCII):
   `02-filters/02-exercises/strip-whitespace/`.
3. Create **all five files** (encoding rules below — get this part right or
   the runner will reject your output):

   | File | Required | What goes in it |
   |---|---|---|
   | `01-description.md` | yes | What the exercise asks. Markdown. **Do not paste the expected output here** — that lives in `03-expected.txt` and renders in the Expected output panel. Describing the shape ("render a bulleted list", "print a sentence") is fine; reproducing the bytes is duplication that goes stale. |
   | `02-datamodel.json` | yes | JSON object the engine binds to. Top-level MUST be an object, not an array. |
   | `03-expected.txt` | yes | Byte-exact expected output. CRLF is collapsed to LF and trailing newlines are trimmed before comparison; everything else is significant. The Expected output panel always shows this — descriptions should not repeat it. |
   | `04-template.txt` | yes | Starter template. Use `???` for the placeholder(s) the learner needs to fill in. |
   | `05-solution.txt` | yes | The canonical, known-good solution. Verified automatically by the test suite. |

4. Open `wwwroot/manifest.json` and add an entry to the lesson's
   `exercises` array (order matters — learners see them in this order):

   ```json
   { "id": "strip-whitespace", "path": "lessons/02-filters/02-exercises/strip-whitespace" }
   ```

5. From the repo root, **verify** your solution actually produces the
   expected output:

   ```powershell
   dotnet run --project tools\ContentBuilder -- --verify src\ScribanTutorial\wwwroot\lessons\02-filters\02-exercises\strip-whitespace
   ```

   Expected: `--verify OK (...)`. If you see `--verify FAIL`, the tool prints
   expected vs. actual — fix the solution or the expected file until they
   agree.

6. Re-run **the full test suite** to catch any regressions:

   ```powershell
   dotnet test
   ```

   The `ExerciseSolutionTests` class auto-includes your new exercise — no
   test code to write.

7. `dotnet build` (regenerates the `.html` siblings) → reload the dev server
   → click the lesson → confirm your new exercise appears and the solution
   passes when typed in.

8. Commit. The same path also verifies in CI on push, gating the deploy.

---

## Add a new lesson (start to finish)

Higher friction than a single exercise — touches the manifest in more
places and needs a theory file.

1. Create the lesson directory:
   ```
   src/ScribanTutorial/wwwroot/lessons/05-functions/
   ```
2. Inside it, create `01-theory.md` and an empty `02-exercises/`:
   ```
   05-functions/
     01-theory.md
     02-exercises/
   ```
3. Write the theory (conventions below). At minimum: one or two `## Section`
   headings and one `:::example` block per concept.
4. Add at least one exercise inside `02-exercises/` — follow the per-exercise
   quick start above.
5. Add the lesson to `manifest.json`'s `lessons` array, in the order
   learners should see it:

   ```json
   {
     "id": "05-functions",
     "title": "Functions",
     "theoryPath": "lessons/05-functions/01-theory",
     "exercises": [
       { "id": "first", "path": "lessons/05-functions/02-exercises/first" }
     ]
   }
   ```

   - `id` must match the directory name.
   - `theoryPath` omits the extension — the runtime fetches `{theoryPath}.html`
     which ContentBuilder writes from your `.md`.
6. `dotnet build` → `dotnet test` → reload.
7. Spot-check the new lesson in both light and dark themes — code blocks
   should be readable in both.

---

## File format rules (critical)

| Rule | Why |
|---|---|
| UTF-8 with **no BOM** | The runner compares bytes; a BOM prefix breaks byte-exact match against expected. |
| LF line endings (`\n`), not CRLF | `.gitattributes` already enforces this on commit, but configure your editor too. |
| JSON parses cleanly with quoted keys, no trailing commas | The data model is `System.Text.Json`-parsed at build time and again at runtime. |
| `03-expected.txt` is byte-exact (except CRLF and trailing newlines) | Interior whitespace, tabs, and trailing spaces *inside a line* all count. |
| `02-datamodel.json` top-level is an object | The Scriban global scope is keyed on object property names; an array at top-level has nothing to bind. |
| Don't rename exercise / lesson `id` after release | Learners' saved progress is keyed `{lessonId}:{exerciseId}` in localStorage; a rename silently looks like a brand-new (unstarted) exercise. |

---

## Theory markdown conventions

### Headings

Use `# Top` once at the top of the file, `## Section` for the main beats,
`### Subsection` sparingly. The hierarchy is what readers scan when looking
for "where do I find X".

### Plain code blocks

Fenced blocks with a language hint get syntax highlighted at build time:

````markdown
```scriban
{{ user.name | string.upcase }}
```
````

Supported language hints: `scriban` (highlights via the custom grammar),
`json`, `text` (no highlighting — verbatim with HTML escaping). Other
language hints fall through to TextMateSharp's bundled grammars if present.

### Inline HTML is sanitised

Markdig passes raw HTML in your `.md` through to the output, but ContentBuilder
runs the result through an HTML sanitizer before writing the `.html`. That
strips `<script>`, `<iframe>`, `<object>`, `<embed>`, `on*=` handlers, and
`javascript:` URLs. The `<span class="hl-*">` highlight wrappers and the
`<pre><code class="language-*">` blocks the renderer emits are explicitly
allowed and stay intact. If you need a tag that isn't on the allow-list,
extend `MarkdownRenderer.BuildSanitizer` in `tools/ContentBuilder/` rather
than working around it in the markdown.

### `:::example` side-by-side panels

For "given this template + this data, you get this output" — the most
common shape a lesson needs:

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

That renders as two columns: Template on the left, Output on the right.

Add a middle data column with a `json` block:

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

The blocks are matched by language:

- one `scriban` block becomes the Template column,
- one `json` block becomes the Data column (optional),
- one `text` block becomes the Output column.

Order inside the `:::example` doesn't matter as long as each kind appears
at most once. Additional blocks are ignored.

### Tables, links, emphasis

The Markdig pipeline enables `UsePipeTables`, `UseAutoLinks`,
`UseEmphasisExtras`, `UseCustomContainers`, and `UseGenericAttributes`.
Pipe tables, autolinked URLs, `~~strikethrough~~`, `_underscore italics_`,
and `{#anchor-id}` attributes on headings all work.

---

## Exercise design

A few hard-earned rules:

- **One concept per exercise.** Hello (just `{{ name }}`) and Member access
  (`{{ user.first_name }}`) split rather than combining them.
- **Starter template is close enough to correct that the gap is obvious.**
  Use `???` for the blank. Don't make the learner type more than the concept
  requires.
- **Expected output is short and visually distinctive.** A 20-line expected
  output hides off-by-one whitespace bugs from the diff view.
- **Always include `05-solution.txt`.** Run `--verify` and the test suite
  before committing.
- **No host-config dependencies.** Custom renamers, `include "..."`
  templates, host-provided functions — none of those transfer to a learner's
  future projects. Stick to features that work with a vanilla Scriban host.

### Whitespace exercises

If your exercise teaches `{{- -}}` whitespace control, write the expected
output character-by-character (including newlines) and verify with
`--verify`. The diff view in the app displays differing characters, so
learners can debug whitespace bugs themselves — but only if the expected
file is exactly right.

### Number types

JSON `2` parses as a long. JSON `2.5` parses as a double. Both render the
same way in most templates. But if you teach a Scriban operator whose
behaviour differs (e.g. integer division), make sure the data file uses the
type you mean.

---

## Verifying your work

### Single exercise

```powershell
dotnet run --project tools\ContentBuilder -- --verify src\ScribanTutorial\wwwroot\lessons\<lesson>\02-exercises\<exercise>
```

Exit 0 + `--verify OK` = green. Exit 1 + diff = something's off; the tool
prints the expected and actual blocks so you can see exactly where.

### Everything at once

```powershell
dotnet test
```

The `ExerciseSolutionTests` xUnit class reads the manifest, walks every
exercise, and asserts the solution renders the expected output. New
exercises are picked up automatically — no test code to write per
exercise.

### Both at once during a build

```powershell
dotnet build
```

The MSBuild `BuildContent` target invokes ContentBuilder to regenerate
the `.html` siblings on every build. It uses an mtime staleness check so
unchanged files cost nothing.

---

## Running the test suite

There are three sets of tests, all under `tests/ScribanTutorial.Tests/`:

| Test class | What it covers |
|---|---|
| `ContentNormalizeTests` | The CRLF-collapse / trailing-newline-trim helper that both the runtime and `--verify` use. |
| `JsonToScribanTests` | The JSON → Scriban `ScriptObject` converter — including the bug that previously turned all integers into doubles. |
| `ExerciseSolutionTests` | **Data-driven from the manifest.** Every exercise gets a test case via xUnit's `MemberData`. Add an exercise → add a test, automatically. Plus structural smoke tests on the manifest itself. |

### How `ExerciseSolutionTests` finds the files

`tests/ScribanTutorial.Tests/RepoPaths.cs` walks up from the test's source
file at compile time (via `[CallerFilePath]`) until it finds the
`ScribanTutorial.slnx`. From there it computes the `wwwroot/lessons/`
path. The tests therefore work regardless of where the test runner sets
its working directory (Visual Studio Test Explorer, `dotnet test`, CI,
etc.).

### Writing a focused test

For an exercise that needs special handling — e.g. a deliberately
non-deterministic template — the data-driven test alone might not cut
it. Add a `[Fact]` to `ExerciseSolutionTests.cs` (or a new test class
under `tests/ScribanTutorial.Tests/`):

```csharp
[Fact]
public void Strip_whitespace_handles_unicode_NBSPs()
{
    var dir = Path.Combine(RepoPaths.LessonsDir,
        "02-filters", "02-exercises", "strip-whitespace");
    // ...read files, run, assert...
}
```

Keep helpers in `RepoPaths` so we don't pile up `Path.Combine` chains.

---

## Editing on Windows / VS Code

Settings that prevent the format rules from biting you:

```json
{
  "files.encoding": "utf8",
  "files.eol": "\n",
  "files.insertFinalNewline": false,
  "files.trimTrailingWhitespace": false
}
```

- `insertFinalNewline: false` because some exercises don't want one.
- `trimTrailingWhitespace: false` because `03-expected.txt` can legitimately
  contain trailing spaces inside a line.

If you use PowerShell to write a file by hand, **never** use `>` or default
`Out-File`: it writes UTF-16 with a BOM. The right pattern:

```powershell
[System.IO.File]::WriteAllText($path, $content, [System.Text.UTF8Encoding]::new($false))
```

Or in PowerShell 7+:

```powershell
Set-Content -Encoding utf8NoBOM -Path $path -Value $content
```

---

## Manifest field reference

| Field | Type | Required | What it does |
|---|---|---|---|
| `courseTitle` | string | yes | Top of the sidebar |
| `courseSubtitle` | string | yes | Below the title |
| `lessons[]` | array | yes | Order matters — sidebar order |
| `lessons[].id` | string | yes | URL slug — must match the folder name |
| `lessons[].title` | string | yes | Display name |
| `lessons[].theoryPath` | string | yes | Path without extension; runtime fetches `.html` |
| `lessons[].exercises[]` | array | yes | Order matters — exercise order within the lesson |
| `lessons[].exercises[].id` | string | yes | Stable identifier — **don't rename** after release |
| `lessons[].exercises[].path` | string | yes | Path to the exercise directory (no trailing slash) |

---

## Common authoring pitfalls

| Symptom | Cause | Fix |
|---|---|---|
| `--verify FAIL` with no obvious diff | Trailing whitespace **inside** a line of `03-expected.txt` you didn't notice | Toggle "render whitespace" in your editor and re-check |
| Exercise renders nothing on the page | Template renders the empty string because the data-model property name is misspelled (Scriban returns null silently for missing members) | Double-check `02-datamodel.json` field names against the template |
| `for x in items` outputs nothing | `items` is missing from the data model, or it's null. Scriban silently skips a loop over null. | Confirm the field exists in `02-datamodel.json` and is an array |
| Extra blank line in output | Block tags like `{{ for }}` or `{{ end }}` leave the surrounding newlines in. | Add `{{-` / `-}}` whitespace control. See `03-control-flow/01-theory.md` for examples. |
| Number formats as `5.0` instead of `5` | Used to be a bug where every JSON integer became a double; fixed (see `JsonToScribanTests.Distinguishes_integers_from_floats`). If it returns, that test will fail. | — |
| Browser still shows old content | GitHub Pages CDN cache — 2–5 min after a deploy lands. | Hard refresh (Ctrl+F5), wait. |
| Sidebar indicator stays `○` even after passing | Browser blocked `localStorage` (private mode, restrictive content-blocker). Progress only persists when the browser allows the write. | Try a normal window. |
