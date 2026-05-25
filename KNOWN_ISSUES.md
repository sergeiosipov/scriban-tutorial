# Known issues

Tracked rough edges that don't block the build but a contributor should be
aware of before opening a related PR.

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
> [`docs/AUTHORING_LESSONS.md`](docs/AUTHORING_LESSONS.md#project-mechanics) —
> they're contributor concerns, not learner-visible gotchas.
