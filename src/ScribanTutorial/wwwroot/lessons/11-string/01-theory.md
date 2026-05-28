The `string.*` module is the largest built-in by function count —
**47 functions** covering case, whitespace, search, slicing, replace,
conversion, padding, encoding, and hashing. You'll use most of them.

Upstream reference:
[scriban.github.io/docs/builtins/string](https://scriban.github.io/docs/builtins/string/).

## Case transformations

| Function | Effect | Example |
|---|---|---|
| `string.upcase x` | All uppercase | `"test" \| string.upcase` → `TEST` |
| `string.downcase x` | All lowercase | `"TeSt" \| string.downcase` → `test` |
| `string.capitalize x` | First letter upper, rest unchanged | `"test" \| string.capitalize` → `Test` |
| `string.capitalizewords x` | First letter of each word upper | `"this is easy" \| string.capitalizewords` → `This Is Easy` |
| `string.equals_ignore_case a b` | Case-insensitive `==` | `"Scriban" \| string.equals_ignore_case "SCRIBAN"` → `true` |

:::example
```scriban
{{ "hello world" | string.capitalizewords }}
```
```text
Hello World
```
:::

## Whitespace

| Function | Effect | Example |
|---|---|---|
| `string.strip x` | Trim both sides | `"  ada  " \| string.strip` → `ada` |
| `string.lstrip x` | Trim leading whitespace | `"   ada" \| string.lstrip` → `ada` |
| `string.rstrip x` | Trim trailing whitespace | `"ada   " \| string.rstrip` → `ada` |
| `string.strip_newlines x` | Remove `\n` and `\r` | `"a\nb\r\nc" \| string.strip_newlines` → `abc` |
| `string.pad_left x w` | Left-pad to width `w` with spaces | `"x" \| string.pad_left 5` → `    x` |
| `string.pad_right x w` | Right-pad to width `w` with spaces | `"x" \| string.pad_right 5` → `x    ` |

:::example
```scriban
[{{ "  ada  " | string.strip }}] / [{{ "x" | string.pad_left 5 }}] / [{{ "x" | string.pad_right 5 }}]
```
```text
[ada] / [    x] / [x    ]
```
:::

## Inspection and search

Predicates and look-ups that return information ABOUT a string without
changing it:

| Function | Effect | Example |
|---|---|---|
| `string.size x` | Character count | `"test" \| string.size` → `4` |
| `string.empty x` | True iff string is `""` | `"" \| string.empty` → `true` |
| `string.whitespace x` | True iff empty or whitespace-only | `"   " \| string.whitespace` → `true` |
| `string.contains x sub` | Substring present? | `"hello" \| string.contains "ell"` → `true` |
| `string.starts_with x sub` | Prefix check | `"hello" \| string.starts_with "he"` → `true` |
| `string.ends_with x sub` | Suffix check | `"hello" \| string.ends_with "lo"` → `true` |
| `string.index_of x sub` | 0-based index, or `-1` | `"hello" \| string.index_of "ll"` → `2` |

:::example
```scriban
{{ "scriban-tutorial" | string.contains "tutorial" }} / {{ "scriban-tutorial" | string.index_of "-" }}
```
```text
true / 7
```
:::

## Slicing and truncation

| Function | Effect | Example |
|---|---|---|
| `string.slice x start length?` | Substring; omit `length` to go to end | `"hello" \| string.slice 1 3` → `ell` |
| `string.slice1 x start length?` | Same but defaults `length` to 1 | `"hello" \| string.slice1 0` → `h` |
| `string.truncate x len ellipsis?` | Truncate w/ `…` (default) | `"hello world" \| string.truncate 8` → `hello... ` |
| `string.truncatewords x n ellipsis?` | Truncate to first `n` words | `"a b c d e" \| string.truncatewords 3` → `a b c...` |

`string.truncate` keeps the ellipsis WITHIN the length budget, so
`truncate "hello world" 8` produces `"hello..."` (8 chars), not
`"hello wo..."`.

:::example
```scriban
{{ "The quick brown fox" | string.truncate 14 }}
{{ "The quick brown fox" | string.truncatewords 2 }}
```
```text
The quick b...
The quick...
```
:::

## Replace and remove

| Function | Effect | Example |
|---|---|---|
| `string.replace x m r` | Replace all occurrences | `"a-b-c" \| string.replace "-" "/"` → `a/b/c` |
| `string.replace_first x m r` | Replace first occurrence only | `"a-b-c" \| string.replace_first "-" "/"` → `a/b-c` |
| `string.remove x m` | Remove all occurrences | `"foo-bar-baz" \| string.remove "bar"` → `foo--baz` |
| `string.remove_first x m` | Remove first occurrence only | `"x x x" \| string.remove_first "x"` → ` x x` |
| `string.remove_last x m` | Remove last occurrence only | `"x x x" \| string.remove_last "x"` → `x x ` |

:::example
```scriban
{{ "hello, world, hello" | string.replace_first "hello" "hi" }}
```
```text
hi, world, hello
```
:::

## Combine and split

| Function | Effect | Example |
|---|---|---|
| `string.append x y` | Concat (right) | `"a" \| string.append "b"` → `ab` |
| `string.prepend x y` | Concat (left) | `"a" \| string.prepend "b"` → `ba` |
| `string.split x sep` | Split into array | `"a,b,c" \| string.split ","` → `["a","b","c"]` |

`string.split` returns an array — pipe it through `array.*` filters
(lesson 16) for further work.

:::example
```scriban
{{ ("a-b-c" | string.split "-")[1] }}
```
```text
b
```
:::

## Conversion to numbers

| Function | Returns | Example |
|---|---|---|
| `string.to_int x` | 32-bit int | `"42" \| string.to_int` → `42` |
| `string.to_long x` | 64-bit int | `"1234567890123" \| string.to_long` → `1234567890123` |
| `string.to_float x` | 32-bit float | `"1.5" \| string.to_float` → `1.5` |
| `string.to_double x` | 64-bit float | `"1.5" \| string.to_double` → `1.5` |

:::example
```scriban
{{ "12" | string.to_int + 3 }}
```
```text
15
```
:::

## Pluralisation

`string.pluralize n singular plural` picks the form by count:

:::example
```scriban
{{ 1 | string.pluralize "item" "items" }}, {{ 5 | string.pluralize "item" "items" }}
```
```text
item, items
```
:::

## URL-style normalisation

| Function | Effect | Example |
|---|---|---|
| `string.handleize x` | URL-friendly slug | `"100% M & Ms!" \| string.handleize` → `100-m-ms` |
| `string.literal x` | Return as a quoted literal | `'Hi "yo"' \| string.literal` → `"Hi \"yo\""` |
| `string.escape x` | Show escapes literally | `"a\tb" \| string.escape` → `a\tb` |

:::example
```scriban
{{ "Hello, World! 123" | string.handleize }}
```
```text
hello-world-123
```
:::

## Encoding

Base64 round-tripping:

:::example
```scriban
{{ "hello" | string.base64_encode }} / {{ "aGVsbG8=" | string.base64_decode }}
```
```text
aGVsbG8= / hello
```
:::

## Hashing

Six hashing helpers, useful for cache keys and integrity checks. The
HMAC variants take a secret key.

| Function | Output |
|---|---|
| `string.md5 x` | MD5 (32 hex chars) — legacy only |
| `string.sha1 x` | SHA-1 (40 hex chars) — also legacy |
| `string.sha256 x` | SHA-256 (64 hex chars) — current default |
| `string.sha512 x` | SHA-512 (128 hex chars) |
| `string.hmac_sha1 x secret` | Keyed SHA-1 (40 hex chars) |
| `string.hmac_sha256 x secret` | Keyed SHA-256 (64 hex chars) |
| `string.hmac_sha512 x secret` | Keyed SHA-512 (128 hex chars) |

Don't use MD5 or SHA-1 for security-sensitive comparisons — collision
attacks make them unsuitable for password hashing or message
authentication. Both still work as fast non-cryptographic checksums.

:::example
```scriban
{{ ("hello" | string.sha256).size }}
```
```text
64
```
:::
