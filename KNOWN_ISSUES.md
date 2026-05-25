# Known issues

Tracked rough edges that don't block the build but a contributor should be
aware of before opening a related PR.

## Course coverage

The course now mirrors the structure of
<https://scriban.github.io/docs/language/> — lessons 01–09 follow the
doc's top-level sections in order, and lesson 10 collects the best
practices. Topics still **not** covered by the course (intentionally
deferred or excluded):

- `wrap` content wrappers — present in lesson 09's theory as a mention,
  no exercise.
- `tablerow` grid layouts — present as a mention; require an HTML host
  to verify visually and don't transfer to a learner's text-only
  templates.
- `include` / `include_join` — depend on a host-configured template
  loader that this WASM tutorial doesn't ship.
- `regex.*` filters beyond a passing example in lesson 03 (verbatim
  strings) and the gotcha list in lesson 10.

These are listed here, not on the rendered Course coverage page, because
adding them would require host-side configuration that this single-user
browser app intentionally avoids.

## Scriban TextMate grammar — `tools/ContentBuilder/grammars/scriban.tmLanguage.json`

Per-edge tests live in
[`ContentBuilderTests`](tests/ScribanTutorial.Tests/ContentBuilderTests.cs).
Each test pins the current grammar behaviour for one edge — fix the grammar
**and** update the test in the same PR if you change behaviour intentionally;
a silent regression breaks the test.

Intentional edges contributors should know about:

| Edge | Status | Test |
|---|---|---|
| `# foo }} bar` comment "eats" closing `}}` | Intentional — matches Scriban's own parser (`#` is comment-to-EOL, even past `}}`). Workaround: put the comment on its own line. Highlighter matching parser behaviour is correct; "fixing" it would visually suggest the tag closes when it actually doesn't. | `Grammar_treats_hash_comment_as_comment_to_end_of_line` |
| `{{- -x -}}` unary minus tokenised as operator+variable | Intentional — same treatment as VS Code, Sublime, every mainstream editor. Disambiguating unary vs binary would require a Lezer parser rewrite for cosmetic-only benefit. | `Grammar_classifies_minus_inside_whitespace_control_as_operator` |
| Liquid `{% ... %}` statement tags | Removed (course doesn't teach Liquid; restore the `statement` rule if a future lesson covers it) | — |

The grammar JSON lives at `tools/ContentBuilder/grammars/scriban.tmLanguage.json`.
The scope→`.hl-*` class mapping is in
[`TextMateHighlighter.cs`](tools/ContentBuilder/TextMateHighlighter.cs).

> Project-mechanics notes that used to live here (CodeMirror vendoring,
> GitHub Pages CDN cache) have moved to
> [`CONTRIBUTING.md`](CONTRIBUTING.md#project-mechanics) — they're
> contributor concerns, not learner-visible gotchas.
