The `object.*` module provides general-purpose helpers that work on
ANY Scriban value — type inspection, default fallbacks, key/value
walks, JSON conversion, and dynamic evaluation. Thirteen functions.

Upstream reference:
[scriban.github.io/docs/builtins/object](https://scriban.github.io/docs/builtins/object/).

**Return types.** Every function in this module returns a new value;
the input is never mutated. Returns vary by function — bool, int,
string, array, or arbitrary value — see the `Returns` column on each
table.

**Daily drivers vs look-up material.** Four functions appear in
real templates constantly: `object.default` (a safer null-fallback than
`??` when empty strings also count), `object.keys` (walk an
unknown-shape object), `object.size` (count members or items), and
`object.typeof` (branch on type). The JSON conversion pair
(`object.from_json`, `object.to_json`) and the dynamic-evaluation pair
(`object.eval`, `object.eval_template`) are specialised power tools —
scan them so you know they exist, and reach for them when nothing
simpler fits.

## Defaults and presence

| Function | Returns | Effect |
|---|---|---|
| `object.default v fallback` | same type as `v` or `fallback` | `v` if non-null AND non-empty, else `fallback` |
| `object.has_key v k` | bool | `true` if `v` has a member named `k` |
| `object.has_value v k` | bool | `true` if `v.k` exists AND is non-null |

:::example
```scriban
{{ name = null
   object.default name 'Anonymous' }} / {{ object.default 'Ada' 'Anonymous' }}
```
```text
Anonymous / Ada
```
:::

`object.default` differs from `??` in that it also treats `''` as
"absent" — `'' | object.default 'fallback'` returns `'fallback'`,
whereas `'' ?? 'fallback'` returns `''`.

:::example
```scriban
{{ user = { name: 'Ada' }
   object.has_key user 'name' }} / {{ object.has_key user 'email' }}
```
```text
true / false
```
:::

## Inspection

| Function | Returns | Effect |
|---|---|---|
| `object.typeof v` | string | One of `string`, `boolean`, `number`, `array`, `iterator`, `object` |
| `object.kind v` | string | Finer type — `int`, `double`, `bool`, `string`, `array`, `object`, etc. |
| `object.size v` | int | Length for arrays/strings/iterators; member count for objects |

:::example
```scriban
{{ object.typeof 'hi' }} / {{ object.typeof 42 }} / {{ object.typeof [1,2] }} / {{ object.typeof null }}
```
```text
string / number / array / 
```
:::

`null` has no type — `object.typeof` returns the empty string for it.

:::example
```scriban
{{ object.kind 1 }} / {{ object.kind 1.5 }} / {{ object.kind true }}
```
```text
int / double / bool
```
:::

## Keys and values

For navigating an object whose shape is dynamic:

| Function | Returns | Effect |
|---|---|---|
| `object.keys v` | array | Array of member names |
| `object.values v` | array | Array of the corresponding values |

:::example
```scriban
{{ product = { name: 'Widget', price: 9.99 }
   object.keys product }}
{{ object.values product }}
```
```text
["name", "price"]
["Widget", 9.99]
```
:::

Pair with a `for` loop to walk an object whose members aren't known at
template-authoring time:

:::example
```scriban
{{- product = { name: 'Widget', price: 9.99 }
   for key in (object.keys product) ~}}
{{ key }}={{ product[key] }}
{{~ end -}}
```
```text
name=Widget
price=9.99
```
:::

## Formatting

`object.format value format culture?` returns **string**. The
type-agnostic cousin of `math.format` — works on numbers, dates, and
anything else with a .NET `IFormattable` implementation:

:::example
```scriban
{{ 255 | object.format 'X4' }} / {{ date.parse '2024-03-15' | object.format 'yyyy-MM' }}
```
```text
00FF / 2024-03
```
:::

(Note: `object.format` uses .NET's NATIVE format strings, not Scriban's
strftime-style `%Y/%m/%d` syntax. `yyyy-MM` is the .NET form. For
strftime-style dates, use `date.to_string` instead.)

## JSON conversion

Two functions convert between Scriban values and JSON strings:

| Function | Returns | Effect |
|---|---|---|
| `object.from_json text` | any (value/array/object from JSON) | Parse JSON into a Scriban value |
| `object.to_json value` | string | Serialise a Scriban value to JSON |

:::example
```scriban
{{ data = '{"items":[1,2,3],"count":3}' | object.from_json
   data.count }} items
```
```text
3 items
```
:::

:::example
```scriban
{{ payload = { id: 42, tags: ['a', 'b'] }
   payload | object.to_json }}
```
```text
{"id":42,"tags":["a","b"]}
```
:::

## Dynamic evaluation

Two power-tool functions that interpret a string as Scriban code.
Useful for late-bound expressions; risky if the string comes from
untrusted input.

| Function | Returns | Effect |
|---|---|---|
| `object.eval text` | any (value of the expression) | Evaluate `text` as a Scriban EXPRESSION |
| `object.eval_template text` | string (rendered output) | Evaluate `text` as a full Scriban TEMPLATE |

:::example
```scriban
{{ '1 + 2 * 3' | object.eval }} / {{ 'x = 5; x * 2' | object.eval }}
```
```text
7 / 10
```
:::

:::example
```scriban
{{ tpl = 'Hello {{ name }}!'
   data_name = 'Ada'
   name = data_name
   tpl | object.eval_template }}
```
```text
Hello Ada!
```
:::

(The inner `{{ name }}` references the surrounding scope's `name`
variable. `object.eval_template` doesn't introduce a sandbox — treat
the input as if you'd typed it into the outer template.)
