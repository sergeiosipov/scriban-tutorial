**Statements** are the verbs of the language: assignment, conditionals,
loops, and a few control words. Each one ends at a `}}`, a newline
inside a code block, or a `;`.

This lesson is organised into four bands:

1. **Assignments and single-expression blocks** — store values; emit them.
2. **Flow control** — `if` / `else if` / `else` and `case` / `when`.
3. **Loops** — `for`, `while`, `tablerow`, plus `break` and `continue`.
4. **Other constructs** — `capture`, `readonly`, `import`, `with`,
   `wrap`, `ret`, `include`.

---

# 1. Assignments and single-expression blocks

## Single-expression block

The simplest statement: a bare expression on its own line. The result is
written to the output. This is how the previous lessons rendered values
without spelling out an explicit assignment:

:::example
```scriban
{{ 'hello' }}
```
```text
hello
```
:::

Multiple expression statements run top to bottom; each one writes
whatever it evaluates to.

## Assignment and compound assignment

`x = value` writes; the family of compound forms combine a read and a
write:

| Form | Equivalent to |
|---|---|
| `x += y` | `x = x + y` |
| `x -= y` | `x = x - y` |
| `x *= y` | `x = x * y` |
| `x /= y` | `x = x / y` (always promotes to float) |
| `x //= y` | `x = x // y` (integer division) |
| `x %= y` | `x = x % y` |

:::example
```scriban
{{ counters = { hits: 0, misses: 0 }
   counters.hits += 5
   counters.misses -= 1
   counters.hits *= 2
   counters.misses %= 3
   counters.hits }} hits, {{ counters.misses }} misses
```
```text
10 hits, -1 misses
```
:::

Two rules to remember:

- **If left or right is a float and the other is an integer, the result
  of the operation is a float.** Once a variable picks up a `.5`, future
  reads return a `double`.
- **The left-hand side of an assignment must be a variable, property, or
  indexer.** `(a + b) = 5` is a parse error.

:::example
```scriban
{{ count = 5
   count += 0.5
   count }}
```
```text
5.5
```
:::

---

# 2. Flow control

## `if` / `else if` / `else`

Branch on a condition. `else if` chains, `else` catches the rest.

:::example
```scriban
{{ if score >= 90
     'A'
   else if score >= 80
     'B'
   else if score >= 70
     'C'
   else
     'D or below'
   end }}
```
```json
{ "score": 85 }
```
```text
B
```
:::

### Truthiness

**`null` and `false` are falsy. Everything else is truthy — including
`0`, `""`, and `[]`.** That last bit catches people coming from
JavaScript or Python:

:::example
```scriban
{{ if 0 }}truthy{{ end }} / {{ if '' }}truthy{{ end }} / {{ if [] }}truthy{{ end }}
```
```text
truthy / truthy / truthy
```
:::

If you mean "has items", write the test explicitly: `array.size items > 0`,
`x != null && x != ""`, etc.

### Combining conditions

`&&` and `||` work inside `if`, and parentheses group sub-conditions for
clarity or precedence:

:::example
```scriban
{{ if (a > 0 && b > 0) || force
     'both'
   else
     'no'
   end }}
```
```json
{ "a": 1, "b": 2, "force": false }
```
```text
both
```
:::

## `case` / `when` / `else`

The switch equivalent. Each `when` takes one or more values separated by
commas (or `||`); a final `else` catches the rest. The match is by
equality:

:::example
```scriban
{{ x = 5
   case x
     when 1, 2, 3
       'low'
     when 4, 5, 6
       'mid'
     when 7, 8, 9
       'high'
     else
       'out of range'
   end }}
```
```text
mid
```
:::

Each `when` arm is one branch; the engine takes the first match and skips
the rest. You can match against any expression — strings, numbers,
booleans — as long as the cases compare with `==`.

---

# 3. Loops

## `for ... in ...`

Iterate over an array, range, or any iterable. The body runs once per
element, with the loop variable bound to the current item.

:::example
```scriban
{{ for fruit in ['red', 'green', 'blue'] ~}}
{{ for.index }}: {{ fruit }}{{ if !for.last }}, {{ end }}
{{- end }}
```
```text
0: red, 1: green, 2: blue
```
:::

