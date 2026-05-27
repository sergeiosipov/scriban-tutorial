Inside a code block, Scriban supports two comment forms.

## Single-line `#`

Everything from `#` to the **next newline or the closing `}}`**, whichever
comes first, is ignored:

:::example
```scriban
{{ name   # this is a comment, name still prints }}
```
```json
{ "name": "Ada" }
```
```text
Ada
```
:::

The comment ends right before the closing `}}`, so the tag closes cleanly
and the next character outside the block resumes ordinary text. If you
want a `#` comment to extend past `}}` you cannot — that's what the
multi-line form below is for.

## Multi-line `##`

Wrap a comment in matching `##` markers to span multiple lines. The
comment emits nothing, and statements before and after it run normally:

:::example
```scriban
{{ "Inside start;"; ## This
is a multi
line
comment ##; "Inside end;" }}Outside
```
```text
Inside start;Inside end;Outside
```
:::

`"Inside start;"` and `"Inside end;"` are expression statements separated
by `;`, with the multi-line comment between them. The `## ... ##` block is
stripped at parse time and the two strings concatenate into the output.

### When `##` has no closing `##`

If you forget the closing `##`, Scriban does NOT raise a parse error.
Instead the comment runs to the closing `}}` of the surrounding code
block — consuming any statements that were supposed to live after it:

:::example
```scriban
{{ "Inside start;"; ## This
is a multi
line
comment; "Inside end;" }}Outside
```
```text
Inside start;Outside
```
:::

Compared with the previous example, `"Inside end;"` is missing from the
output — it sat between the unclosed `##` and the closing `}}`, so the
parser swallowed it as part of the comment. Treat this as a foot-gun: a
typo turns runnable code into a silent no-op. Always close your `##` if
you intend statements to follow.

Comments are stripped at parse time — they have no runtime cost and never
appear in the output.
