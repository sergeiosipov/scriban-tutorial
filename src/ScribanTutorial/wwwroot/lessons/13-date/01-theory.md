The `date.*` module exposes the .NET `DateTime` type to your templates
— parsing, formatting, arithmetic, and field access. Twelve functions
plus the per-instance properties for year/month/day/etc.

Upstream reference:
[scriban.github.io/docs/builtins/date](https://scriban.github.io/docs/builtins/date/).

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
{{ today = date.parse "2024-03-15"
   today | date.to_string `%Y-%m-%d` }}
```
```text
2024-03-15
```
:::

(Examples in this lesson use a fixed parsed date so the expected output
stays stable. Use `date.now` in real templates — `now` doesn't
round-trip through the test runner.)

## Parsing strings

`date.parse text pattern? culture?` reads a string into a `DateTime`.
With no pattern, .NET's culture-aware parser figures out common shapes
(`"2024-03-15"`, `"03/15/2024"`, `"15 Mar 2024"`, ISO 8601):

:::example
```scriban
{{ d = date.parse "2024-03-15"
   "Y=" + d.Year + " M=" + d.Month + " D=" + d.Day }}
```
```text
Y=2024 M=3 D=15
```
:::

For a pattern-driven parse, pass a format string (same shapes as
`date.to_string`):

```scriban
{{ date.parse "15/03/2024" `%d/%m/%Y` }}
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
{{ d = date.parse "2024-03-15 13:45:00"
   d.Hour }}:{{ d.Minute }} on day {{ d.DayOfYear }}
```
```text
13:45 on day 75
```
:::

## Adding intervals

Seven `add_*` functions return a new `DateTime` shifted by the given
amount. They don't mutate the source:

| Function | Adds |
|---|---|
| `date.add_years d n` | n years |
| `date.add_months d n` | n months |
| `date.add_days d n` | n days |
| `date.add_hours d n` | n hours |
| `date.add_minutes d n` | n minutes |
| `date.add_seconds d n` | n seconds |
| `date.add_milliseconds d n` | n milliseconds |

`n` can be negative — `add_days d -7` is "one week earlier."

:::example
```scriban
{{ start = date.parse "2024-01-15"
   later = start | date.add_months 3 | date.add_days 5
   later | date.to_string `%Y-%m-%d` }}
```
```text
2024-04-20
```
:::

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
{{ d = date.parse "2024-03-15 13:45:00"
   d | date.to_string `%A, %B %d, %Y at %H:%M` }}
```
```text
Friday, March 15, 2024 at 13:45
```
:::
