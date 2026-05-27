The data model gives you a `product` with a `name` and a `price`. Two
things to add and then print:

1. Add a **global** variable `currency = "USD"` (plain assignment, no
   prefix — writes to the global scope per [lesson 4](/scriban-tutorial/lesson/04-variables)).
2. Add a **member** to the existing object: `product.total = product.price * 2`
   (two units of the product).

Then print all three on one line: name, currency, total, separated by
single spaces. Expected output: `Widget USD 19.98`.
