The `timespan.*` module wraps .NET's `TimeSpan` — an interval of time
(not a moment), like "5 hours" or "3 days 12 minutes." Six constructor
functions, one parser, plus per-instance properties.

Upstream reference:
[scriban.github.io/docs/builtins/timespan](https://scriban.github.io/docs/builtins/timespan/).

**Return types.** Every function in this module returns a new
**TimeSpan** value; nothing mutates the input. The per-instance
properties (`.Hours`, `.TotalMinutes`, etc.) return **int** for
component accessors and **double** for total accessors.

## Constructors

Six builders that each produce a new `TimeSpan` from a single unit:

| Function | Returns | Effect |
|---|---|---|
| `timespan.from_days n` | TimeSpan | n days |
| `timespan.from_hours n` | TimeSpan | n hours |
| `timespan.from_minutes n` | TimeSpan | n minutes |
| `timespan.from_seconds n` | TimeSpan | n seconds |
| `timespan.from_milliseconds n` | TimeSpan | n milliseconds |
| `timespan.parse text` | TimeSpan | Parse from `'d.HH:MM:SS'` or `'HH:MM:SS'` |

`n` can be fractional — `timespan.from_hours 1.5` is "one and a half
hours". Rendered directly, a `TimeSpan` prints in `HH:MM:SS` form (or
`d.HH:MM:SS` when ≥ 24 hours):

:::example
```scriban
{{ timespan.from_minutes 90 }} / {{ timespan.from_hours 25 }}
```
```text
01:30:00 / 1.01:00:00
```
:::

## Per-instance properties (PascalCase)

Same situation as the date module — the host's `MemberRenamer` keeps
.NET property names verbatim, so use PascalCase. The upstream
snake_case forms (`.days`, `.total_minutes`) return empty in this app.

Components — the integer part of each unit, what you'd write on a
clock:

| Property | Range |
|---|---|
| `.Days` | days component |
| `.Hours` | 0–23 (hours within the day) |
| `.Minutes` | 0–59 |
| `.Seconds` | 0–59 |
| `.Milliseconds` | 0–999 |

Totals — the whole interval expressed as a fractional count of one
unit:

| Property | Means |
|---|---|
| `.TotalDays` | total days, including fractions |
| `.TotalHours` | total hours |
| `.TotalMinutes` | total minutes |
| `.TotalSeconds` | total seconds |
| `.TotalMilliseconds` | total milliseconds |

The difference between "components" and "totals" is the difference
between *clock time* and *duration*. A 90-minute interval has
`.Minutes == 30` (the clock part: "30 minutes past an hour") and
`.TotalMinutes == 90` (the duration: "90 minutes total").

:::example
```scriban
{{ t = timespan.from_minutes 90
   'hours-comp=' + t.Hours + ' min-comp=' + t.Minutes + ' total-min=' + t.TotalMinutes }}
```
```text
hours-comp=1 min-comp=30 total-min=90
```
:::

## Combining timespans

The arithmetic `+` and `-` operators don't work on `TimeSpan` values in
this app — `(a + b)` raises *"Unsupported types"*. To combine intervals,
sum their `.TotalSeconds` (or whichever unit matches your need) and
build a new `TimeSpan` from the result:

:::example
```scriban
{{ a = timespan.from_hours 2
   b = timespan.from_minutes 30
   combined = timespan.from_seconds (a.TotalSeconds + b.TotalSeconds)
   combined }}
```
```text
02:30:00
```
:::

## Parsing

`timespan.parse text` accepts the same format `TimeSpan` renders:

:::example
```scriban
{{ t = timespan.parse '1.02:30:00'
   'days=' + t.Days + ' hours=' + t.Hours + ' minutes=' + t.Minutes }}
```
```text
days=1 hours=2 minutes=30
```
:::

`'1.02:30:00'` reads as one day, two hours, thirty minutes — `1.26:30:00`
in totals.
