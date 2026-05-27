Expressions are anything that **evaluates to a value**: literals,
variable reads, arithmetic, comparisons, function calls. This lesson
walks the operators in roughly the order you'll reach for them.

## Variable reads

The simplest expression — a name. Plus the path variants.

| Form | Reads |
|---|---|
| `x` | global named `x` |
| `$x` | local named `x` (per [lesson 4](/scriban-tutorial/lesson/04-variables)) |
| `obj.field` | member `field` of object `obj` |
| `obj["field"]` | same, with a runtime key |
| `arr[i]` | i-th element of array `arr` |
| `arr.label` | named property attached to the array |
| `obj.a.b.c` | chained member access |
| `obj?.a?.b` | chained access with optional-chain short-circuit |
| `this` | the current bound object (top-level: globals; inside `with`: the target) |

:::example
```scriban
{{ user.name }} - {{ user["role"] }} - {{ tags[0] }} - {{ matrix[1][2] }}
```
```json
{ "user": { "name": "Ada", "role": "admin" }, "tags": ["x","y","z"], "matrix": [[1,2,3],[4,5,6]] }
```
```text
Ada - admin - x - 6
```
:::

## Assignment expressions

`=` writes the value on the right into the slot on the left:

:::example
```scriban
{{ x = 1 + 2
   x }}
```
```text
3
```
:::

The left side must be a **variable**, **property**, or **indexer** —
not a general expression. `(a + b) = 5` is a parse error.

Compound assignments (`+=`, `-=`, `*=`, `/=`, `//=`, `%=`) combine a
read and a write:

:::example
```scriban
{{ counters = { hits: 0 }
   counters.hits += 5
   counters.hits *= 2
   counters.hits }}
```
```text
10
```
:::

The full set lives in [lesson 9](/scriban-tutorial/lesson/09-statements)
under Statements.

## Nested expressions and grouping

Parentheses group sub-expressions and override default precedence:

:::example
```scriban
{{ a = 2
   (a + 3) * 4 }} vs. {{ a + 3 * 4 }}
```
```text
20 vs. 14
```
:::

A sub-expression can be used wherever a value can — inside another
expression, as a function argument, as the right side of an assignment.
The grouping is purely syntactic; the parser builds it into the tree
before evaluation.

## Arithmetic on numbers

`+`, `-`, `*`, `/`, `//` (integer division), `%` (modulus).

:::example
```scriban
{{ a + b }} | {{ a - b }} | {{ a * b }} | {{ a / b }} | {{ a // b }} | {{ a % b }}
```
```json
{ "a": 10, "b": 3 }
```
```text
13 | 7 | 30 | 3.3333333333333335 | 3 | 1
```
:::

Note that `/` on two ints produces a `double` (not an int). `//` is the
integer-division operator if you want the floor result:

:::example
```scriban
{{ 7 / 2 }} vs. {{ 7 // 2 }}
```
```text
3.5 vs. 3
```
:::

### Mixing int and float

If either side is a float, the other is promoted and the result is a
float:

:::example
```scriban
{{ 10 / 3 }} - {{ 10 / 3.0 }} - {{ 10.0 / 3 }}
```
```text
3.3333333333333335 - 3.3333333333333335 - 3.3333333333333335
```
:::

The string forms of `double` results in Scriban use C#'s "round-trip"
formatting, which is why you see the trailing 5. For human output, pipe
through `math.format` or `math.round`.

## Arithmetic on strings

`+` glues two strings; `*` repeats a string a number of times. Other
arithmetic operators (`-`, `/`) are NOT supported on strings — they
raise an error:

:::example
```scriban
{{ "ab" + "c" }} | {{ "ha" * 3 }} | {{ "x"+"y"+"z" + (3+2) }}
```
```text
abc | hahaha | xyz5
```
:::

## Conversions: number ↔ string

If **either** side of `+` is a string, the other is **coerced to a
string** — `"qty=" + 5` produces `"qty=5"`, not an error. This is
convenient for building output but a foot-gun when your data shape
slips: `"qty": "4"` (string) and `"qty": 4` (int) behave very
differently:

:::example
```scriban
{{ "5" + 3 }} | {{ 5 + 3 }} | {{ "qty=" + 5 }}
```
```text
53 | 8 | qty=5
```
:::

Force the type when it matters: `int 0 + qty` parses `qty` as an int
even if it came in as a string. `math.format` is the canonical way to
pretty-print numbers.

## Comparison

