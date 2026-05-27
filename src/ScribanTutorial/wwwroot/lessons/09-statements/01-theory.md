**Statements** are the verbs of the language: assignment, conditionals,
loops, and a few control words. Each one ends at a `}}`, a newline inside
a code block, or a `;`.

## Compound assignment

`x += 5` is `x = x + 5`. The same shortcut works for `-=`, `*=`, `/=`,
`//=`, `%=`.

:::example
```scriban
{{ x = 10
   x += 5
   x }}
```
```text
15
```
:::

## `if` / `else if` / `else`

```scriban
{{ if score >= 90 }}
A
{{ else if score >= 80 }}
B
{{ else }}
C or below
{{ end }}
```

Truthiness in Scriban: `null` and `false` are false; everything else is
true — including `0`, `""`, and `[]`. Use `array.size x > 0` when you
mean "has items".

## `case` / `when` / `else`

The switch equivalent. Each `when` accepts one or more values (separated
by `,` or `||`). A final `else` catches the rest.

:::example
```scriban
{{ x = 5
   case x
     when 1, 2, 3
       "Value is 1, 2 or 3"
     when 5
       "Value is 5"
     else
       "Value is " + x
   end }}
```
```text
Value is 5
```
:::

## `for ... in ...`

Iterate over an array or a range. The `for` object inside the loop
exposes useful state: `for.index` (0-based), `for.first`, `for.last`,
`for.length`, `for.changed`, `for.even`, `for.odd`.

The loop accepts named parameters: `offset:N`, `limit:N`, and `reversed`.

:::example
```scriban
{{ for $i in (4..9) limit:2 }}{{ $i }}
{{ end }}
```
```text
4
5
```
:::

## `while`

Repeats while a condition stays truthy. `while.index`, `while.first`,
`while.even`, `while.odd` are available inside.

```scriban
{{ $i = 0
   while $i < 3
     $i = $i + 1
     "tick"
   end }}
```

## `break` and `continue`

Exit the surrounding loop early (`break`) or skip to the next iteration
(`continue`).

## `capture`

Capture the rendered output of a block into a variable instead of
emitting it.

:::example
```scriban
{{- capture greeting ~}}
Hello, world.
{{~ end ~}}
{{ greeting | string.upcase }}
```
```text
HELLO, WORLD.

```
:::

## `with`

`with obj` makes assignments inside the block write to `obj`'s members.
A cleaner alternative to `obj.x = ...; obj.y = ...` when you have several
fields to set.

```scriban
{{ box = {}
   with box
     width = 10
     height = 4
   end
   box.width * box.height }}
```

renders `40`.

## `import`

Drops every member of an object into the current scope as a plain
variable — convenient when you keep your settings as one object.

```scriban
{{ settings = { greeting: "Hello", subject: "world" }
   import settings
   greeting + ", " + subject }}
```

renders `Hello, world`.

## `ret`

Early-exit from the current function or include page. The remainder of
the template doesn't run.

## `include` (mention)

`include "name.scriban"` evaluates another template at this position.
This tutorial doesn't configure a template loader, so includes aren't
available here — but you'll meet them in any real Scriban host.
