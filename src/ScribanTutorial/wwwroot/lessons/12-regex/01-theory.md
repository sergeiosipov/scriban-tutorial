The `regex.*` module exposes .NET's regex engine through six functions
— match, find-all, replace, split, escape, and unescape. Pair them with
verbatim string literals (`` `…` ``, lesson 3) so the backslashes in
pattern syntax don't need doubling.

Upstream reference:
[scriban.github.io/docs/builtins/regex](https://scriban.github.io/docs/builtins/regex/).

**Return types.** Every function in this module returns a new value;
the input is never mutated.

| Function | Returns |
|---|---|
| `regex.match` | array (full match + capture groups; empty array on no match) |
| `regex.matches` | array of arrays (one per match) |
| `regex.replace` | string |
| `regex.split` | array |
| `regex.escape` | string |
| `regex.unescape` | string |

## The two literal forms you'll use for patterns

Patterns ARE strings. The verbatim form skips escape processing, which
matters because regex syntax is full of backslashes:

| Form | Same pattern |
|---|---|
| Regular | `'\\d+'` |
| Verbatim | `` `\d+` `` |

The verbatim form is what you see in 90% of real-world Scriban regex
calls. Use it unless you specifically need an interpolated pattern (in
which case use the regular form with doubled backslashes).

## `regex.match text pattern options?`

Returns an **array**: index 0 is the whole match; indices 1+ are
capture groups. The array is empty when there's no match.

:::example
```scriban
{{ 'order 42 widget' | regex.match `(\w+)\s+(\d+)` }}
```
```text
["order 42", "order", "42"]
```
:::

Trace: pattern `(\w+)\s+(\d+)` matched `'order 42'` (group 0 — full
match), with `'order'` and `'42'` as capture groups 1 and 2.

Read individual groups by indexing the returned array:

:::example
```scriban
{{ m = 'order 42 widget' | regex.match `(\w+)\s+(\d+)`
   'verb=' + m[1] + ' qty=' + m[2] }}
```
```text
verb=order qty=42
```
:::

## `regex.matches text pattern options?`

Returns ALL matches as an **array of arrays** (each shaped like
`regex.match`'s return):

:::example
```scriban
{{ all = 'a1 b2 c3' | regex.matches `([a-z])(\d)`
   all.size }}
```
```text
3
```
:::

`regex.matches` is what you reach for when you need to scan a string
for every instance of a pattern.

## `regex.replace text pattern replacement options?`

Returns **string**. Substitutes every match with the replacement
string, which can use `$1`, `$2`, … to refer to capture groups:

:::example
```scriban
{{ 'John Smith' | regex.replace `(\w+)\s+(\w+)` `$2, $1` }}
```
```text
Smith, John
```
:::

Capture-group references make `regex.replace` more powerful than
`string.replace` for any rearrangement task.

## `regex.split text pattern options?`

Returns **array** of fragments split everywhere the pattern matches
(already covered briefly in
[lesson 3](/scriban-tutorial/lesson/03-literals) — here's the full
treatment):

:::example
```scriban
{{ 'a, b   , c,    d' | regex.split `\s*,\s*` }}
```
```text
["a", "b", "c", "d"]
```
:::

The `\s*,\s*` pattern absorbs any whitespace around each comma, so the
fragments come out clean.

## `regex.escape pattern`

Returns **string**. Escapes a string so it can be safely embedded as
a LITERAL inside a larger regex. Use this when one part of your pattern
comes from user-supplied input:

:::example
```scriban
{{ literal = '(price)*'
   'see (price)* now' | regex.replace (literal | regex.escape) 'X' }}
```
```text
see X now
```
:::

Without `regex.escape`, the user's `(price)*` would be interpreted as a
group of zero-or-more `price`s, matching every empty string — not what
the template intended.

## `regex.unescape pattern`

Returns **string**. The inverse of `regex.escape` — strips the escape
backslashes from a pattern to recover its original form. Useful when
displaying or logging a regex.

:::example
```scriban
{{ '\\(abc\\.\\*\\)' | regex.unescape }}
```
```text
(abc.*)
```
:::
