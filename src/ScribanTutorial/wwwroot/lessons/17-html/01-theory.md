The `html.*` module is the smallest built-in — five focused functions
for HTML escaping, tag stripping, and URL encoding. Use them whenever
your template emits HTML and the substituted values came from outside
your control (data files, user input, third-party APIs).

Upstream reference:
[scriban.github.io/docs/builtins/html](https://scriban.github.io/docs/builtins/html/).

**Return types.** All five functions return a new **string**; the
input is never mutated.

## Why HTML escaping matters

When a template substitutes a value into HTML markup, any `<`, `>`, `&`,
`"`, or `'` in the value collides with the surrounding syntax. Worse,
an unescaped `<script>` from data renders as a real script tag and
executes in the user's browser. `html.escape` is the canonical defence:

:::example
```scriban
{{ '<p>Hello & welcome!</p>' | html.escape }}
```
```text
&lt;p&gt;Hello &amp; welcome!&lt;/p&gt;
```
:::

`html.escape` replaces `<`, `>`, `&`, `"`, and `'` with their HTML
entities. The browser displays the literal characters; the engine never
interprets them as markup. **Use it for every substitution into HTML
that didn't come from a trusted, sanitised source.**

(This tutorial's own theory pipeline runs the result through an HTML
sanitiser as defence-in-depth — see the
[About page](/scriban-tutorial/about). But you should still apply
`html.escape` in your own templates that emit HTML.)

## Strip tags

The inverse of escaping — remove markup from a string, keeping only the
text content. Useful when you have HTML data but need a plain-text
preview:

:::example
```scriban
{{ '<p>Hello <b>world</b>!</p>' | html.strip }}
```
```text
Hello world!
```
:::

`html.strip` is regex-based and not a full HTML parser — don't rely on
it as a security boundary. Use it for "give me a plain-text version"
needs; sanitise hostile input separately.

## Newlines → `<br />`

Convert `\n` to `<br />` for displaying multi-line text inside an HTML
paragraph that doesn't preserve whitespace:

:::example
```scriban
{{ 'line one\nline two\nline three' | html.newline_to_br }}
```
```text
line one<br />
line two<br />
line three
```
:::

(The original newlines stay too — `html.newline_to_br` INSERTS the
`<br />` before each `\n`, it doesn't replace.)

## URL encoding

Two URL-encoders for different parts of a URL:

| Function | Returns | Encodes |
|---|---|---|
| `html.url_encode x` | string | Percent-encodes characters not safe in a URL query/path component (more aggressive — used for individual parameter values) |
| `html.url_escape x` | string | Percent-encodes characters that aren't allowed at all in URLs (less aggressive — used to make an already-shaped URL safe) |

The difference is subtle and matters in edge cases. Use `url_encode`
when building a query-string value; use `url_escape` when you have a
mostly-formed URL with a few troublesome characters in the path.

:::example
```scriban
{{ 'ada lovelace@example.com' | html.url_encode }}
{{ '/path/with spaces/<file>' | html.url_escape }}
```
```text
ada%20lovelace%40example.com
/path/with%20spaces/%3Cfile%3E
```
:::

Both encoders use `%20` for spaces in this Scriban runtime. The
practical difference is which characters each function considers
"unsafe": `html.url_encode` percent-encodes more aggressively (treating
`&`, `=`, `?` as data that needs escaping — appropriate for an
individual query-string VALUE), while `html.url_escape` is closer to a
direct `Uri.EscapeUriString` (leaving structural URL characters alone
— appropriate for an already-shaped URL).

## Putting them together

A common idiom: take untrusted user content, strip any HTML the user
might have tried to inject, escape what's left for safe HTML output:

```scriban
<p>{{ raw_input | html.strip | html.escape }}</p>
```

`html.strip` removes whole tags first (so users can't sneak markup
through). `html.escape` then turns any remaining special characters
(`<`, `>`, `&`, `"`, `'`) into entities. Defence in depth — pair both
when handling input you don't trust.
