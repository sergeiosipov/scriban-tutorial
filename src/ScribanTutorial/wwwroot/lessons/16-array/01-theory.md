The `array.*` module is the everyday toolkit for sequences — sort,
filter, map, slice, join, deduplicate. 21 functions in total, most
pipe-friendly.

Upstream reference:
[scriban.github.io/docs/builtins/array](https://scriban.github.io/docs/builtins/array/).

**Return types.** Every function in this module returns a new value;
the input list is never mutated. Most return **array**; a handful
return scalar values — see the `Returns` column on each table.

For true in-place mutation of an array, use index-assignment
(`a[i] = v` from lesson 6) — that's the only form that writes into the
existing list.

**Daily drivers vs look-up material.** The functions you'll use in
almost every real template: `array.sort`, `array.filter`, `array.each`,
`array.map`, `array.join`, `array.size`, `array.first`, `array.last`,
and `array.uniq`. `array.compact` (drop nulls) and `array.limit` (take
first N) earn their place once you're dealing with real data. The
building functions (`array.add`, `array.concat`, `array.insert_at`)
are useful in one-off contexts but O(N²) in loops — prefer
index-assignment there (the warning in the *Building* section below).
`array.cycle` and `array.any` are reference items: look them up when
alternating row classes or existence-checking is exactly what you need.

## Building

| Function | Returns | Effect |
|---|---|---|
| `array.add list v` | array | NEW list with `v` appended |
| `array.add_range a b` | array | Concatenate two lists into a NEW list; same as `array.concat` |
| `array.concat a b` | array | Concatenate two lists into a NEW list |
| `array.insert_at list i v` | array | Insert `v` at index `i`, returning a NEW list |

:::example
```scriban
{{ [1, 2, 3] | array.add 4 }}
{{ [1, 2] | array.concat [3, 4] }}
{{ ['a', 'b', 'c'] | array.insert_at 1 'X' }}
```
```text
[1, 2, 3, 4]
[1, 2, 3, 4]
["a", "X", "b", "c"]
```
:::

### Don't use these in a tight loop

**All four functions above return a NEW array.** The original is left
untouched. That's harmless for one-off building, but it's a
memory-and-time foot-gun inside a loop:

```scriban
{{- a = []
   for n in 1..10000
     a = a | array.add n        # ← creates a NEW 1-, 2-, 3-, ... N-element array per iter
   end -}}
```

By iteration 10,000 you've allocated 10,000 arrays — total `O(N²)`
copy work, with the older arrays only kept alive briefly before garbage
collection. Slow and memory-thrashy.

**For true in-place append, use index-assignment** (lesson 6's
`a[i] = v` form). It mutates the existing array — no copy, no
short-lived garbage:

:::example
```scriban
{{- a = []
   for n in 1..5
     a[a.size] = n
   end -}}
{{ a }}
```
```text
[1, 2, 3, 4, 5]
```
:::

`a[a.size] = n` writes to the slot one past the current end — that's
how lesson 6 introduced array append. The same `a[i] = v` form lets
you mutate any element, not just append.

(Note: `array.insert_at` ALSO returns a new array — it's not a
mutate-in-place alternative. Use index-assignment for any
hot-loop accumulation.)

## Size and access

| Function | Returns | Effect |
|---|---|---|
| `array.size list` | int | Element count |
| `array.first list` | element (same type as list items) | First element |
| `array.last list` | element (same type as list items) | Last element |

:::example
```scriban
{{ items = [10, 20, 30, 40]
   'size=' + (items | array.size) + ' first=' + (items | array.first) + ' last=' + (items | array.last) }}
```
```text
size=4 first=10 last=40
```
:::

## Slicing

| Function | Returns | Effect |
|---|---|---|
| `array.limit list n` | array | Take first `n` elements |
| `array.offset list n` | array | Drop first `n` elements |
| `array.remove_at list i` | array | Drop element at index `i` |

:::example
```scriban
{{ items = [10, 20, 30, 40, 50]
   items | array.limit 3 }}
{{ items | array.offset 2 }}
{{ items | array.remove_at 1 }}
```
```text
[10, 20, 30]
[30, 40, 50]
[10, 30, 40, 50]
```
:::

## Ordering

| Function | Returns | Effect |
|---|---|---|
| `array.reverse list` | array | Reverse the order |
| `array.sort list member?` | array | Sort ascending; sort by `obj.member` when given |
| `array.uniq list` | array | Deduplicate (preserves first occurrence) |

:::example
```scriban
{{ [1, 2, 3, 4, 5] | array.reverse }}
```
```text
[5, 4, 3, 2, 1]
```
:::

:::example
```scriban
{{ [3, 1, 4, 1, 5, 9, 2, 6, 5] | array.sort }}
{{ [3, 1, 4, 1, 5, 9, 2, 6, 5] | array.uniq }}
{{ [{n: 3}, {n: 1}, {n: 2}] | array.sort 'n' | array.map 'n' }}
```
```text
[1, 1, 2, 3, 4, 5, 5, 6, 9]
[3, 1, 4, 5, 9, 2, 6]
[1, 2, 3]
```
:::

## Filter / map / each

The higher-order trio. Each takes a list and a function reference
(remember `@function_name` from
[lesson 7](/scriban-tutorial/lesson/07-functions)):

| Function | Returns | Effect |
|---|---|---|
| `array.filter list @fn` | array | Keep elements where `@fn(elem)` is truthy |
| `array.each list @fn` | array | Transform every element by `@fn` |
| `array.map list 'member'` | array | Pluck a member out of each element |

:::example
```scriban
{{ [' a', ' b', ' c'] | array.each @string.strip }}
```
```text
["a", "b", "c"]
```
:::

`array.map` is a shorthand that extracts a member name from each
element — it's `array.each` specialised for object navigation:

:::example
```scriban
{{ users = [{name: 'Ada'}, {name: 'Babbage'}, {name: 'Carl'}]
   users | array.map 'name' | array.join ', ' }}
```
```text
Ada, Babbage, Carl
```
:::

## Search

| Function | Returns | Effect |
|---|---|---|
| `array.contains list v` | bool | `true` if `v` ∈ `list` |
| `array.any list @fn args?` | bool | `true` if any element satisfies `@fn` |

:::example
```scriban
{{ [1, 2, 3, 4] | array.contains 3 }} / {{ ['hi', 'world'] | array.any @string.contains 'or' }}
```
```text
true / true
```
:::

## Compaction

| Function | Returns | Effect |
|---|---|---|
| `array.compact list` | array | Drop null entries |

:::example
```scriban
{{ [1, null, 2, null, 3] | array.compact }}
```
```text
[1, 2, 3]
```
:::

## Combine

| Function | Returns | Effect |
|---|---|---|
| `array.join list sep fn?` | string | Join into a string with separator |
| `array.cycle list group?` | element (same type as list items) | Cycle through elements across calls |

`array.cycle` is the trick for alternating row classes in a loop:

:::example
```scriban
{{ for x in 1..6 }}{{ array.cycle ['odd','even'] }} {{ end }}
```
```text
odd even odd even odd even 
```
:::

Each call to `array.cycle` advances internally to the next element,
wrapping around at the end.

## What about `array.any` returning a filtered subset?

`array.any` does NOT return the matching elements — it returns a single
boolean. To get the subset, use `array.filter`:

:::example
```scriban
{{ ['', 'hi', '', 'yo'] | array.filter @string.empty }}
```
```text
["", ""]
```
:::

That returns the EMPTY strings (where `@string.empty` is truthy). To
get the inverse (non-empty strings), define a small "not empty" helper
function and filter on it:

:::example
```scriban
{{ func is_set; ret !($0 | string.empty); end
   ['', 'hi', '', 'yo'] | array.filter @is_set }}
```
```text
["hi", "yo"]
```
:::
