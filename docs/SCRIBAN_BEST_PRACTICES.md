# Scriban best practices

This guide is for the **course authors**, not the runtime. It collects the
patterns lesson content should teach — and the gotchas worth a warning. All
references to the language assume default (non-Liquid) Scriban mode, where
both expressions and statement blocks use `{{ ... }}`.

The authoritative reference is <https://scriban.github.io/docs/language/>. When
in doubt, that document wins over this one.

## Naming and casing

- Identifiers are case-sensitive. Pick `snake_case` for JSON keys and reach for
  them the same way in templates — that's the convention Scriban uses by
  default. Mixed casing across hosts is the most common source of "why is this
  empty?" bugs.
- Lesson exercises use snake_case throughout for this reason. Keep it
  consistent when adding new content.

## Whitespace control

Every block tag (`{{ for }}`, `{{ if }}`, `{{ end }}`) leaves a blank line in
the output unless you trim it. Add `-` on the side you want trimmed:

- `{{-` strips whitespace and one newline **before** the tag.
- `-}}` strips whitespace and one newline **after** the tag.
- Both at once: `{{- ... -}}`.

The list-rendering pattern in lesson 03 is the canonical example:

```scriban
{{- for item in items -}}
- {{ item }}
{{ end -}}
```

## `if` / `for` patterns

- Use `else if`, not nested `if ... end ... if`.
- `for x in items` exposes `for.index`, `for.first`, `for.last`, `for.length`,
  `for.changed`. Don't reinvent loop counters.
- Conditional trailing separators are clean with `for.last`:

  ```scriban
  {{- for x in items -}}{{ x }}{{ if !for.last }}, {{ end }}{{- end -}}
  ```

## Filters

- Pipe left to right: `{{ s | string.strip | string.upcase }}`.
- The modules you'll mention most: `string.*`, `array.*`, `object.*`, `math.*`,
  `date.*`, `regex.*`, `html.*`.
- Teach `array.size`, `string.size`, `object.keys` early — they unlock most
  "introspect this data" patterns.
- `math.format` for number formatting, `date.to_string` for dates.

## Functions

- Define with `func name; ...; end`. Return the last expression or use `ret`.
- Closures capture the surrounding scope. Use sparingly in templates — flatter
  code is easier to read.
- `do ... end` defines an anonymous function that can be passed to higher-order
  filters.

## Includes and partials

- `{{ include "header.scriban" }}` only works if the host configures a template
  loader. This tutorial doesn't, so exercises here don't use includes.
  Learners can read about them but should plan for them being host-dependent.

## Common gotchas

1. **Truthy/falsy.** `if x` is truthy for any non-null, non-false, non-empty-string
   value. **Empty arrays are still truthy** — use `if array.size x > 0` when
   you mean "has items".
2. **`for` over a null variable silently does nothing.** No error, no output.
   If an exercise expects output, missing data won't fail loudly. Teach
   defensive checks.
3. **Pipe chains break on newlines unless wrapped in parens or expressed on
   one line.** Multi-line filter chains are valid but visually surprising.
4. **`string.capitalize` capitalises only the first letter.** Use
   `string.capitalizewords` for title-case.
5. **Numbers vs strings.** `{{ "5" + 3 }}` is `"53"` (string concatenation).
   `{{ 5 + 3 }}` is `8`. The JSON shape (`"qty": "4"` vs `"qty": 4`) matters.
6. **Missing members return null.** Which renders as the empty string. A
   typo in `user.first_namee` produces silent emptiness rather than an
   exception — so when a template renders nothing, check the data first.

## Designing exercises

- Each exercise should test exactly one concept. The hello / member-access
  split in lesson 01 is the template — same domain, different concept.
- Starter templates should be *close enough* to correct that the missing
  piece is unambiguous. Use `???` for the blank.
- Expected outputs should be short and visually distinctive. Long expected
  outputs hide off-by-one whitespace bugs from the diff view.
- Always include `05-solution.txt`, and run `--verify` on it before committing.
- Avoid exercises whose answer depends on Scriban host configuration
  (custom renamers, custom functions, includes). They don't transfer to
  learners' future projects.

## Advanced topics for later lessons

When the four core lessons land, the next batch could cover:

- `tablerow` — grid layouts that cycle `for.index` across columns.
- `wrap` — content wrappers, the template-as-function pattern.
- `capture name; ...; end` — store rendered output in a variable.
- `case x; when 1; ...; else; ...; end` — pattern matching.
- `with object; ...; end` — implicit member access inside the block.
- `regex.match`, `regex.replace` — be careful with greedy matches in lesson
  examples; teach non-greedy.

## Mapping to the official docs

Coverage check against
<https://scriban.github.io/docs/language/> — every concept gets at least one
theory mention and one exercise where possible:

| Concept | Theory | Exercise |
|---|---|---|
| Comments / escapes | — | — (future) |
| Expressions, variables, properties | 01-basics | hello, member-access |
| Pipe operator | 02-filters | upcase, math |
| String concat, math operators | 02-filters | math |
| Comparison, logic | 03-control-flow | conditional |
| `if` / `else` | 03-control-flow | conditional |
| `for`, ranges | 03-control-flow | list-loop |
| `for.first` / `for.last` | 03-control-flow | — (future) |
| Combining features | 04-assembly | invoice |
| `case` / `when` | — | — (future) |
| `capture`, `func`, `with`, `wrap`, `include` | — | — (future) |
| Whitespace control | 01-basics / 03 / 04 | list-loop, invoice |

Future lessons should fill in the "—"s, prioritising `case`/`when` and
`capture` since they're the most-asked features that the current course
doesn't yet cover.
