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

## Single quotes by default for string literals

Scriban accepts `'..'` and `".."` interchangeably for non-interpolated
strings. **Prefer single quotes** as the default for ordinary string
literals — reserve double quotes for strings whose body contains a `'`.
Three reasons that compound across a long template:

1. **Less quote-noise in object literals.** Compare
   `{ name: 'Ada', role: 'admin' }` with
   `{ name: "Ada", role: "admin" }`. The single-quoted form reads more
   like data and less like nested syntax.
2. **Visual distinction from interpolated strings.** A single-quoted
   literal is **never** interpolated — no need to scan the body for
   `{` to know if Scriban will evaluate something inside. Interpolated
   strings stand out as `$"..."` / `$'...'`.
3. **HTML attributes embed cleanly.** `'<a href="/foo">link</a>'`
   needs zero escapes; `"<a href=\"/foo\">link</a>"` is the same value
   with four extra characters per attribute.

The rule of thumb: pick the quote style that lets the body sit literal
with no `\"` or `\'` escapes. Single quotes win most of the time.

## Numbers vs strings

`{{ 5 + 3 }}` is `8`. `{{ '5' + 3 }}` is `'53'`. Always check the JSON
shape — `"qty": "4"` and `"qty": 4` behave differently in templates and
the bug shows up only at the operator boundary.

## Accumulating in a loop: mutate in place, don't copy

Most `array.*` and `string.*` operations return a NEW value — the
original is untouched. That's the default behaviour you want for
one-off transforms. Inside a loop, it becomes a quiet performance trap:

```scriban
{{- a = []
   for n in 1..10000
     a = a | array.add n   # ← new array every iteration
   end -}}
```

By iteration 10,000 you've allocated 10,000 arrays. The shape of the
work is O(N²) (each copy walks the growing tail) and you've handed the
garbage collector 10,000 short-lived objects. `array.add_range`,
`array.concat`, and `array.insert_at` all behave the same way — they
all return new arrays.

**For true in-place append, use index assignment** (covered in
[lesson 6](/scriban-tutorial/lesson/06-arrays) and revisited in
[lesson 16](/scriban-tutorial/lesson/16-array)):

```scriban
{{- a = []
   for n in 1..10000
     a[a.size] = n   # ← writes into the existing array
   end -}}
```

`a[a.size] = v` writes to the slot one past the current end. The array
grows by one and no copies are made. Use this pattern for every
hot-loop accumulator that needs a list.

### Same idea, other modules

- **Strings**: `s + 'x'` and `s | string.append 'x'` both return new
  strings. For lots of small appends, build into an array and join at
  the end (`array.join ''`). Or use [`capture`](/scriban-tutorial/lesson/09-statements)
  to render a block into a variable — the engine streams into the
  capture buffer instead of re-allocating per concat.
- **Dates**: `date.add_*` returns a new `DateTime` per call. Templates
  rarely chain enough of these to matter, but in a long loop, compute
  the offset once (`days_since = i; d | date.add_days days_since`)
  rather than nesting calls.
- **Objects**: members assigned with `o.x = v` DO mutate in place —
  that's the recommended pattern for accumulating into a map (see
  the `transaction-rollup` exercise below). The contrast with arrays
  is intentional: object member assignment is a write, array `add` is
  a copy.

The takeaway: when you see `x = x | something y` in a loop, ask whether
`something` returns a new value. If it does, see if there's an
in-place form (index assignment for arrays, member assignment for
objects) before reaching for it.

## Filters you'll reach for first

A short tour of the standard library worth memorising:

| Need | Reach for |
|---|---|
| Make uppercase / strip spaces | `string.upcase`, `string.strip` |
| Format a number | `math.format '0.00'`, `math.round` |
| Format a date | `date.to_string '%Y-%m-%d'` |
| Test "has items" | `(array.size x) > 0` |
| Join with a separator | `array.join ', '` |
| Keys of an object | `object.keys o`, `object.size o` |

The full reference: <https://scriban.github.io/docs/builtins/>.

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
     if t.status == 'settled'
       key = t.fund + ' ' + t.type
       totals[key] = (totals[key] ?? 0) + t.amount
     end
   end
   for k in (object.keys totals)
     k + ': ' + totals[k]
   end }}
```

The parens around `(object.keys totals)` matter — a bare
`for k in object.keys totals` reads as
`for k in object.keys` *followed by* a stray `totals`, and the engine
trips on the zero-argument `object.keys` call. Same reason
`(array.size x) > 0` needs its parens.

## Regex when text doesn't fit a structured model

When the data arrives as a free-form string (a log line, a user-typed
message, a CSV cell), the structured `for` + member-access toolkit
doesn't reach. `regex.*` does. `regex.replace text pattern replacement`
is the workhorse — pass a verbatim backtick pattern so the backslashes
don't double up:

```scriban
{{ log | regex.replace `\d+\.\d+\.\d+\.\d+` '[redacted]' }}
```

For matching (rather than replacing), `regex.match` returns the first
match; `regex.split` slices the input on a pattern (you saw that one
back in lesson 03's verbatim-string exercise).

The full filter list is at <https://scriban.github.io/docs/builtins/>.

## The capstones

The capstone exercises pull everything together. There are four, in rough
order of difficulty:

1. **`invoice`** — data-model fields and member access only; subtotals
   and the grand total arrive pre-computed.
2. **`regex-redact`** — strip IPv4 addresses out of a log line with
   `regex.replace` and a verbatim pattern. A single-function drill, but
   the pattern syntax takes a moment.
3. **`invoice-from-items`** — same shape as `invoice`, but the data
   carries only `qty`, a decimal `unit_price`, and an ISO `issued`
   date. The template computes each subtotal via an inline function,
   accumulates the grand total in `$grand`, and integrates the
   formatting lessons: `math.format 'N2'` for the money columns,
   `date.parse` + `date.to_string` for the header date.
4. **`transaction-rollup`** — group-by aggregation: four fund
   transactions are filtered by status and merged by
   fund+direction into one line per group.

If you can read all four without squinting, you're ready to write
Scriban for real.
