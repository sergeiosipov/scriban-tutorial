Same shape as the `invoice` capstone but the data model **does not**
carry pre-computed subtotals or a grand total — just `qty` and a
decimal `unit_price` per item, plus an ISO `issued` date. Compute each
subtotal and the grand total **in the template**, and format everything
on the way out.

Patterns to combine:

1. An inline function `subtotal(line) = line.qty * line.unit_price` so
   each row's number is named, not buried in an expression.
2. A local variable `$grand` accumulated inside the `for` loop (`$grand`
   is local — it doesn't leak into the global scope).
3. `math.format 'N2'` (lesson 10) on every money figure so `49` prints
   as `49.00`.
4. `date.parse` + `date.to_string '%d %b %Y'` (lesson 13) for the
   issued date — parenthesise `(date.parse issued)` before the pipe;
   function arguments parse greedily, so the parens are the safe habit.
5. The same whitespace control as `invoice` so each line lands on its
   own row.

Expected output:

```
Invoice for Ada Lovelace
Issued 15 Mar 2026
------------------------
widget (x3): $29.97
gadget (x2): $49.00
sprocket (x1): $12.00
------------------------
Total: $90.97
```
