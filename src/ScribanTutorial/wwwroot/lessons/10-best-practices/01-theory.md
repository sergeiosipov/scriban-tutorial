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

## Pre-compute in the host — or in the template?

Pre-computing totals in the host code that produces the JSON is the
cleanest default: templates stay readable and the same numbers feed
every consumer. But sometimes the data arrives as a flat list of rows
and you have to do the rollup at render time. Two patterns cover almost
every case.

**Accumulator with a local variable.** Declare `$grand = 0` outside the
loop, add inside it. The `$` prefix keeps the counter local — it won't
leak into the global scope of an `include` or the next render. Pair it
with an inline `subtotal(line) = line.qty * line.unit_price` and the
per-row arithmetic gets a name.

```scriban
{{- subtotal(line) = line.qty * line.unit_price
    $grand = 0 -}}
{{- for line in lines
      $line_total = subtotal line
      $grand = $grand + $line_total
}}
{{- end }}
Total: {{ $grand }}
```

**Object-as-map for group-by.** Scriban doesn't ship a `group_by`
function, but a plain object plus the null-coalesce idiom does the job:

```scriban
{{ totals = {}
   for t in transactions
     if t.status == "settled"
       key = t.fund + " " + t.type
       totals[key] = (totals[key] ?? 0) + t.amount
     end
   end
   for k in (object.keys totals)
     k + ": " + totals[k]
   end }}
```

The parens around `(object.keys totals)` matter — a bare
`for k in object.keys totals` reads as
`for k in object.keys` *followed by* a stray `totals`, and the engine
trips on the zero-argument `object.keys` call. Same reason
`(array.size x) > 0` needs its parens.

## The capstones

The exercises below pull everything together. There are three flavours,
in order of difficulty:

1. **`invoice`** — data-model fields and member access only; subtotals
   and the grand total arrive pre-computed.
2. **`invoice-from-items`** — same shape, but the data carries only
   `qty` and `unit_price`. The template computes each subtotal via an
   inline function and the grand total via a `$grand` accumulator.
3. **`transaction-rollup`** — group-by aggregation: four fund
   transactions are filtered by status and merged by
   fund+direction into one line per group.

If you can read all three without squinting, you're ready to write
Scriban for real.
