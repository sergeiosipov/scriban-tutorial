# Known issues

Tracked rough edges that don't block the build but a contributor should be
aware of before opening a related PR.

## Scriban TextMate grammar — `tools/ContentBuilder/grammars/scriban.tmLanguage.json`

Per-edge tests live in
[`ContentBuilderTests`](tests/ScribanTutorial.Tests/ContentBuilderTests.cs).
Each test pins the current grammar behaviour for one historical edge — fix
the grammar **and** update the test in the same PR if you change behaviour
intentionally; a silent regression breaks the test.

Closed-out edges and the test that locks each one in:

| Edge | Status | Test |
|---|---|---|
| Multi-line string literals inside `{{ ... }}` | Works correctly across newlines | `Grammar_handles_strings_spanning_multiple_lines_inside_a_tag` |
| `"hello ${name}"` interpolation | Fixed via `interpolation` rule + `${ ... }` breakout inside double-quoted strings | `Grammar_breaks_out_of_string_for_dollar_brace_interpolation` |
| `regex.match` / `string.upcase` / other builtin functions | Fixed via `builtin-call` rule — the X in `builtin.X` is now classified as a function call | `Grammar_treats_verbatim_string_argument_as_a_string` |
| `# foo }} bar` comment "eats" closing `}}` | Intentional — matches Scriban's own parser (`#` is comment-to-EOL, even past `}}`). Workaround: put the comment on its own line. Highlighter matching parser behaviour is correct; "fixing" it would visually suggest the tag closes when it actually doesn't. | `Grammar_treats_hash_comment_as_comment_to_end_of_line` |
| `{{- -x -}}` unary minus tokenised as operator+variable | Intentional — same treatment as VS Code, Sublime, every mainstream editor. Disambiguating unary vs binary would require a Lezer parser rewrite for cosmetic-only benefit. | `Grammar_classifies_minus_inside_whitespace_control_as_operator` |
| Liquid `{% ... %}` statement tags | Removed (course doesn't teach Liquid; restore the `statement` rule if a future lesson covers it) | — |

The grammar JSON lives at `tools/ContentBuilder/grammars/scriban.tmLanguage.json`.
The scope→`.hl-*` class mapping is in
[`TextMateHighlighter.cs`](tools/ContentBuilder/TextMateHighlighter.cs).

## Course coverage

The `:::example` blocks and exercises cover a working subset of Scriban —
expressions, member access, pipe filters, arithmetic, `if`/`else`, `for`,
ranges, basic whitespace control, and a real-world combining example.
Topics intentionally **not yet** covered, queued for follow-up lessons:

- `case` / `when` pattern matching.
- `capture` for storing rendered output in a variable.
- `func name; ...; end` user-defined functions.
- `with object; ...; end` implicit member access blocks.
- `wrap` content wrappers.
- `tablerow` grid layouts.
- `regex.*` filters beyond a passing mention.

A `Future lessons` checklist lives at the bottom of
`docs/SCRIBAN_BEST_PRACTICES.md`.

## CodeMirror vendoring

Vendored under `src/ScribanTutorial/wwwroot/lib/codemirror/` as 11 ESM files
resolved through an importmap in `index.html`. Bumps are scripted via
`tools/Vendor-CodeMirror.ps1` — edit the `$packages` table with the new
version pin, run the script, update `wwwroot/lib/codemirror/VERSION.txt`
to match, and commit the bumped files + VERSION.txt in one PR. The script
reports a SHA-256 prefix per file so two runs on a clean checkout can be
diff-compared. The `codemirror` umbrella package is intentionally *not*
vendored: its `basicSetup` would drag in `@codemirror/search`,
`@codemirror/autocomplete`, and `@codemirror/lint`, none of which this app
uses. Extensions are composed by hand in `wwwroot/js/editor.js`.

## GitHub Pages CDN

Pages serves through a CDN that occasionally takes 2–5 minutes after a
successful deploy to propagate. If the workflow run is green but the site
still shows old content, hard-refresh (Ctrl+F5) and wait. This is documented
in `docs/DEPLOYMENT.md` so users don't think their deploy is broken.