### Loop state: the `for.*` object

Inside the body, the `for` object exposes useful metadata:

| Field | Meaning |
|---|---|
| `for.index` | 0-based iteration counter |
| `for.rindex` | reverse counter — counts down to 0 on the last iteration |
| `for.first` / `for.last` | `true` on first / last iteration |
| `for.length` | total iterations |
| `for.changed` | `true` when the current item differs from the previous |
| `for.even` / `for.odd` | parity of `for.index` |

### `for ... else`

A `for` block can carry an `else` branch, which runs only when the
iterable is empty — the loop-flavoured cousin of `if`'s `else`. It saves
a separate `if products.size > 0` wrapper around the loop:

:::example
```scriban
{{ for product in products ~}}
- {{ product.name }}
{{ else ~}}
No products found.
{{ end }}
```
```json
{ "products": [] }
```
```text
No products found.
```
:::

With a non-empty array the body runs as usual and the `else` branch is
skipped entirely.

### Named parameters: `offset`, `limit`, `reversed`

Three filters can ride on the `for` header:

| Parameter | Effect |
|---|---|
| `offset:N` | Skip the first N items |
| `limit:N` | Stop after N items |
| `reversed` | Iterate back-to-front |

:::example
```scriban
{{ for n in [10, 20, 30, 40, 50] offset:1 limit:3 }}{{ n }} {{ end }}
```
```text
20 30 40 
```
:::

### Stepping through what gets emitted

A `for` body is a normal template fragment, so it can contain text,
expressions, and nested blocks. Trace through what each iteration emits.

For the `["red", "green", "blue"]` example above:

| Iteration | `for.index` | `for.last` | Body emits |
|---|---|---|---|
| 1st | 0 | false | `0: red, ` |
| 2nd | 1 | false | `1: green, ` |
| 3rd | 2 | true | `2: blue` (no `, ` — the `if` skipped) |

Concatenated, that's `0: red, 1: green, 2: blue` — the output above.

## `while`

Repeat while a condition stays truthy. `while.index`, `while.first`,
`while.even`, `while.odd` are available inside.

:::example
```scriban
{{ $i = 0
   while $i < 3
     if !while.first; ', '; end
     'tick'
     while.index
     $i = $i + 1
   end }}
```
```text
tick0, tick1, tick2
```
:::

Step-through: same shape as `for`. Each iteration runs the body with the
current `while.*` state, then re-tests the condition.

### Loop foot-gun: forgetting to mutate

`while`'s condition must change inside the body, or you get an infinite
loop. This app caps Scriban at 100,000 iterations so a stuck template
errors instead of hanging the browser, but the real safeguard is your
own discipline.

## `tablerow`

A loop that emits an HTML `<table>`-row layout — handy when generating
grid-style markup. The named `cols:N` parameter wraps every N items
into a new row.

:::example
```scriban
{{- tablerow item in [1, 2, 3, 4, 5, 6] cols:3 -}}
{{ item }}
{{- end -}}
```
```text
<tr class="row1"><td class="col1">1</td><td class="col2">2</td><td class="col3">3</td></tr>
<tr class="row2"><td class="col1">4</td><td class="col2">5</td><td class="col3">6</td></tr>
```
:::

Each item gets wrapped in a `<td class="colN">`; every `cols` items get
grouped into a `<tr class="rowM">`. The `cols` parameter is what makes
`tablerow` more than just a slightly differently-rendered `for`.

## `break` and `continue`

Exit the surrounding loop early (`break`) or skip to the next iteration
(`continue`). Both work inside any loop kind.

:::example
```scriban
{{- for n in [1, 2, 3, 4, 5, 6, 7] -}}
{{ if n > 5; break; end -}}
{{ if n % 2 == 0; continue; end -}}
{{ n }} {{ end }}
```
```text
1 3 5 
```
:::

`continue` skipped the even VALUES (2, 4); `break` exited once `n > 5`.
Output: just the odd values up to 5.

---

# 4. Other constructs

## `capture`

Render a block into a variable instead of emitting it. Handy when you
need to apply a filter to a chunk of mixed text-and-expressions:

