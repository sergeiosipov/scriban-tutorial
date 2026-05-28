The `object.*` module provides general-purpose helpers that work on
ANY Scriban value — type inspection, default fallbacks, key/value
walks, JSON conversion, and dynamic evaluation. Thirteen functions.

Upstream reference:
[scriban.github.io/docs/builtins/object](https://scriban.github.io/docs/builtins/object/).

## Defaults and presence

| Function | Returns |
|---|---|
| `object.default v fallback` | `v` if non-null AND non-empty, else `fallback` |
| `object.has_key v k` | `true` if `v` has a member named `k` |
| `object.has_value v k` | `true` if `v.k` exists AND is non-null |

:::example
```scriban
{{ name = null
   object.default name "Anonymous" }} / {{ object.default "Ada" "Anonymous" }}
```
```text
Anonymous / Ada
```
:::

`object.default` differs from `??` in that it also treats `""` as
"absent" — `"" | object.default "fallback"` returns `"fallback"`,
whereas `"" ?? "fallback"` returns `""`.

:::example
```scriban
{{ user = { name: "Ada" }
   object.has_key user "name" }} / {{ object.has_key user "email" }}
```
```text
true / false
```
:::

## Inspection

| Function | Returns |
|---|---|
| `object.typeof v` | One of `string`, `boolean`, `number`, `array`, `iterator`, `object` |
| `object.kind v` | Finer type — `int`, `double`, `bool`, `string`, `array`, `object`, etc. |
| `object.size v` | Length for arrays/strings/iterators; member count for objects |

:::example
```scriban
{{ object.typeof "hi" }} / {{ object.typeof 42 }} / {{ object.typeof [1,2] }} / {{ object.typeof null }}
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

| Function | Returns |
|---|---|
| `object.keys v` | Array of member names |
| `object.values v` | Array of the corresponding values |

:::example
```scriban
{{ product = { name: "Widget", price: 9.99 }
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
{{- product = { name: "Widget", price: 9.99 }
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

`object.format value format culture?` is the type-agnostic
`math.format` — it works on numbers, dates, and anything else with a
.NET `IFormattable` implementation:

:::example
```scriban
{{ 255 | object.format "X4" }} / {{ date.parse "2024-03-15" | object.format "yyyy-MM" }}
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

| Function | Effect |
|---|---|
| `object.from_json text` | Parse JSON into a Scriban value |
| `object.to_json value` | Serialise a Scriban value to JSON |

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
{{ payload = { id: 42, tags: ["a", "b"] }
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

| Function | Effect |
|---|---|
| `object.eval text` | Evaluate `text` as a Scriban EXPRESSION; return its value |
| `object.eval_template text` | Evaluate `text` as a full Scriban TEMPLATE; return the rendered output |

:::example
```scriban
{{ "1 + 2 * 3" | object.eval }} / {{ "x = 5; x * 2" | object.eval }}
```
```text
7 / 10
```
:::

:::example
```scriban
{{ tpl = "Hello {{ name }}!"
   data_name = "Ada"
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
