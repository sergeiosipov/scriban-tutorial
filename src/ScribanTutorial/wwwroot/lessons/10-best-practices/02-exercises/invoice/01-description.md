Build a small invoice document. The data model carries the customer
name, an array of line items (each with `description`, `qty`, and a
**pre-computed** `subtotal`), and a grand `total`. Reproduce the expected
output below. Mind the whitespace control on the `for` block so each
line lands on its own row without an extra blank.

```
Invoice for Ada Lovelace
------------------------
widget (x2): $20
gadget (x1): $25
------------------------
Total: $45
```
