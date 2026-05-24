# Control flow

Control flow tags use the same `{{ ... }}` delimiters as expressions. The
template engine recognises a handful of statement keywords (`if`, `else`,
`for`, `while`, …) and pairs each with a closing `{{ end }}`.

## `if` / `else if` / `else`

```scriban
{{ if stock > 0 }}in stock ({{ stock }}){{ else }}out of stock{{ end }}
```

Scriban's truthiness: a value is "truthy" if it's not `null`, not `false`, and
(for strings) not the empty string. Empty arrays are still truthy, so check
their size explicitly when that's what you mean: `if array.size items > 0`.

## `for ... in ...`

```scriban
{{- for item in items -}}
- {{ item }}
{{ end -}}
```

The `-` inside `{{-` / `-}}` strips whitespace and one newline on the indicated
side. Without it, the newlines around the `for` and `end` tags would leak
blank lines into the output.

### Loop variables

Inside a `for` block, Scriban exposes a `for` object with:

- `for.index` — zero-based iteration number
- `for.first` — `true` on the first iteration
- `for.last` — `true` on the last iteration
- `for.length` — total number of items
- `for.changed` — `true` when the current value differs from the previous one

Use `for.last` to handle trailing separators cleanly:

:::example
```scriban
{{- for x in items -}}{{ x }}{{ if !for.last }}, {{ end }}{{- end -}}
```
```json
{ "items": ["red", "green", "blue"] }
```
```text
red, green, blue
```
:::

## Ranges

`1..5` is a range that iterates from 1 to 5 inclusive. Handy with `for`:

```scriban
{{- for i in 1..3 -}}{{ i }} {{ end -}}
```

renders `1 2 3`.

## `break` and `continue`

Both work as you'd expect — exit the loop early or skip to the next iteration.

You'll meet these in the next two exercises.
