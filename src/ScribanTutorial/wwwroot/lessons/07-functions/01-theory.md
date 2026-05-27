Functions let you name a chunk of template logic and call it by name.
Scriban has four flavours.

## Calling functions

Before defining any, the call syntax. A function can be called three ways:

| Form | Reads as |
|---|---|
| `f a b` | Space-separated arguments. Common in templates and pipe chains. |
| `f(a, b)` | Parentheses with commas. Visually unambiguous; recommended whenever the call is part of a bigger expression or sits in code-heavy markup. |
| `a \| f b` | Pipe — `a` becomes the **first** argument of `f`; `b` is the second. |

:::example
```scriban
{{ func sub
     ret $0 - $1
   end
   sub 10 3 }}, {{ sub(10, 3) }}, {{ 10 | sub 3 }}
```
```text
7, 7, 7
```
:::

All three calls produce the same result. The parentheses form is the
cleanest when a call appears inside another expression: `sub(10, 3) * 2`
parses without ambiguity, while `sub 10 3 * 2` is read as
`sub 10 (3 * 2)`.

## Simple functions — `func name ... end`

A function block named `name`. Arguments arrive in special locals:

| Name | Holds |
|---|---|
| `$0`, `$1`, `$2`, … | The positional argument at the given index. |
| `$` | The entire argument list as an array (use with `for` for variadic patterns). |
| `$.name` | The named argument `name:value` from the caller. |

The body's last expression (or an explicit `ret`) is the result.

:::example
```scriban
{{ func sub
     ret $0 - $1
   end
   sub 5 1 }}
```
```text
4
```
:::

### Function bodies can contain text

`func ... end` is a block — its body is a normal template fragment, so
plain text and `{{ ... }}` expressions inside it both render to output:

:::example
```scriban
{{ func greet }}
Hello, {{ $0 }}! You will be {{ $1 + 1 }} next year.
{{- end -}}
{{ greet "Ada" 35 -}}
```
```text

Hello, Ada! You will be 36 next year.
```
:::

### Plain assignments inside a simple function leak to global

This is the most surprising rule in Scriban functions — covered already
in [lesson 4](/scriban-tutorial/lesson/04-variables) and worth repeating:

:::example
```scriban
{{ func bump
     count = count + 1
   end
   count = 0
   bump
   bump
   count }}
```
```text
2
```
:::

The function's `count = count + 1` wrote to the GLOBAL scope, so the
caller's `count` ended at 2. If you want a function to keep its
bookkeeping private, use `$`-prefixed locals (the "Parametric"
section below introduces a cleaner way for named parameters).

### Variadic: iterate `$`

The whole argument array sits in `$`, iterable with `for`:

:::example
```scriban
{{ func sum_all
     r = 0
     for x in $
       r = r + x
     end
     ret r
   end
   sum_all 1 2 3 4 5 }}
```
```text
15
```
:::

Named arguments come in alongside positional ones via `$.name`:

:::example
```scriban
{{ func join
     ret $0 + ($.sep ?? ", ") + $1
   end
   join "left" "right" sep:" | " }}
```
```text
left | right
```
:::

## Parametric functions — `func name(a, b) ... end`

The same block shape with **named** parameters. The engine checks the
call matches the signature — extra arguments are a parse-time error.
Parameters get defaults with `=`:

:::example
```scriban
{{ func sub(x, y, z = 1)
     ret x - y - z
   end
   sub 10 3 }}
```
```text
6
```
:::

A call that omits `z` uses the default `1`, so `sub 10 3 → 10 - 3 - 1 = 6`.

Named arguments let the caller override one default without naming the
others:

:::example
```scriban
{{ func make_url(host, scheme="https", port=443, path="/")
     ret scheme + "://" + host + ":" + port + path
   end
   make_url "example.com" port:8080 }}
```
```text
https://example.com:8080/
```
:::

`scheme` and `path` defaulted; `port` was named-overridden.

**Parametric functions have fixed arity** — they don't accept "rest"
arguments. If you need variable-length input, use the simple `func name`
form (no parens) and read `$` as shown above.

## Inline functions — `name(a, b) = expr`

A one-liner. The expression on the right is the body; the parameters
list is exactly the named ones:

:::example
```scriban
{{ add(a, b) = a + b
   add 7 5 }}
```
```text
12
```
:::

**Inline functions are restricted** — they don't support optional
parameters or variadic input. `f(a, b = 5) = a + b` is a parse error.
Reach for the full `func name(a, b = 5) ... end` form when you need
those.

## Anonymous functions — `do ... end`

A function literal you can assign to a name or pass as an argument.
Useful for higher-order patterns (passing a "block" to a wrapper):

:::example
```scriban
{{ sub = do; ret $0 - $1; end
   sub 9 4 }}
```
```text
5
```
:::

The body is a normal block — you can use `;` or newlines to separate
statements, including locals:

:::example
```scriban
{{ mult2 = do
     $x = 2
     ret $0 * $x
   end
   mult2 3 }}
```
```text
6
```
:::

`$x` is function-local; the caller's `$x` is untouched.

### `do` as a custom block argument

When a function expects a "block" parameter, you pass a `do` block
without the `=` — it becomes the trailing-block argument. Built-in
`array.each` works this way:

:::example
```scriban
{{ items = [1, 2, 3]
   items | array.each do
     ret $0 * 10
   end }}
```
```text
[10, 20, 30]
```
:::

The `do ... end` block was handed to `array.each` as the function it
runs on every element.

## Function pointers — `@name`

Prefix a function reference with `@` to mean "the function itself, don't
call it." You can store the reference, pass it around, and call it
later. Without `@`, a function name in expression position invokes the
function with no arguments (often producing an error).

### Alias a built-in

:::example
```scriban
{{ upper = @string.upcase
   "ada" | upper }}
```
```text
ADA
```
:::

### Alias a user-defined function

:::example
```scriban
{{ func sq
     ret $0 * $0
   end
   sqr = @sq
   sqr 5 }}, {{ 6 | sqr }}
```
```text
25, 36
```
:::

`sqr = @sq` captured the function. Calling `sqr 5` or piping into it
both invoke the underlying `sq`.

### Pass a function as an argument

:::example
```scriban
{{ func sq
     ret $0 * $0
   end
   func apply(f, v)
     ret @f v
   end
   apply @sq 7 }}
```
```text
49
```
:::

Inside `apply`, `f` is the function reference. Calling it still needs
the `@` — `@f v` invokes the captured function with `v` as its argument.
This is the official-docs example written correctly; the upstream
snippet for function pointers historically dropped the inner `@` and
won't run.

## Pipes in depth

Pipes chain left-to-right and read like the value flowing through a
sequence of transformations. Each `| f` step receives the previous
result as its first argument:

:::example
```scriban
{{ "  ada  " | string.strip | string.upcase | string.append "!" }}
```
```text
ADA!
```
:::

Trace: `"  ada  "` → strip → `"ada"` → upcase → `"ADA"` → append `"!"`
→ `"ADA!"`. The pipe form is the dominant convention in
Scriban templates for the same reason Unix shell pipes are: each step
is a transform with one input, and the order matches reading direction.

`a | f b c` is the same as `f a b c` — pipe slips `a` into the first
slot, not the last.
