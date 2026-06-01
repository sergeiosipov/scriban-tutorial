A Scriban template is plain text with **blocks** punched into it. There are
three kinds, and every concept later in the course is built on top of them.

## Code blocks `{{ ... }}`

A code block holds one or more statements that the engine evaluates. The
result of an **expression statement** is written to the output; an
**assignment statement** writes nothing.

### Single-line vs multi-line code blocks

A code block is **single-line** when the whole expression fits between the
two delimiters with no internal newline:

:::example
```scriban
Hello {{ name }}, welcome.
```
```json
{ "name": "Ada" }
```
```text
Hello Ada, welcome.
```
:::

A **multi-line code block** spans several lines — convenient for short
programs that read more clearly stacked vertically:

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

### Statements separated by `;`

Inside a single block you can stack statements onto one line by separating
them with `;` — useful for compact one-liners, or to run several
side-effecting statements without spreading a multi-line block over your
markup. `;` is equivalent to a newline as a statement separator:

:::example
```scriban
{{ x = 5; x; x + 1 }}
```
```text
56
```
:::

## Text blocks

Everything outside `{{ ... }}` is a text block — copied to the output as-is.
A typical template is a sandwich: text frame on the outside, code blocks
substituting values inside (the `Hello {{ name }}, welcome.` example above).

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

### Nesting escape blocks

Sometimes the literal text you want to escape contains escape markers
itself — e.g. you're writing documentation about Scriban escaping. Scriban
supports any number of `%` symbols on the delimiters. `{%%{ ... }%%}` lets
the inner content contain `{%{ ... }%}`; `{%%%{ ... }%%%}` lets it contain
`{%%{ ... }%%}`; and so on. The rule: each level needs ONE MORE `%` than
the deepest level it contains.

:::example
```scriban
{%%{Wrapped {%{Hello {{ name }}}%}}%%}
```
```text
Wrapped {%{Hello {{ name }}}%}
```
:::

The outer `{%%{ ... }%%}` (double-percent) emits its body verbatim,
INCLUDING the inner `{%{ ... }%}` markers — neither gets evaluated.

## Whitespace control

By default the whitespace and newlines around `{{ ... }}` are preserved.
This is fine for inline expressions, but `for` and `if` blocks then leak
blank lines into the output. Two modes control trimming.

### Without any stripping

To make the problem concrete, here is the same loop rendered with no
whitespace control at all:

:::example
```scriban
<ul>
{{ for product in products }}
<li>{{ product.name }}</li>
{{ end }}
</ul>
```
```json
{ "products": [ { "name": "Orange" }, { "name": "Banana" } ] }
```
```text
<ul>

<li>Orange</li>

<li>Banana</li>

</ul>
```
:::

Every iteration picked up the newline that surrounds `{{ for }}` and
`{{ end }}`, so every `<li>` has a blank line above it. Worth seeing once;
the controls below exist to fix it.

### Greedy `-` mode

Strips **all** whitespace and newlines on the indicated side until the
first non-whitespace character.

| Form | Trims |
|---|---|
| `{{- expr }}` | left side |
| `{{ expr -}}` | right side |
| `{{- expr -}}` | both |

:::example
```scriban
Trailing whitespace        {{- 'stripped' -}}        and leading.
```
```text
Trailing whitespacestrippedand leading.
```
:::

The greedy mode walks left or right consuming spaces and newlines until it
hits a non-whitespace character. Use it when you want a control tag to
disappear with no trace.

### Non-greedy `~` mode

Strips one line's worth of whitespace on its side — typically the newline
the tag sits on, but never spills into the next line's leading indentation.
Useful when you want a `for` tag on its own line to disappear but the
indented body to keep its indent:

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

The same conservative trimming applies to `if` / `else` / `end`:

:::example
```scriban
Header
{{~ if visitor ~}}
    Welcome, {{ visitor }}.
{{~ else ~}}
    Welcome, friend.
{{~ end ~}}
Footer
```
```json
{ "visitor": "Ada" }
```
```text
Header
    Welcome, Ada.
Footer
```
:::

### Tracing the output line-by-line

Beginners often write whitespace control by trial and error. A faster
approach is to step through what each iteration (or branch) actually emits,
character by character.

**`for` walkthrough.** With the products example above, the template lays
out like this after the engine processes the `~` strippers:

| Step | What the engine emits |
|---|---|
| Static prefix | `<ul>\n` |
| Iter 1 body | `    <li>Orange</li>\n` |
| Iter 2 body | `    <li>Banana</li>\n` |
| Iter 3 body | `    <li>Apple</li>\n` |
| Static suffix | `</ul>` |

Concatenated, that is the `<ul>...</ul>` block you saw above. The `~`
strippers on `{{~ for ~}}` and `{{~ end ~}}` consumed the newline directly
beside each tag, so the loop header and footer don't leave a blank line.

**`if` walkthrough.** With the if/else example:

| Step | If `visitor` is truthy | If `visitor` is falsy |
|---|---|---|
| Static prefix | `Header\n` | `Header\n` |
| Body | `    Welcome, Ada.\n` | `    Welcome, friend.\n` |
| Static suffix | `Footer` | `Footer` |

Both branches keep the surrounding `Header` and `Footer` lines because the
`~` on the if/else/end tags only ate the newline adjacent to the tag itself.

### Static template whitespace vs dynamic output

A common puzzle: why doesn't `-` or `~` trim whitespace produced by an
expression like `{{ "\n" }}`?

**Static whitespace** is the literal spaces, tabs, and newlines you typed
into the template file, OUTSIDE the `{{ ... }}` tags. The `-` and `~`
markers are instructions to the *parser* to ignore this structural
whitespace before evaluation starts.

**Dynamic output** is the string content an expression produces when the
engine evaluates it. `{{ "\n" }}` evaluates to a newline string and the
engine writes that character into the output. The parser-level strippers
never see it — they ran before evaluation.

So in:

```scriban
Start{{'\n'}}{{-for x in 1..3-}}{{'\n'}}    <{{x}}>{{'\n'}}{{-end-}}{{'\n'}}End
```

The `-` characters scan adjacent **static** whitespace and find none —
every character beside them is part of another `{{ ... }}` tag, which is
code, not whitespace. The only static whitespace in that line is the four
spaces before `<{{x}}>`, which a stripper *could* eat (e.g. `{{ "\n" -}}    <...`).

The takeaway: whitespace control is structural, not an output filter.

## Auto-indentation

When a multi-line value is rendered at an indented position, Scriban
automatically prefixes every line of the value with the same indentation:

:::example
```scriban
Before
   {{ multi }}
After
```
```json
{ "multi": "L1\nL2\nL3" }
```
```text
Before
   L1
   L2
   L3
After
```
:::

The three-space indent before `{{ multi }}` propagates to L2 and L3 even
though the JSON value has none. Handy for generating indented YAML,
Markdown lists, or nested code blocks from a flat string. (The C# host can
disable this via `TemplateContext.AutoIndent = false`.)