:::example
```scriban
{{- capture greeting ~}}
Hello, {{ name }}.
{{~ end -}}
{{ greeting | string.upcase }}
```
```json
{ "name": "Ada" }
```
```text
HELLO, ADA.
```
:::

The block between `capture greeting` and `end` ran normally, but its
output went into `greeting` instead of the page. Then `| string.upcase`
transformed it.

Use cases: building up a value for later re-use; pre-rendering a chunk
so a downstream filter (escape, encode, slugify) can clean it.

## `readonly`

Mark a variable as immutable. Subsequent assignments to it raise a
runtime error. Declare AFTER the initial assignment:

:::example
```scriban
{{ pi = 3.14159
   readonly pi
   pi }}
```
```text
3.14159
```
:::

Why use it? Two reasons:

1. **Catch mistakes.** If a variable should never change again, declaring
   it readonly turns a typo elsewhere into a loud error instead of a
   silent overwrite.
2. **Sandbox configuration.** When the host passes a settings object into
   the template, the template author can lock those fields against
   accidental mutation later in the pipeline.

(`readonly x = 5` is NOT valid syntax — declare on its own line, after
the initial assignment.)

## `import`

Drop every member of an object into the current scope as a plain
variable. Convenient when you keep settings or context as one object:

:::example
```scriban
{{ settings = { greeting: 'Hello', subject: 'world' }
   import settings
   greeting + ', ' + subject }}
```
```text
Hello, world
```
:::

After `import settings`, both `settings.greeting` and the bare
`greeting` work — `import` is a copy, not a redirect.

## `with`

`with obj` scopes assignments inside the block to `obj`'s members. A
cleaner alternative to `obj.x = ...; obj.y = ...` when several fields
need setting in one place:

:::example
```scriban
{{ box = {}
   with box
     this.width = 10
     this.height = 4
   end
   box.width * box.height }}
```
```text
40
```
:::

Inside the block, plain identifiers READ from `box`, and `this.X = ...`
WRITES to it. The block is closed by `end`.

`import` and `with` are the two ways to bring an object's fields into
scope. The difference: `import` is a one-shot copy; `with` is a
positioned block that also lets you write back.

## `wrap`

A custom block construct. Define a function whose body uses the special
local `$$` (the "block delegate"); then `wrap <fn> args ... body ... end`
calls the function with `$$` bound to the rendered body:

:::example
```scriban
{{ func box(tag) }}<{{ tag }}>{{ $$ }}</{{ tag }}>{{ end -}}
{{ wrap box 'div' -}}
hello, world
{{- end }}
```
```text
<div>hello, world</div>
```
:::

`wrap box "div"` invoked `box("div")`; inside the function, `$$` was
substituted with the rendered body `"hello, world"`. Use cases:

- Reusable HTML wrappers (`<div class="card">…</div>`, `<a href="…">…</a>`).
- Layout templates where the outer chrome is in one place and the
  per-call content is in another.
- Reducing repetition of multi-line decoration patterns (banners,
  callout boxes, JSON envelopes).

## `ret`

Early-exit from the current function or include page. The remainder of
the template doesn't run:

:::example
```scriban
{{ func first_word
     for word in string.split($0, ' ')
       ret word
     end
   end
   first_word 'hello world from Scriban' }}
```
```text
hello
```
:::

`ret word` returned the first iteration's `word` immediately — no
further iterations, no fallthrough. Use cases: short-circuit search,
fail-fast validation, returning a default early.

## `include` (and `include_join`)

`include "name.scriban"` evaluates another template file at this
position and emits its output. `include_join names sep` includes each
of `names` and joins the results with `sep`.

```scriban
{{ include 'header.scriban' }}
{{ include 'body.scriban'   }}
{{ include 'footer.scriban' }}
```

This composition pattern is the standard way real Scriban hosts
assemble bigger pages from smaller partials.

**Not available in this app.** Includes need a `TemplateLoader` on the
C# side — a callback that reads a name and returns the matching template
text. This browser-only tutorial doesn't ship a filesystem or HTTP
loader, so the runtime raises *"Unable to include … No TemplateLoader
registered"* when an include is reached. The pattern is unchanged in
your real host; you just won't be able to exercise it interactively
inside this course.
