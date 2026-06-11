Where [lesson 09-statements](/scriban-tutorial/lesson/09-statements) covered
flow control (`if`, `case`, `for`, `while`, `break`, `continue`), this
lesson covers the **scope and composition** constructs that manage output
capture, controlled mutation, data-sharing, and template assembly.

This lesson covers six bands:

1. **`capture`** — render a block into a variable.
2. **`readonly`** — mark a variable immutable.
3. **`import`** — flatten an object's fields into the current scope.
4. **`with`** — scoped reads and writes against an object.
5. **`wrap`** — custom block delimiters with a content delegate.
6. **`ret` and `include`** — early exit and partial-template inclusion.

---

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
