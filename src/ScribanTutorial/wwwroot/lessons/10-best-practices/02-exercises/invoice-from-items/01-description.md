Same shape as the `invoice` capstone but the data model **does not**
carry pre-computed subtotals or a grand total — just `qty` and
`unit_price` per item. Compute each subtotal and the grand total
**in the template**.

Three patterns to combine:

1. An inline function `subtotal(line) = line.qty * line.unit_price` so
   each row's number is named, not buried in an expression.
2. A local variable `$grand` accumulated inside the `for` loop (`$grand`
   is local — it doesn't leak into the global scope).
3. The same whitespace control as `invoice` so each line lands on its
   own row.

Expected output:

```
Invoice for Ada Lovelace
------------------------
widget (x3): $30
gadget (x2): $50
sprocket (x1): $12
------------------------
Total: $92
```
