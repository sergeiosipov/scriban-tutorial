Expressions are anything that **evaluates to a value**: literals, variable
reads, arithmetic, comparisons, function calls. This lesson walks the
operators in the order you'll reach for them.

## Arithmetic on numbers

`+`, `-`, `*`, `/`, `//` (integer division), `%` (modulus). Mixing int
and float promotes to float.

:::example
```scriban
{{ a + b }} and {{ a * b }}
```
```json
{ "a": 4, "b": 3 }
```
```text
7 and 12
```
:::

## Strings: concat and repeat

`+` glues two strings. `*` repeats a string a number of times.

:::example
```scriban
{{ "ab" + "c" }}
{{ "ha" * 3 }}
```
```text
abc
hahaha
```
:::

## Strings and numbers in the same expression

If either side of `+` is a string, the other is **coerced to a string**:
`"5" + 3 → "53"` (concat), while `5 + 3 → 8` (arithmetic). The JSON
shape of your data matters — `"qty": "4"` and `"qty": 4` behave very
differently in templates.

## Comparison and logic

`==`, `!=`, `<`, `<=`, `>`, `>=` return booleans. Combine with `&&` and
`||`. The ternary `cond ? a : b` is the inline conditional.

:::example
```scriban
{{ price > 100 ? "expensive" : "ok" }}
```
```json
{ "price": 250 }
```
```text
expensive
```
:::

## Ranges

`a..b` and `a..<b` are iterators. `1..5` yields 1 2 3 4 5; `1..<5`
yields 1 2 3 4. Mostly seen in `for` loops (lesson 09).

## Null-coalescing — `??` and `?!`

`a ?? b` is `a` when `a` is not null, otherwise `b`.
`a ?! b` is the opposite — `b` when `a` is not null, else `null`.

## Pipes

`a | f` passes `a` as the first argument to `f`. Multiple stages chain
naturally:

:::example
```scriban
{{ "  ada  " | string.strip | string.upcase }}
```
```text
ADA
```
:::

Pipes are whitespace-greedy enough to span lines:

```scriban
{{- "text"
    | string.append "END"
    | string.prepend "START"
-}}
```

renders `STARTtextEND`.

## Built-in filter modules

The modules you'll meet most often. Browse the full list at
<https://scriban.github.io/docs/built-ins/>.

| Module | Examples |
|---|---|
| `string` | `string.upcase`, `string.size`, `string.append`, `string.strip` |
| `array`  | `array.size`, `array.first`, `array.sort`, `array.join` |
| `object` | `object.keys`, `object.values`, `object.size` |
| `math`   | `math.round`, `math.format`, `math.abs` |
| `date`   | `date.now`, `date.to_string`, `date.parse` |
| `regex`  | `regex.match`, `regex.replace`, `regex.split` |
