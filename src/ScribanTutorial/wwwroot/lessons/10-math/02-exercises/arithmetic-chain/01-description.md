Use a pipe chain of `math.*` functions to compute the final amount of
an order. Given `base = 80`, apply these operations in order:

1. Multiply by `1.25` (markup).
2. Subtract `10` (loyalty discount).
3. Divide by `2` (split between two people).

Use `math.times`, `math.minus`, `math.divided_by` in a single pipe.

Expected output: `45`.
