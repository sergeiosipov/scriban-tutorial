A Scriban template is plain text with **blocks** punched into it. There are
three kinds, and every concept later in the course is built on top of them.

## Code blocks `{{ ... }}`

A code block holds one or more statements that the engine evaluates. The
result of an expression statement is written to the output; an assignment
statement writes nothing.

:::example
```scriban
{{ x = 5     # assignment — no output
   x         # expression — writes 5
   x + 1     # expression — writes 6
}}
```
```text
56
```
:::

There is no newline between `5` and `6` because both expressions ran inside
the same code block. To get separate lines, put each expression in its own
block — the text between blocks is preserved verbatim.

:::example
```scriban
{{ x = 5 -}}
{{ x }}
{{ x + 1 }}
```
```text
5
6
```
:::

## Text blocks

Everything outside `{{ ... }}` is a text block — copied to the output as-is.
A typical template is a sandwich: text frame on the outside, code blocks
substituting values inside.

```scriban
Hello {{ name }}, welcome.
```

## Escape blocks `{%{ ... }%}`

When you need to render a literal `{{ ... }}` (e.g. when the output IS a
Scriban template), wrap it in an escape block. The contents are emitted
verbatim — code blocks inside are NOT evaluated.

:::example
```scriban
{%{Hello {{ name }}}%}
```
```text
Hello {{ name }}
```
:::

## Whitespace control

By default the whitespace and newlines around `{{ ... }}` are preserved.
This is fine for inline expressions, but `for` and `if` blocks then leak
blank lines into the output. Two modes control trimming:

**Greedy `-`** — strips *all* whitespace and newlines on the indicated side.

| Form | Trims |
|---|---|
| `{{- expr }}` | left side |
| `{{ expr -}}` | right side |
| `{{- expr -}}` | both |

**Non-greedy `~`** — strips whitespace up to (and including) one newline
only. Useful when you want a `for` tag on its own line to disappear from
the output but the indented body to keep its indentation.

:::example
```scriban
<ul>
    {{~ for product in products ~}}
    <li>{{ product.name }}</li>
    {{~ end ~}}
</ul>
```
```json
{ "products": [ { "name": "Orange" }, { "name": "Banana" }, { "name": "Apple" } ] }
```
```text
<ul>
    <li>Orange</li>
    <li>Banana</li>
    <li>Apple</li>
</ul>
```
:::

If you forget to trim block tags, the output picks up an extra blank line per
iteration — the most common "why does my list have gaps" bug.
