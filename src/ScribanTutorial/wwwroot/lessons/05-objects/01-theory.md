Scriban objects are JavaScript-style: a set of named members enclosed in
`{ ... }`. The data model the host hands the template is itself an
object — so member access is the first thing every real template does.

## Creating objects

```scriban
{{ empty   = {} }}
{{ short   = { first: "Ada", last: "Lovelace" } }}
{{ jsonish = { "first": "Ada", "last": "Lovelace" } }}
```

Both syntaxes are equivalent — the JSON-quoted form is just convenient
when you're translating from a JSON document.

## Member access

Two equivalent forms — dot notation and bracket notation. Use the
bracket form when the member name comes from a variable or isn't a valid
identifier.

```scriban
{{ user.first_name }}          # dot
{{ user["first_name"] }}       # equivalent
```

Missing members resolve to `null` — they render as the empty string,
exactly like missing globals.

## Adding members

Pure Scriban objects (the ones the template itself creates) accept new
members via simple assignment:

:::example
```scriban
{{ box = {}
   box.size = "medium"
   box.size }}
```
```text
medium
```
:::

## The `?.` optional-chain and `??` fallback operators

`?.` skips the rest of the chain when a member is missing instead of
raising. `??` substitutes a default when the left side is `null`.

:::example
```scriban
{{ user.address?.city ?? "unknown" }}
```
```json
{ "user": { "first": "Ada" } }
```
```text
unknown
```
:::

## The `.empty?` property

Every Scriban object answers `.empty?` with a boolean.

:::example
```scriban
{{ a = {}
   b = { x: 1 } ~}}
{{ a.empty? }}
{{ b.empty? }}
```
```text
true
false
```
:::
