A **literal** is a value written directly in the template — no variable, no
function call. Scriban has four kinds: strings, numbers, booleans, and
`null`.

## Strings

Three forms.

### Regular: `"..."` and `'..'`

Double-quoted and single-quoted strings are interchangeable. They both
process backslash escapes, so use single quotes when the string itself
contains `"`, and double when it contains `'`:

:::example
```scriban
{{ "She said \"hi\"" }}
{{ 'She said "hi"' }}
```
```text
She said "hi"
She said "hi"
```
:::

Supported escapes:

| Escape | Produces |
|---|---|
| `\n` | newline |
| `\t` | tab |
| `\r` | carriage return |
| `\\` | backslash |
| `\"` / `\'` | matching quote |
| `\xHH` | byte with hex value `HH` (2 hex digits) |
| `\uHHHH` | Unicode code point `U+HHHH` (4 hex digits) |

:::example
```scriban
{{ "line1\nline2\tindented" }}
```
```text
line1
line2	indented
```
:::

:::example
```scriban
{{ "letter \x42 then smörgåsbord" }}
```
```text
letter B then smörgåsbord
```
:::

`\xHH` is handy when you need an exact byte but spelling out the symbol is
awkward (control characters, separators in CSV/TSV output). `\uHHHH` covers
the rest of Unicode — accented characters, currency symbols, emoji-adjacent
glyphs.

### Verbatim: `` `text` ``

Backticks turn off escape processing — every character in the body is
literal, including `\`. The classic use case is regex patterns:

:::example
```scriban
{{ "this is a text" | regex.split `\s+` }}
```
```text
["this", "is", "a", "text"]
```
:::

Without verbatim, you would have to write `"\\s+"` — double the backslashes
to keep them past the string parser. Verbatim is also handy for Windows
paths (`` `C:\Users\you` ``) and any literal containing the escape
characters above.

### Interpolated: `$"..."` and `$'...'`

Prefix a string with `$` to evaluate `{expr}` inside it. The form works
with both double and single quotes:

:::example
```scriban
{{ $"sum is {1 + 2}, name is {"Ada"}" }}
```
```text
sum is 3, name is Ada
```
:::

:::example
```scriban
{{ $'sum is {1 + 2}, says "Ada"' }}
```
```text
sum is 3, says "Ada"
```
:::

Use single-quoted interpolation when the surrounding text already contains
double quotes — saves you the `\"` escapes.

## Numbers

Scriban accepts the same numeric literal shapes as C# itself, with similar
suffixes:

| Literal | Type | When you'd write it |
|---|---|---|
| `100` | int (32-bit signed) | Default for whole numbers. |
| `0x1ef` | int (hex) | Bit patterns, address-like values, byte masks. Spells `495`. |
| `0x80000000u` | uint | Hex literals above `0x7fffffff` need the `u` suffix — the value won't fit in a signed `int`. |
| `1e3` | double | Scientific notation. Spells `1000`. Use when zero-counting hurts readability — `1e9` reads cleaner than `1000000000`. |
| `100.0` | double (64-bit) | Default for fractional numbers; gives ~15 significant digits. |
| `1.0e-3` | double | Tiny scientific values without the leading zeros. Spells `0.001`. |
| `100.0f` | float (32-bit) | Half the memory of `double`, ~7 significant digits. Useful when the .NET host expects `Single` (graphics, certain APIs). |
| `100.0d` | double | Explicit form; same as `100.0`. Helps when you're side-by-side with `f` and `m` literals and want to be clear. |
| `100.0m` | decimal | Exact base-10 arithmetic. **Use for money** — `0.1 + 0.2 == 0.3` is FALSE for `double` and TRUE for `decimal`. |

Mixing an integer and a float promotes the result to float. `100 / 3`
gives `33`; `100 / 3.0` gives `33.333…`.

For arithmetic-heavy work, the built-in `math.*` module ships ceiling,
floor, round, abs, min/max, and similar — see
[lesson 10](/scriban-tutorial/lesson/10-math).

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

Three operators combine them:

| Operator | Reads | Example |
|---|---|---|
| `!` | not | `!true` → `false` |
| `&&` | and (both) | `true && false` → `false` |
| `\|\|` | or (either) | `true \|\| false` → `true` |

:::example
```scriban
{{ a = true; b = false
   !a }} / {{ a && b }} / {{ a || b }}
```
```text
false / false / true
```
:::

`&&` and `||` short-circuit — if the left side determines the answer, the
right side is never evaluated. Useful when the right side could fail or do
something expensive: `user && user.name` won't blow up when `user` is null.

## `null`

The absence of a value. When rendered, it produces the empty string —
which makes typo'd variable references silently disappear from the output
instead of raising an error.

:::example
```scriban
before-{{ null }}-after
```
```text
before--after
```
:::

That silent-empty behaviour is the most common source of "why doesn't my
template render anything?" bugs. Three patterns to catch it:

1. **Explicit equality** — `{{ if x == null }}…{{ end }}` branches on
   nullness. Use when you want to handle the null case differently from
   "the value is the empty string" or "the value is `false`".
2. **Null-coalescing `??`** — `{{ x ?? "fallback" }}` renders `x` if it
   exists, otherwise renders the fallback. The whole topic gets its own
   section in [lesson 8](/scriban-tutorial/lesson/08-expressions).
3. **Required-field guard** — when a field MUST be set for the template to
   make sense, fail loudly: `{{ if !email; "MISSING_EMAIL"; else; email;
   end }}`. Better an obvious placeholder than a blank in production
   output.

:::example
```scriban
{{ x = null
   x ?? "default" }} / {{ if x == null }}absent{{ else }}present{{ end }}
```
```text
default / absent
```
:::
