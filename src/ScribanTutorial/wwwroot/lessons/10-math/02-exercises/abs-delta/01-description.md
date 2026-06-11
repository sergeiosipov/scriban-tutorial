Compute the absolute forecast error and report whether it's even or
odd.

The data has `forecast = 120` and `actual = 133` — the units a store
expected to sell versus what it actually sold.

1. Compute the delta: `(forecast - actual) | math.abs`. The
   subtraction gives `-13`; `math.abs` strips the sign, so it doesn't
   matter which way the forecast missed.
2. Report the parity: pipe the delta through `math.modulo 2` and
   compare the result to `0` — `even` when it is, `odd` when it isn't.

Both inputs are integers, so the delta renders as `13`, not `13.0`.
Expected output:

    delta=13 parity=odd
