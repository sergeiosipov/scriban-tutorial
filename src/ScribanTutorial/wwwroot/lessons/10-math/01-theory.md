The `math.*` module covers the arithmetic, rounding, formatting, and
random utilities that every template eventually needs — currency
totals, percentages, capped values, sortable IDs. Thirteen functions in
total.

Upstream reference:
[scriban.github.io/docs/builtins/math](https://scriban.github.io/docs/builtins/math/).

**Return types.** Every function in this module returns a new value;
the input is never mutated. Most return **numbers** (int or double
following the input). Three exceptions: `math.format` and `math.uuid`
return **string**; `math.is_number` returns **bool**.

## Basic arithmetic

The four basic operators have pipe-friendly companions in the math
module. They're rarely cleaner than the binary forms (`a + b`, `a - b`,
etc.), but they shine in pipe chains where each step transforms the
value flowing through:

| Function | Returns | Operator equivalent | Example |
|---|---|---|---|
| `math.plus a b` | number | `a + b` | `{{ 1 \| math.plus 2 }}` → `3` |
| `math.minus a b` | number | `a - b` | `{{ 5 \| math.minus 2 }}` → `3` |
| `math.times a b` | number | `a * b` | `{{ 2 \| math.times 3 }}` → `6` |
| `math.divided_by a b` | number | `a / b`, floored when divisor is int | `{{ 8.4 \| math.divided_by 2 }}` → `4` |
| `math.modulo a b` | number | `a % b` | `{{ 11 \| math.modulo 10 }}` → `1` |

:::example
```scriban
{{ 100 | math.times 1.2 | math.minus 5 }}
```
```text
115
```
:::

`100` flows in, gets multiplied by `1.2` → `120`, then `5` is
subtracted → `115`.

## Rounding

`math.ceil`, `math.floor`, and `math.round` cover the three standard
behaviours:

| Function | Returns | Effect | Example |
|---|---|---|---|
| `math.ceil x` | number | Round up | `{{ 4.2 \| math.ceil }}` → `5` |
| `math.floor x` | number | Round down | `{{ 4.8 \| math.floor }}` → `4` |
| `math.round x precision?` | number | Round to N decimal places (default 0) | `{{ 4.5612 \| math.round 2 }}` → `4.56` |

:::example
```scriban
{{ 7.5 | math.ceil }} / {{ 7.5 | math.floor }} / {{ math.round 7.5 }} / {{ math.round 3.14159 2 }}
```
```text
8 / 7 / 8 / 3.14
```
:::

`math.round` uses banker's rounding (.NET's `Math.Round`) — `0.5`
rounds to the nearest even integer, so `7.5 → 8` and `8.5 → 8` (not
`9`).

## Absolute value

`math.abs` strips the sign:

:::example
```scriban
{{ -15.5 | math.abs }} / {{ 15.5 | math.abs }}
```
```text
15.5 / 15.5
```
:::

## Number formatting

`math.format value format culture?` formats a number using a .NET
numeric format string. Useful for hex, currency, padding, and
fixed-width display:

| Format | Effect | Example |
|---|---|---|
| `"X4"` | Hex, 4 digits | `{{ 255 \| math.format "X4" }}` → `00FF` |
| `"D6"` | Decimal padded to 6 | `{{ 42 \| math.format "D6" }}` → `000042` |
| `"N2"` | Number, 2 decimal places | `{{ 1234.5 \| math.format "N2" }}` → `1,234.50` |
| `"P1"` | Percent, 1 decimal | `{{ 0.125 \| math.format "P1" }}` → `12.5%` |
| `"C"` | Currency (locale-dependent) | `{{ 99.95 \| math.format "C" }}` → `$99.95` (en-US) |

:::example
```scriban
{{ 255 | math.format 'X4' }} / {{ 42 | math.format 'D6' }} / {{ 0.125 | math.format 'P1' }}
```
```text
00FF / 000042 / 12.5 %
```
:::

(Whitespace inside `P1` output varies by .NET runtime — recent .NET
versions insert a non-breaking space before the `%`.)

## Type test

`math.is_number` returns `true` when the input is numeric:

:::example
```scriban
{{ 255 | math.is_number }} / {{ '255' | math.is_number }}
```
```text
true / false
```
:::

Note that a numeric string like `"255"` is NOT a number — `math.is_number`
checks the runtime type, not whether the value parses. Pair with
`string.to_int` (lesson 11) when you need to coerce. (Passing `null`
through `math.is_number` raises a runtime error — guard with `??` if
the input might be null.)

## IDs and randomness

`math.uuid` generates a fresh UUID (Version 4) — useful for stable
identifiers in generated output:

:::example
```scriban
{{ id = math.uuid
   'id length is ' + id.size }}
```
```text
id length is 36
```
:::

`math.random min max` generates a random integer in `[min, max]`. The
result is non-deterministic by design, so the lesson-runner can't show
a fixed expected output — use it interactively for cache-busting,
sample data, or stub identifiers.

```scriban
{{ for i in 1..3 }}{{ math.random 1 100 }} {{ end }}
```

Each render produces a different sequence.
