A **variable** holds a value. Scriban has three kinds, distinguished by
where they live.

## Global variables `{{ name }}`

Plain identifiers reference the **global** scope. The host hands you a
data model (the JSON object), and every top-level key in that JSON is a
global variable in the template.

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

## Local variables `{{ $name }}`

A name prefixed with `$` is **local** — scoped to the current `include`,
function body, or the top-level template only. Locals are useful for
loop counters and short-lived helpers that shouldn't pollute the global
scope.

```scriban
{{ $i = 0 }}
{{ for $x in items }}
  {{- $i = $i + 1 -}}
{{ end }}
```

## The `this` variable

`this` refers to the current scope's bound object. Assigning `this.a = 5`
is equivalent to `a = 5`, and accessing `this.a` is equivalent to `a`.

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

`this` is more useful inside a `with` block (lesson 09), where it lets you
read and write the wrapped object's members.

## The `empty` variable

`empty` is a sentinel for "an empty object". Compare against it to test
emptiness, particularly when interoperating with Liquid-style templates.

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
