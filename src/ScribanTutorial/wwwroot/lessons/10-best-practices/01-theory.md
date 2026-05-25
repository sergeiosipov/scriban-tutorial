By this point you've seen every language construct the course teaches.
This lesson collects the patterns and gotchas that distinguish a
template that works in the small from one that survives real data.

## Naming and casing

Identifiers are case-sensitive. Pick `snake_case` for JSON keys and
reach for them the same way in templates — that's the convention Scriban
uses by default, and the safest pattern across hosts that may rename
.NET property names. Mixed casing across a code base is the most common
source of "why is this empty?" bugs.

## Whitespace control around block tags

Every block tag (`{{ for }}`, `{{ if }}`, `{{ end }}`) leaves a blank
line in the output unless you trim it. The pattern below renders a clean
bulleted list:

```scriban
{{- for item in items -}}
- {{ item }}
{{ end -}}
```

`{{- ... -}}` trims everything aggressively; `{{~ ... ~}}` trims more
gently (one newline only on each side). Pick the gentler form when
you care about preserving the body's indentation; pick the greedy form
when you want the tag to vanish entirely.

## `for.last` for clean separators

Trailing-separator bugs are easy to avoid with `for.last`:

```scriban
{{- for x in items -}}{{ x }}{{ if !for.last }}, {{ end }}{{- end -}}
```

The same shape works for line-separated output, slash-separated paths,
etc. — anything where the joiner shouldn't follow the last element.

## Defensive null handling

Three patterns earn their keep:

- `obj?.member` — short-circuits when the chain hits a null.
- `value ?? "default"` — substitutes when the left side is null.
- `if (array.size items) > 0` — explicit empty check; empty arrays are
  still truthy in Scriban, so a bare `if items` won't catch them. The
  parens matter: without them, Scriban parses the expression as
  `array.size (items > 0)` and the comparison fails at runtime.

## Numbers vs strings

`{{ 5 + 3 }}` is `8`. `{{ "5" + 3 }}` is `"53"`. Always check the JSON
shape — `"qty": "4"` and `"qty": 4` behave differently in templates and
the bug shows up only at the operator boundary.

## Pre-compute where you can

Scriban can sum, average, and multiply inside a template, but it's
clumsy compared to doing it in the host code that produces the JSON.
Pass per-line subtotals and grand totals in the data model and just
print them — your templates stay readable.

## Filters you'll reach for first

A short tour of the standard library worth memorising:

| Need | Reach for |
|---|---|
| Make uppercase / strip spaces | `string.upcase`, `string.strip` |
| Format a number | `math.format "0.00"`, `math.round` |
| Format a date | `date.to_string "%Y-%m-%d"` |
| Test "has items" | `(array.size x) > 0` |
| Join with a separator | `array.join ", "` |
| Keys of an object | `object.keys o`, `object.size o` |

The full reference: <https://scriban.github.io/docs/built-ins/>.

## The capstone

The exercise below pulls everything together: data-model member access,
a loop with whitespace control, per-line and grand-total fields
pre-computed in the data model, and a literal frame around the dynamic
parts. If you can read it without squinting, you're ready to write
Scriban for real.
