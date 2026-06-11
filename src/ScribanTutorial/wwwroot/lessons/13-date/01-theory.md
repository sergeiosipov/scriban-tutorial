The `date.*` module exposes the .NET `DateTime` type to your templates
— parsing, formatting, arithmetic, and field access. Twelve functions
plus the per-instance properties for year/month/day/etc.

Upstream reference:
[scriban.github.io/docs/builtins/date](https://scriban.github.io/docs/builtins/date/).

**Return types.** Every function in this module returns a new value;
the input `DateTime` is never mutated. Most return **DateTime**
(parse, now, all the `add_*` family); `date.to_string` and
`date.parse_to_string` return **string**. Property accessors like
`.Year` return **int**.

## A note on property names

The upstream Scriban docs write date properties in `snake_case`
(`d.year`, `d.month`, etc.). **This app preserves the underlying .NET
property names in PascalCase** — so the templates here use `d.Year`,
`d.Month`, `d.Day`, etc. instead. The functions still use snake_case
(`date.parse`, `date.add_days`); it's only the per-instance properties
on the `DateTime` value that change.

If you copy a snippet from the upstream docs into this playground and
nothing renders for `d.year`, that's why — flip to `d.Year`.

## Getting "now"

| Function | Returns |
|---|---|
| `date.now` | Current local date+time |
| `date.utc_now` | Current UTC date+time |

Both return a `DateTime` value. Rendered directly it formats like
`28 May 2026`; pass through `date.to_string` for control:

:::example
```scriban
{{ today = date.parse '2024-03-15'
   today | date.to_string `%Y-%m-%d` }}
```
```text
2024-03-15
```
:::

(Examples in this lesson use a fixed parsed date so the recorded output
stays stable. `date.now` itself can still be exercised — assert
*structure* instead of an exact value, e.g. `date.now.Year >= 2024`,
the same pattern lesson 10 uses for `math.uuid`.)

## Parsing strings

`date.parse text pattern? culture?` reads a string into a `DateTime`.
With no pattern, .NET's culture-aware parser figures out common shapes
(`'2024-03-15'`, `'03/15/2024'`, `'15 Mar 2024'`, ISO 8601):

:::example
```scriban
{{ d = date.parse '2024-03-15'
   'Y=' + d.Year + ' M=' + d.Month + ' D=' + d.Day }}
```
```text
Y=2024 M=3 D=15
```
:::

For a pattern-driven parse, pass a format string (same shapes as
`date.to_string`):

```scriban
{{ date.parse '15/03/2024' `%d/%m/%Y` }}
```

`date.parse_to_string` combines parse + format in one call when you're
converting between two textual representations.

## Per-instance properties

A `DateTime` value exposes these fields directly (remember:
PascalCase in this app):

| Property | Value |
|---|---|
| `.Year` | 4-digit year |
| `.Month` | 1–12 |
| `.Day` | 1–31 |
| `.Hour` | 0–23 |
| `.Minute` | 0–59 |
| `.Second` | 0–59 |
| `.Millisecond` | 0–999 |
| `.DayOfYear` | 1–366 |

:::example
```scriban
{{ d = date.parse '2024-03-15 13:45:00'
   d.Hour }}:{{ d.Minute }} on day {{ d.DayOfYear }}
```
```text
13:45 on day 75
```
:::

## Adding intervals

Seven `add_*` functions return a new `DateTime` shifted by the given
amount. They don't mutate the source:

| Function | Returns | Adds |
|---|---|---|
| `date.add_years d n` | DateTime | n years |
| `date.add_months d n` | DateTime | n months |
| `date.add_days d n` | DateTime | n days |
| `date.add_hours d n` | DateTime | n hours |
| `date.add_minutes d n` | DateTime | n minutes |
| `date.add_seconds d n` | DateTime | n seconds |
| `date.add_milliseconds d n` | DateTime | n milliseconds |

`n` can be negative — `add_days d (-7)` is "one week earlier." (The
parentheses around `-7` matter: without them the `-` reads as
subtraction and the call fails.)

:::example
```scriban
{{ start = date.parse '2024-01-15'
   later = start | date.add_months 3 | date.add_days 5
   later | date.to_string `%Y-%m-%d` }}
```
```text
2024-04-20
```
:::

## Date arithmetic

Two operator forms work directly on `DateTime` values: subtracting two
dates (`later - earlier`) returns a **TimeSpan**, and adding a
timespan to a date (`d + interval`) returns a new, shifted
**DateTime**. (Lesson 14 covers the `timespan.from_*` constructors.)

Subtraction is the "days until deadline" computation — read
`.TotalDays` (or `.TotalHours`, etc.) off the resulting `TimeSpan`:

:::example
```scriban
{{ start = date.parse '2026-06-10'
   due = date.parse '2026-07-01'
   gap = due - start
   gap.TotalDays }} days until launch
```
```text
21 days until launch
```
:::

Addition shifts a date by an interval:

:::example
```scriban
{{ kickoff = date.parse '2026-06-10'
   review = kickoff + (timespan.from_days 7)
   review | date.to_string `%A, %Y-%m-%d` }}
```
```text
Wednesday, 2026-06-17
```
:::

Two sharp edges to know about:

**`date - timespan` is not supported.** `d - (timespan.from_days 1)`
raises *"The operator `Subtract` is not supported"*. To step a date
backwards, add a *negative* interval — `d + (timespan.from_days (-1))`
— or use `date.add_days d (-1)`.

**Parenthesise calls on the left of an operator.** Function arguments
parse greedily, so the inline form `date.parse a - date.parse b`
fails — the first call swallows the rest of the line as arguments and
one of the parses ends up with none (*"Invalid number of arguments
`0` passed to `date.parse`"*). Either wrap every call:

```scriban
{{ ((date.parse '2026-07-01') - (date.parse '2026-06-10')).TotalDays }}
```

or assign to intermediate variables first, as in the examples above —
that form is the clearest.

## Formatting with `date.to_string`

`date.to_string d pattern culture?` formats using strftime-style
specifiers. The most common ones:

| Specifier | Means |
|---|---|
| `%Y` | 4-digit year |
| `%m` | 2-digit month (01–12) |
| `%d` | 2-digit day (01–31) |
| `%H` | 2-digit hour, 24-hour clock (00–23) |
| `%M` | 2-digit minute (00–59) |
| `%S` | 2-digit second (00–59) |
| `%A` | Full weekday name |
| `%a` | Abbreviated weekday |
| `%B` | Full month name |
| `%b` | Abbreviated month |
| `%p` | AM/PM |

:::example
```scriban
{{ d = date.parse '2024-03-15 13:45:00'
   d | date.to_string `%A, %B %d, %Y at %H:%M` }}
```
```text
Friday, March 15, 2024 at 13:45
```
:::
