An **array** is an ordered list of values, written with square brackets.
Indices start at zero.

## Creating arrays

```scriban
{{ empty   = [] }}
{{ short   = [1, 2, 3, 4] }}
{{ multi   = [
                "Orange",
                "Banana",
                "Apple",
              ] }}
```

## Indexing

`array[i]` returns the i-th element, zero-based.

:::example
```scriban
{{ items = ["red", "green", "blue"]
   items[0] }} - {{ items[2] }}
```
```text
red - blue
```
:::

## Appending by index

Like objects, pure Scriban arrays grow on assignment to a previously-
unused index:

```scriban
{{ list = []
   list[0] = "a"
   list[1] = "b" }}
```

## The `.size` property

Every array exposes `.size`:

:::example
```scriban
{{ items = [1, 2, 3]
   items.size }}
```
```text
3
```
:::

## Arrays as objects

A Scriban array can also carry attached named properties — it's a list
*and* an object at the same time. This is occasionally useful when you
want to thread metadata alongside the elements.

:::example
```scriban
{{ a = [5, 6, 7]
   a.label = "x"
   a.label }}-{{ a[0] }}
```
```text
x-5
```
:::

## Whitespace gotcha around `[`

A space between a name and `[` flips the meaning from "indexer" to
"function call with an array argument":

| Source | Meaning |
|---|---|
| `myvar[1]` | indexer — fetch element 1 of `myvar` |
| `myfunc [1]` | function call — pass the array `[1]` to `myfunc` |

The lesson 09 (Functions) covers function calls; for now, keep no space
between a name and an indexing `[`.
