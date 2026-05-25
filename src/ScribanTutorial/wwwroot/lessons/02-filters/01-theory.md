A **filter** transforms a value. You apply a filter with the pipe operator `|`,
which feeds the value on its left into the function on its right. The result of
the whole expression is the final filter's output.

```scriban
{{ name | string.upcase }}
```

If `name` is `"ada"`, the expression renders `ADA`.

## Chaining filters

Pipes chain left to right:

:::example
```scriban
{{ name | string.strip | string.upcase }}
```
```json
{ "name": "  ada  " }
```
```text
ADA
```
:::

The value flows `name` → `string.strip` (`"ada"`) → `string.upcase` (`"ADA"`).

## Built-in filter modules

Scriban groups filters into modules. The ones you'll meet most often:

| Module | Examples |
|---|---|
| `string` | `string.upcase`, `string.downcase`, `string.capitalize`, `string.size`, `string.strip` |
| `array`  | `array.size`, `array.first`, `array.last`, `array.join`, `array.sort` |
| `object` | `object.keys`, `object.values`, `object.size` |
| `math`   | `math.round`, `math.format`, `math.abs`, `math.ceil`, `math.floor` |
| `date`   | `date.to_string`, `date.now`, `date.parse` |
| `regex`  | `regex.match`, `regex.replace`, `regex.split` |

Filters with extra arguments take them after the function name:

```scriban
{{ price | math.format "0.00" }}
```

## Arithmetic without filters

You don't always need a filter — Scriban supports the usual operators on
numbers (`+ - * / % ^`), strings (`+` concatenates), and ranges (`1..5`):

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

Mind types: `{{ "5" + 3 }}` is `"53"` (string concat), while `{{ 5 + 3 }}` is
`8`. The exercises below use whole numbers so float surprises stay out of the
way.
