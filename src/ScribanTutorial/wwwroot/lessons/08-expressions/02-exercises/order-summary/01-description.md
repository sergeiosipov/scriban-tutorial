Consolidating exercise — pulls together arithmetic, string coercion,
comparison, ternary, null-coalescing, and a `?!` decoration.

The data carries an order record:

- `qty` (number of items)
- `unit_price` (per item)
- `discount_code` (a string code, or `null` if no discount applied)

Produce one line of output of the shape:

    qty=<qty>, total=$<total>, code=<code>, banner=[<banner>]

Where:

- `<total>` is `qty * unit_price`, rounded to a whole number with
  `math.round`, then concatenated to a leading `$` (use the
  string-coercion rule from this lesson — no `string.format`).
- `<code>` falls back to `"none"` when `discount_code` is null.
- `<banner>` shows `" SAVINGS APPLIED "` (with surrounding spaces) when
  the discount code is non-null, and the empty string otherwise. Use
  `?!` so the decoration **only** appears when the code is set.

With the data below the output should read:

    qty=3, total=$60, code=none, banner=[]
