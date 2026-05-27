An **array** is an ordered list of values, written with square brackets.
Indices start at zero.

## Creating arrays

Three equivalent ways to write an array literal.

### Empty

:::example
```scriban
{{ items = []
   items }}
```
```text
[]
```
:::

### Short (single line)

:::example
```scriban
{{ items = [1, 2, 3, 4]
   items }}
```
```text
[1, 2, 3, 4]
```
:::

### Over multiple lines

Long literals read more clearly stacked vertically. Trailing commas are
allowed — handy when reordering items in a code review:

:::example
```scriban
{{ fruits = [
     "Orange",
     "Banana",
     "Apple",
   ]
   fruits[0] }} - {{ fruits[1] }} - {{ fruits[2] }}
```
```text
Orange - Banana - Apple
```
:::

## Indexing

`array[i]` returns the i-th element. Indices are **zero-based** — the
first element is `array[0]`, not `array[1]`:

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

:::example
```scriban
{{ list = []
   list[0] = "a"
   list[1] = "b"
   list }}
```
```text
["a", "b"]
```
:::

Length, slicing, mapping, filtering, sorting, and the rest of the array
toolkit live under the `array.*` built-in module — see
[lesson 16](/scriban-tutorial/lesson/16-array). The lessons up to that
point only need indexing and append-by-index.

## Arrays as objects: arrays with properties

A Scriban array can also carry attached named properties — it's a list
*and* an object at the same time. Occasionally useful when you want to
thread metadata (a label, an id, a context) alongside the elements:

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

The properties live in a separate namespace from the indices, so
`a.label = "x"` doesn't shift the elements. The most common real use is
attaching an id (`results.query`, `rows.source_file`, etc.) so downstream
code can correlate a list back to where it came from.

## Whitespace gotcha around `[`

A space between a name and `[` flips the meaning from "indexer" to
"function call with an array argument":

| Source | Meaning |
|---|---|
| `myvar[1]` | indexer — fetch element 1 of `myvar` |
| `myfunc [1]` | function call — pass the array `[1]` to `myfunc` |

The difference matters because the engine can't tell from the name alone
whether `myvar` is an array or a function — it relies on the space as
the disambiguator.

:::example
```scriban
{{ items = [10, 20, 30]
   func first; ret $0; end
   items[1] }} vs. {{ first [10, 20, 30] }}
```
```text
20 vs. [10, 20, 30]
```
:::

`items[1]` (no space) fetched element 1 from the array, giving `20`.
`first [10, 20, 30]` (space) called the `first` function with the array
literal as its only argument, which returned the whole array. Functions
proper land in [lesson 7](/scriban-tutorial/lesson/07-functions); for
now, keep no space between a name and an indexing `[`.
