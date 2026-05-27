Combine multiple arithmetic operators in a single expression and respect
operator precedence with parentheses where you need to.

Given `total = 100` and `discount = 25`, print **two** values on one
line, separated by ` / `:

1. The discounted total: `total - discount`.
2. The discounted total after applying VAT at 20%: round the result of
   `(total - discount) * 1.2` to a whole number with `math.round`.

Expected output: `75 / 90`.
