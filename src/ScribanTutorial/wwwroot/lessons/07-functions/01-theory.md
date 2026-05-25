Functions let you name a chunk of template logic and call it by name.
Scriban has four flavours; the first two cover almost everything.

## Simple functions — `func name ... end`

A function block named `name`. Arguments arrive in the special variable
`$`, indexed `$0`, `$1`, … The body's last expression (or an explicit
`ret`) is the result.

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

Inside `sub`, `$0` is `5` and `$1` is `1`. Pipes work too: `5 | sub 1`
passes `5` as the first argument and `1` as the second.

## Parametric functions — `func name(a, b) ... end`

The same shape with **named** parameters; the engine checks the call
matches the signature. Optional parameters get defaults with `=`.

:::example
```scriban
{{ func sub(x, y, z = 1)
     ret x - y - z
   end
   sub 10 3
}}
```
```text
6
```
:::

A call that omits `z` uses the default `1`, so `sub 10 3 → 10 - 3 - 1 = 6`.

## Inline functions — `name(x, y) = expr`

A one-liner. The expression on the right is the body; the parameters
list is exactly the named ones.

:::example
```scriban
{{ add(a, b) = a + b
   add 7 5 }}
```
```text
12
```
:::

## Anonymous functions — `do ... end`

A function literal you can pass as an argument or assign to a name.
Useful for higher-order patterns (passing a "block" to a wrapper
function).

:::example
```scriban
{{ sub = do; ret $0 - $1; end
   sub 9 4 }}
```
```text
5
```
:::

## Pipe semantics

A pipe (`a | f`) passes `a` as the **first** argument to `f`. So
`5 | sub 1` is identical to `sub 5 1`, not `sub 1 5`. Pipes chain left to
right and read naturally:

```scriban
{{ "Ada" | string.upcase | string.append "!" }}
```

renders `ADA!`.
