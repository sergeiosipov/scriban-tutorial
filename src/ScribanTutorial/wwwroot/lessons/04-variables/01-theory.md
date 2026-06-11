A **variable** holds a value. Scriban has three storage classes,
distinguished by how their name is written and by where they live.

## Identifier rules

A variable name must:

- Start with a letter or `_`. Digits are allowed after the first
  character, but `2a` is a parse error and `a-b` is read as `a - b`
  (subtraction).
- Contain only letters, digits, and underscores. No dashes, dots, or
  spaces inside the name.

By convention, lowercase with underscores reads idiomatically in Scriban
(`user_name`, `is_admin`); the templating layer doesn't enforce a style,
but the .NET host's default member renamer turns C# `MyProperty` into
`my_property` when projecting a model, so matching that case keeps your
JSON, Scriban, and C# names visually aligned.

## Global variables `{{ name }}`

Plain identifiers reference the **global** scope. The host hands you a
data model (the JSON object), and every top-level key in that JSON
becomes a global variable in the template. Assignments to plain
identifiers also land in the global scope.

:::example
```scriban
Hello, {{ name }}.
```
```json
{ "name": "Ada" }
```
```text
Hello, Ada.
```
:::

A missing global resolves to `null`, which renders as the empty string —
no error, no warning. Misspelt variable names produce eerie blank output.
The catch patterns from lesson 3 (`?? "fallback"`, `if x == null`) are
your friends.

### Special global: `empty`

`empty` is a sentinel for "an empty object". Compare against it to test
emptiness, particularly when interoperating with Liquid-style templates:

:::example
```scriban
{{ a = {}
   b = [1, 2] ~}}
{{ a == empty }}
{{ b == empty }}
```
```text
true
false
```
:::

`empty` is a reserved name — don't try to use it as your own variable.

## Local variables `{{ $name }}`

A name prefixed with `$` is **local** — scoped to the surrounding
function body, or to the top-level template if there is no enclosing
function. Locals are useful inside functions for short-lived helpers
that shouldn't leak into the caller's globals.

:::example
```scriban
{{ $count = 1
   $count + $count }}
```
```text
2
```
:::

### Function arguments: `$`, `$0`, `$1`, `$.named`

Inside a **simple function** (`func f ... end` — covered in lesson 7),
the engine pre-populates several locals:

| Name | What it holds |
|---|---|
| `$` | The entire argument list as an array. Iterate it with `for arg in $`. |
| `$0`, `$1`, `$2`, … | The positional argument at the given index (0-based). |
| `$.name` | The named argument `name:value` (e.g. `f x:5` makes `$.x` equal `5`). |

:::example
```scriban
{{ func sum
     ret $0 + $1 + $2
   end
   sum 10 20 30 }}
```
```text
60
```
:::

:::example
```scriban
{{ func vec
     r = 0
     for x in $
       r = r + x
     end
     ret r
   end
   vec 1 2 3 4 5 }}
```
```text
15
```
:::

### Loop state: `for.*` and `while.*`

`for` and `while` expose iteration metadata (`for.index`, `for.last`,
`while.index`, etc.) — covered fully in the Loops section of
[lesson 9](/scriban-tutorial/lesson/09-statements).

## Scope: when assignments stay local vs leak global

This is the most surprising rule in Scriban — worth memorising:

| Block | Plain `x = …` writes to | `$x = …` writes to |
|---|---|---|
| Top-level template | global | template-local |
| Inside `func`/`do` body | **global** (foot-gun) | function-local |
| Inside `for` / `while` / `if` | enclosing scope (global if at top) | enclosing scope (template-local if at top) |

Two consequences worth pinning down.

**Functions are the only blocks that introduce a real new scope** — and
even then, only for `$`-prefixed names. A plain assignment inside a
`func` body still writes to the global scope:

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

If you want a function to keep its working state private, use `$`-prefixed
names inside it. Otherwise the caller sees your bookkeeping.

**`for` / `while` / `if` do NOT introduce a new scope.** A variable
assigned inside a loop is visible after the loop:

:::example
```scriban
{{ for i in 1..3
     last_seen = i
   end
   last_seen }}
```
```text
3
```
:::

This is convenient for accumulator patterns and confusing if you're
coming from a language where blocks always introduce scope. The takeaway:
in Scriban, scope follows `func` / `do`, not braces.

## The `this` variable

`this` refers to the current scope's bound object. At the top of a
template `this` IS the global scope, so `this.x = 5` and `x = 5` are the
same write:

:::example
```scriban
{{ a = 5
   this.a = 6
   a }}
```
```text
6
```
:::

`this` becomes much more interesting inside a `with` block (covered in
[lesson 9](/scriban-tutorial/lesson/09-statements)), where it points at
the wrapped object — letting you read and write that object's members by
name as if they were locals:

:::example
```scriban
{{ user = { name: 'Ada' }
   with user
     this.role = 'admin'
   end
   user.name }} is an {{ user.role }}
```
```text
Ada is an admin
```
:::

Inside the `with` block, `this.role = "admin"` set the wrapped `user`
object's `role`, so reading `user.role` after the block returns the new
value.

### Member access by `.` vs `[…]`

Whether the name is plain, `$`-prefixed, or accessed through `this`,
members can be reached two ways:

- **Dotted** — `user.name`. Compile-time name; can't be a variable.
- **Indexed** — `user["name"]`. Runtime string; `user[key]` lets the field
  name come from another variable. Use this when the key is computed.

:::example
```scriban
{{ user = { first: 'Ada', last: 'Lovelace' }
   key = 'last'
   user.first }} {{ user[key] }}
```
```text
Ada Lovelace
```
:::
