# Known issues

Tracked rough edges that don't block the build but a contributor should be
aware of before opening a related PR.

## Scriban TextMate grammar — `tools/ContentBuilder/grammars/scriban.tmLanguage.json`

The grammar is hand-written and intentionally minimal. It's enough to colour
the lesson examples readably; it is **not** a complete Scriban parser. Edges
that won't tokenise the way you might expect:

- **Multi-line string literals inside `{{ ... }}`.** A string that spans two
  source lines won't continue its `string.quoted` scope across the newline —
  the line-based stream parser ends the token at EOL. In practice authors
  rarely write multi-line strings in lesson Scriban, but if you start, expect
  the second line to render unstyled.
- **Interpolation inside strings.** Scriban supports `"hello ${name}"`-style
  interpolation; the grammar treats the whole string body as one
  `string.quoted` and does not break out into the inner expression. Tokens
  inside `${ ... }` render with the string colour.
- **Regex literals.** No dedicated scope. They fall through to the generic
  string handling if quoted, or to identifiers / operators otherwise. The
  `regex.*` filter modules tokenise correctly (they're regular function
  calls), but a regex pattern argument is just a string.
- **Comments at end of line containing `}}`.** `# foo }} bar` inside an
  expression tag is correctly parsed as a comment that runs to EOL, but
  the closing `}}` on the SAME line is not seen — the rest of that physical
  line is comment. Workaround: put the comment on its own line.
- **Whitespace-control dash inside operators.** A construct like `{{x-y}}`
  (subtraction with no spaces) is tokenised correctly, but `{{- -x -}}`
  (unary minus on a value, with strip markers) tokenises the inner `-x` as
  an operator-then-variable rather than a unary-minus expression. Cosmetic
  only — Scriban itself parses it fine; the grammar just under-classifies.
- **Liquid mode tags `{% ... %}`.** Present in the grammar in case a future
  lesson teaches it, but the course doesn't currently use Liquid syntax and
  the highlighter's class mapping for those tags hasn't been tested against
  real Liquid content.

If you want to harden any of these, the grammar lives in
`tools/ContentBuilder/grammars/scriban.tmLanguage.json`. The class mapping
that turns scopes into `.hl-*` CSS classes is in `TextMateHighlighter.cs`.
A single smoke test in
[`ContentBuilderTests.TextMateHighlighter_emits_expected_classes_for_a_simple_scriban_snippet`](tests/ScribanTutorial.Tests/ContentBuilderTests.cs)
catches the worst regression (no `hl-brace` / `hl-variable` / `hl-operator` /
`hl-type` at all). Extending that test with cases for each edge above is the
natural next step when someone touches the grammar.

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
resolved through an importmap in `index.html`. Re-vendoring (bump CodeMirror,
update the `style-mod` or `crelt` version, etc.) is by hand — there's no
automated script. The pinned versions and update procedure are documented in
`wwwroot/lib/codemirror/VERSION.txt`. The `codemirror` umbrella package is
intentionally *not* vendored: its `basicSetup` would drag in `@codemirror/search`,
`@codemirror/autocomplete`, and `@codemirror/lint`, none of which this app uses.
Extensions are composed by hand in `wwwroot/js/editor.js`.

A `tools/vendor-codemirror.ps1` script that fetches each file from unpkg with
the right version would be worth writing the next time anyone has to bump.

## GitHub Pages CDN

Pages serves through a CDN that occasionally takes 2–5 minutes after a
successful deploy to propagate. If the workflow run is green but the site
still shows old content, hard-refresh (Ctrl+F5) and wait. This is documented
in `docs/DEPLOYMENT.md` so users don't think their deploy is broken.
