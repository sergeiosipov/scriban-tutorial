Inside a code block, Scriban supports two comment forms.

## Single-line `#`

Everything from `#` to the end of the line is ignored.

```scriban
{{ name   # this is a comment, name still prints }}
```

> Important: a single-line `#` comment runs to the *end of the line*, even
> past the closing `}}` on the same line. So `{{ x # done }} after }}` is
> the comment "eating" the closing tag and the trailing text up to the next
> newline. If you want a comment and then more content on the same line,
> use a multi-line comment instead.

## Multi-line `##`

Wrap a comment in matching `##` markers to span multiple lines. The
comment emits nothing.

:::example
```scriban
{{ ## This
is a multi
line
comment ## }}
```
```text

```
:::

Comments are stripped at parse time — they have no runtime cost and never
appear in the output.
