The `string.*` module is the largest built-in by function count —
**47 functions** covering case, whitespace, search, slicing, replace,
conversion, padding, encoding, and hashing. You'll use most of them.

Upstream reference:
[scriban.github.io/docs/builtins/string](https://scriban.github.io/docs/builtins/string/).

**Return types.** Every function in this module returns a new value;
the input string is never mutated. The `Returns` column on each table
gives the specific type — most return **string**, a handful return
**bool** (predicates), **int** (size, index_of, parsers), or **array**
(split).

**Daily drivers vs look-up material.** With 47 functions this is the
largest module — don't try to memorise all of it. The handful you'll
use in almost every real template: `string.upcase`, `string.downcase`,
`string.strip`, `string.split`, `string.replace`, `string.append`,
`string.prepend`, `string.contains`, `string.starts_with`, and
`string.to_int`. The hashing functions (`string.sha256`,
`string.hmac_sha256`, etc.), encoding (`string.base64_encode`),
and look-up-heavy utilities (`string.handleize`, `string.truncate`,
`string.pad_left`) are reference material — bookmark the section and
come back when you need them.

## Case transformations

Reach for these when normalising text for display: uppercasing a
header, lowercasing a search term before comparing, or title-casing
user-entered names. `string.equals_ignore_case` is the right choice
for equality checks that should be case-insensitive without mutating
either side.

| Function | Returns | Effect | Example |
|---|---|---|---|
| `string.upcase x` | string | All uppercase | `'test' \| string.upcase` → `TEST` |
| `string.downcase x` | string | All lowercase | `'TeSt' \| string.downcase` → `test` |
| `string.capitalize x` | string | First letter upper, rest unchanged | `'test' \| string.capitalize` → `Test` |
| `string.capitalizewords x` | string | First letter of each word upper | `'this is easy' \| string.capitalizewords` → `This Is Easy` |
| `string.equals_ignore_case a b` | bool | Case-insensitive `==` | `'Scriban' \| string.equals_ignore_case 'SCRIBAN'` → `true` |

:::example
```scriban
{{ 'hello world' | string.capitalizewords }}
```
```text
Hello World
```
:::

## Whitespace

Reach for these when data arrives with extra padding — user input,
CSV cells, imported text — or when building fixed-width columns.
`strip` is the default first pass; `pad_left` / `pad_right` are for
generating aligned tabular output.

| Function | Returns | Effect | Example |
|---|---|---|---|
| `string.strip x` | string | Trim both sides | `'  ada  ' \| string.strip` → `ada` |
| `string.lstrip x` | string | Trim leading whitespace | `'   ada' \| string.lstrip` → `ada` |
| `string.rstrip x` | string | Trim trailing whitespace | `'ada   ' \| string.rstrip` → `ada` |
| `string.strip_newlines x` | string | Remove `\n` and `\r` | `'a\nb\r\nc' \| string.strip_newlines` → `abc` |
| `string.pad_left x w` | string | Left-pad to width `w` with spaces | `'x' \| string.pad_left 5` → `    x` |
| `string.pad_right x w` | string | Right-pad to width `w` with spaces | `'x' \| string.pad_right 5` → `x    ` |

:::example
```scriban
[{{ '  ada  ' | string.strip }}] / [{{ 'x' | string.pad_left 5 }}] / [{{ 'x' | string.pad_right 5 }}]
```
```text
[ada] / [    x] / [x    ]
```
:::

## Inspection and search

Reach for these when you need to branch on what a string contains
without changing it: guard against empty fields (`empty`, `whitespace`),
drive routing logic on a URL path (`starts_with`, `ends_with`), or
locate a delimiter before slicing (`index_of`). These are read-only —
they never modify the input.

Predicates and look-ups that return information ABOUT a string without
changing it:

| Function | Returns | Effect | Example |
|---|---|---|---|
| `string.size x` | int | Character count | `'test' \| string.size` → `4` |
| `string.empty x` | bool | True iff string is `''` | `'' \| string.empty` → `true` |
| `string.whitespace x` | bool | True iff empty or whitespace-only | `'   ' \| string.whitespace` → `true` |
| `string.contains x sub` | bool | Substring present? | `'hello' \| string.contains 'ell'` → `true` |
| `string.starts_with x sub` | bool | Prefix check | `'hello' \| string.starts_with 'he'` → `true` |
| `string.ends_with x sub` | bool | Suffix check | `'hello' \| string.ends_with 'lo'` → `true` |
| `string.index_of x sub` | int | 0-based index, or `-1` | `'hello' \| string.index_of 'll'` → `2` |

:::example
```scriban
{{ 'scriban-tutorial' | string.contains 'tutorial' }} / {{ 'scriban-tutorial' | string.index_of '-' }}
```
```text
true / 7
```
:::

## Slicing and truncation

Reach for `string.slice` when the format is fixed and you know
exactly where the piece you want starts (area code from a phone
number, first two chars of an ISO code, digits before a separator).
Reach for `string.truncate` / `string.truncatewords` when generating
list previews or UI summaries that need a hard character or word cap.

| Function | Returns | Effect | Example |
|---|---|---|---|
| `string.slice x start length?` | string | Substring; omit `length` to go to end | `'hello' \| string.slice 1 3` → `ell` |
| `string.slice1 x start length?` | string | Same but defaults `length` to 1 | `'hello' \| string.slice1 0` → `h` |
| `string.truncate x len ellipsis?` | string | Truncate w/ `…` (default) | `'hello world' \| string.truncate 8` → `hello... ` |
| `string.truncatewords x n ellipsis?` | string | Truncate to first `n` words | `'a b c d e' \| string.truncatewords 3` → `a b c...` |

`string.slice` is the substring workhorse — here it pulls the area
code out of a phone number:

:::example
```scriban
{{ phone | string.slice 0 3 }}
```
```json
{ "phone": "415-555-1234" }
```
```text
415
```
:::

`string.truncate` keeps the ellipsis WITHIN the length budget, so
`truncate 'hello world' 8` produces `'hello...'` (8 chars), not
`'hello wo...'`.

:::example
```scriban
{{ 'The quick brown fox' | string.truncate 14 }}
{{ 'The quick brown fox' | string.truncatewords 2 }}
```
```text
The quick b...
The quick...
```
:::

## Replace and remove

Reach for these when you need to rewrite content in-place: swap a
delimiter, redact a token, or clean up a known pattern. For pattern-
based replacements (anything with wildcards or character classes), use
`regex.replace` from lesson 12 instead.

| Function | Returns | Effect | Example |
|---|---|---|---|
| `string.replace x m r` | string | Replace all occurrences | `'a-b-c' \| string.replace '-' '/'` → `a/b/c` |
| `string.replace_first x m r` | string | Replace first occurrence only | `'a-b-c' \| string.replace_first '-' '/'` → `a/b-c` |
| `string.remove x m` | string | Remove all occurrences | `'foo-bar-baz' \| string.remove 'bar'` → `foo--baz` |
| `string.remove_first x m` | string | Remove first occurrence only | `'x x x' \| string.remove_first 'x'` → ` x x` |
| `string.remove_last x m` | string | Remove last occurrence only | `'x x x' \| string.remove_last 'x'` → `x x ` |

:::example
```scriban
{{ 'hello, world, hello' | string.replace_first 'hello' 'hi' }}
```
```text
hi, world, hello
```
:::

## Combine and split

Reach for `string.append` / `string.prepend` when building a string
from labelled parts — they read left-to-right in a pipe chain. Reach
for `string.split` when a delimited value (CSV field, path, tag list)
needs to become an array so you can loop over it or pass it through
`array.*` filters.

| Function | Returns | Effect | Example |
|---|---|---|---|
| `string.append x y` | string | Concat (right) | `'a' \| string.append 'b'` → `ab` |
| `string.prepend x y` | string | Concat (left) | `'a' \| string.prepend 'b'` → `ba` |
| `string.split x sep` | array | Split into array | `'a,b,c' \| string.split ','` → `['a','b','c']` |

`string.split` returns an array — pipe it through `array.*` filters
(lesson 16) for further work.

:::example
```scriban
{{ ('a-b-c' | string.split '-')[1] }}
```
```text
b
```
:::

## Conversion to numbers

Reach for these when JSON data arrives with numeric fields typed as
strings — a common pattern in form submissions and legacy APIs. Parse
once, then do all your arithmetic with the result; arithmetic on the
original string will silently concatenate instead of add.

| Function | Returns | Effect | Example |
|---|---|---|---|
| `string.to_int x` | int (32-bit) | Parse decimal integer | `'42' \| string.to_int` → `42` |
| `string.to_long x` | int (64-bit) | Parse decimal long | `'1234567890123' \| string.to_long` → `1234567890123` |
| `string.to_float x` | float (32-bit) | Parse decimal float | `'1.5' \| string.to_float` → `1.5` |
| `string.to_double x` | double (64-bit) | Parse decimal double | `'1.5' \| string.to_double` → `1.5` |

:::example
```scriban
{{ '12' | string.to_int + 3 }}
```
```text
15
```
:::

## Pluralisation

Reach for this whenever you display a count next to a noun:
`{{ count | string.pluralize 'result' 'results' }}` saves an `if`
branch and reads cleanly in the template.

`string.pluralize n singular plural` returns **string** — picks the
form by count:

:::example
```scriban
{{ 1 | string.pluralize 'item' 'items' }}, {{ 5 | string.pluralize 'item' 'items' }}
```
```text
item, items
```
:::

## URL-style normalisation

Reach for `string.handleize` when you need a URL-safe slug from
arbitrary user text (product names, article titles, tag labels).
`string.literal` and `string.escape` are diagnostic tools — useful
when debugging what escape sequences a string actually contains.

| Function | Returns | Effect | Example |
|---|---|---|---|
| `string.handleize x` | string | URL-friendly slug | `'100% M & Ms!' \| string.handleize` → `100-m-ms` |
| `string.literal x` | string | Return as a quoted literal | `"Hi 'yo'" \| string.literal` → `"Hi 'yo'"` |
| `string.escape x` | string | Show escapes literally | `'a\tb' \| string.escape` → `a\tb` |

:::example
```scriban
{{ 'Hello, World! 123' | string.handleize }}
```
```text
hello-world-123
```
:::

## Encoding

Reach for `string.base64_encode` when you need to embed binary
content (images, small files) in text output, or when an API or
email header expects base64 instead of raw bytes. `base64_decode` is
the inverse for reading encoded input back.

`string.base64_encode` and `string.base64_decode` both return **string**.
Round-tripping:

:::example
```scriban
{{ 'hello' | string.base64_encode }} / {{ 'aGVsbG8=' | string.base64_decode }}
```
```text
aGVsbG8= / hello
```
:::

## Hashing

Reach for `string.sha256` when you need a deterministic fingerprint
of a string — cache keys, ETag headers, content-addressed filenames.
Reach for `string.hmac_sha256` when the output must be verifiable but
tamper-resistant (webhook signatures, signed tokens). The MD5 and
SHA-1 variants are legacy — fast non-cryptographic checksums only.

Seven hashing helpers — four digests and three keyed (HMAC) variants —
useful for cache keys and integrity checks. The
HMAC variants take a secret key. **All return string** (hex-encoded
digest).

| Function | Returns | Output |
|---|---|---|
| `string.md5 x` | string | MD5 (32 hex chars) — legacy only |
| `string.sha1 x` | string | SHA-1 (40 hex chars) — also legacy |
| `string.sha256 x` | string | SHA-256 (64 hex chars) — current default |
| `string.sha512 x` | string | SHA-512 (128 hex chars) |
| `string.hmac_sha1 x secret` | string | Keyed SHA-1 (40 hex chars) |
| `string.hmac_sha256 x secret` | string | Keyed SHA-256 (64 hex chars) |
| `string.hmac_sha512 x secret` | string | Keyed SHA-512 (128 hex chars) |

Don't use MD5 or SHA-1 for security-sensitive comparisons — collision
attacks make them unsuitable for password hashing or message
authentication. Both still work as fast non-cryptographic checksums.

:::example
```scriban
{{ ('hello' | string.sha256).size }}
```
```text
64
```
:::
