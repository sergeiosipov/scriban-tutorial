Compute a tax-inclusive price and format it as a fixed-point decimal:

1. Multiply `price` by `(1 + tax_rate)` — that's `1 +` the rate, not
   the rate itself.
2. Round to 2 decimal places with `math.round`.
3. Format with `math.format "N2"` so the output always has exactly two
   decimal digits (so `12` becomes `12.00`).

With `price = 19.99` and `tax_rate = 0.08` the expected output is
`21.59`.