`==`, `!=`, `<`, `<=`, `>`, `>=` all return booleans:

:::example
```scriban
{{ a == b }} | {{ a != b }} | {{ a < b }} | {{ a <= b }} | {{ a > b }} | {{ a >= b }}
```
```json
{ "a": 5, "b": 7 }
```
```text
false | true | true | true | false | false
```
:::

(Some upstream docs render these as `≠`, `≤`, `≥` — those are
typographic. The real operators are the ASCII pairs above.)

## Logic

`&&` and `||` combine booleans with short-circuit evaluation. The
**ternary** `cond ? a : b` is the inline conditional — `a` when `cond`
is truthy, `b` otherwise:

:::example
```scriban
{{ price > 100 ? "expensive" : "ok" }} | {{ (price > 50 && active) ? "VIP" : "STD" }}
```
```json
{ "price": 250, "active": true }
```
```text
expensive | VIP
```
:::

`&&` and `||` short-circuit — useful for safe guarded access, e.g.
`user && user.name` returns `null` when `user` is null instead of
raising.

## Unary operators

Three:

| Operator | Effect |
|---|---|
| `-x` | numeric negation |
| `+x` | numeric identity (`+(-5)` is still `-5`); rarely useful but accepted |
| `!x` | boolean inversion (`true` ↔ `false`, also flips truthy/falsy) |

:::example
```scriban
{{ -x }} | {{ +x }} | {{ !flag }}
```
```json
{ "x": 7, "flag": true }
```
```text
-7 | 7 | false
```
:::

Unary `-` binds tighter than binary `-`, so `-x - 1` reads as `(-x) - 1`.

## Range expressions

`a..b` is an iterator over the integers from `a` to `b` inclusive.
`a..<b` excludes the upper bound. Both can be used anywhere a value can
— not just in `for` loops.

| Form | Yields |
|---|---|
| `1..5` | 1, 2, 3, 4, 5 |
| `1..<5` | 1, 2, 3, 4 |

:::example
```scriban
{{ (1..5).size }} | {{ (1..<5) | array.join "," }} | {{ (0..9)[3] }}
```
```text
5 | 1,2,3,4 | 3
```
:::

`(1..<5)` is a range; piping into `array.join` collapses it into a
string. Indexing `(0..9)[3]` gets the fourth element — `3` (since the
range starts at 0).

## Null-coalescing

| Form | Reads |
|---|---|
| `a ?? b` | `a` if `a` is non-null, otherwise `b` |
| `a ?! b` | `b` if `a` is non-null, otherwise `null` |

:::example
```scriban
{{ x = 5; y = null
   x ?? "fallback" }} | {{ y ?? "fallback" }} | {{ x ?! "shown" }} | {{ y ?! "missing" }}_end
```
```text
5 | fallback | shown | _end
```
:::

`?!` is the "trigger on presence" cousin. The use case: render decorations
only when an optional field exists. `{{ user.badge ?! "<span>VIP</span>" }}`
emits the span only when `user.badge` is set — no `if` block needed.

## Function-call expressions

A function call is also an expression — it evaluates to whatever the
function returns. Three call forms (recap from [lesson 7](/scriban-tutorial/lesson/07-functions)):

:::example
```scriban
{{ "  ada  " | string.strip | string.upcase }} | {{ string.upcase("ada") }} | {{ string.upcase "ada" }}
```
```text
ADA | ADA | ADA
```
:::

A simple-form function can iterate its variadic `$` to handle any number
of arguments:

:::example
```scriban
{{ func longest
     winner = ""
     for s in $
       if s.size > winner.size; winner = s; end
     end
     ret winner
   end
   longest "go" "python" "javascript" "rust" }}
```
```text
javascript
```
:::

## Built-in filter modules

The built-in modules each get their own lesson:

| Module | Lesson |
|---|---|
| `math.*` | [10](/scriban-tutorial/lesson/10-math) |
| `string.*` | [11](/scriban-tutorial/lesson/11-string) |
| `regex.*` | [12](/scriban-tutorial/lesson/12-regex) |
| `date.*` | [13](/scriban-tutorial/lesson/13-date) |
| `timespan.*` | [14](/scriban-tutorial/lesson/14-timespan) |
| `object.*` | [15](/scriban-tutorial/lesson/15-object) |
| `array.*` | [16](/scriban-tutorial/lesson/16-array) |
| `html.*` | [17](/scriban-tutorial/lesson/17-html) |
