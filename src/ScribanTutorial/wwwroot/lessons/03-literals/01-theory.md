A **literal** is a value written directly in the template — no variable, no
function call. Scriban has four kinds.

## Strings

Three forms:

| Form | Notes |
|---|---|
| `"text"` or `'text'` | Regular. Supports escapes: `\n`, `\t`, `\\`, `\"`, `é`, `\x0a`. |
| `` `text` `` | Verbatim. No escape processing — useful for regex patterns. |
| `$"text {expr}"` | Interpolated. `{expr}` is evaluated inline. |

:::example
```scriban
{{ $"sum is {1 + 2}, name is {"Ada"}" }}
```
```text
sum is 3, name is Ada
```
:::

## Numbers

`100`, `1e3`, `0x1ef` (hex), `100.0`, `1.0e-3`, `100.0f` (float32),
`100.0m` (decimal). Mixing an integer and a float promotes the result to
float.

## Booleans

`true` and `false`. They render as the strings `true` / `false`.

:::example
```scriban
{{ true }}
{{ false }}
```
```text
true
false
```
:::

## `null`

The absence of a value. When rendered, it produces the empty string —
which makes typo'd variable references silently disappear from the output
instead of erroring. Be vigilant when something "doesn't render".

:::example
```scriban
before-{{ null }}-after
```
```text
before--after
```
:::
