Scriban objects are JavaScript-style: a set of named members enclosed in
`{ ... }`. The data model the host hands the template is itself an
object — so member access is the first thing every real template does.

## Creating objects

Three equivalent ways to write an object literal.

### Empty

:::example
```scriban
{{ blank = {}
   blank }}
```
```text
{}
```
:::

(Don't name the variable `empty` — that's a reserved global covered in
[lesson 4](/scriban-tutorial/lesson/04-variables). Scriban won't stop you
from shadowing it, but you'll hide the sentinel from the rest of the
template.)

### Short form: unquoted keys

When the keys are valid identifiers, you can drop the quotes:

:::example
```scriban
{{ user = { first: 'Ada', last: 'Lovelace' }
   user.first }}
```
```text
Ada
```
:::

### JSON-quoted form

Convenient when you're pasting a literal block from a JSON document:

:::example
```scriban
{{ user = { 'first': 'Ada', 'last': 'Lovelace' }
   user.first }}
```
```text
Ada
```
:::

Both syntaxes are interchangeable. Mix them when one key is an identifier
and another isn't (e.g. it contains a dash or starts with a digit).

### Over multiple lines

Long literals read more clearly stacked vertically, one member per line:

:::example
```scriban
{{ person = {
     first: 'Ada',
     last: 'Lovelace',
     age: 36,
     fields: ['math', 'computing']
   }
   person.first }} {{ person.last }}, {{ person.age }}
```
```text
Ada Lovelace, 36
```
:::

## Member access

Two equivalent forms — dot notation and bracket notation. Use the
bracket form when the member name comes from a variable or isn't a valid
identifier:

:::example
```scriban
{{ user = { first_name: 'Ada', last_name: 'Lovelace' }
   user.first_name }} ({{ user['last_name'] }})
```
```text
Ada (Lovelace)
```
:::

The bracket form earns its keep when the key is computed at runtime:

:::example
```scriban
{{ user = { first_name: 'Ada', last_name: 'Lovelace' }
   key = 'last_name'
   user[key] }}
```
```text
Lovelace
```
:::

### Missing members resolve to `null`

A typo or an unset field doesn't raise — it produces `null`, which
renders as the empty string just like a missing global:

:::example
```scriban
{{ user = { name: 'Ada' }
   'name=' + user.name + ', email=' + (user.email ?? '[unset]') }}
```
```text
name=Ada, email=[unset]
```
:::

**Best practice for catching missing members:** when a field is
**required** for the output to make sense (e.g. an order line missing a
price would produce blank money amounts), guard explicitly with `if X ==
null` and fail loudly. When a field is genuinely **optional**, use the
`?? "fallback"` pattern as above so the output stays presentable.

## .NET-host naming convention

When the host hands you an object backed by a C# class, properties and
methods are exposed with **lowercase and underscore** names by default:
`MyPropertyName` becomes `my_property_name`, `IsAdmin` becomes
`is_admin`. The convention comes from Scriban's Liquid heritage; the C#
host can override it via a `MemberRenamer` delegate. Keep this in mind
when you're staring at a Razor-style model in C# and a `user.first_name`
in your template — they're the same field.

## Adding members

Pure Scriban objects (the ones the template itself creates) accept new
members via assignment. The same works on objects the host provides, as
long as they're plain Scriban `ScriptObject`s rather than read-only
adaptations of immutable .NET types:

:::example
```scriban
{{ box = {}
   box.size = 'medium'
   box.weight = 3
   'size=' + box.size + ', weight=' + box.weight }}
```
```text
size=medium, weight=3
```
:::

Inside a code block at the top level, `this` IS the global scope — so
`this.surname = "Lovelace"` and `surname = "Lovelace"` write to the same
slot:

:::example
```scriban
{{ this.surname = 'Lovelace'
   name + ' ' + surname }}
```
```json
{ "name": "Ada" }
```
```text
Ada Lovelace
```
:::

## The `?.` optional-chain and `??` fallback operators

`?.` skips the rest of the chain when a member is missing instead of
raising. `??` substitutes a default when the left side is `null`. Pair
them when the data shape is nested and unreliable:

:::example
```scriban
{{ user.address?.city ?? 'unknown' }}
```
```json
{ "user": { "first": "Ada" } }
```
```text
unknown
```
:::

`?.` reads as "if this member exists, continue; otherwise produce `null`
and short-circuit." Without it, `user.address.city` on the JSON above
would throw a runtime error (address is missing). With it, the chain
yields `null`, which `??` then replaces with `"unknown"`.

## Testing for emptiness

Two ways to ask "is this object empty?":

| Form | When to use |
|---|---|
| `obj.empty?` | Idiomatic, reads like English. |
| `obj == empty` | Useful with `!` for the negated form: `!(obj == empty)`. |

:::example
```scriban
{{ a = {}
   b = { x: 1 } ~}}
{{ a.empty? }} / {{ b.empty? }}
{{ a == empty }} / {{ !(b == empty) }}
```
```text
true / false
true / true
```
:::
