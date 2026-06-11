The starter template runs as-is — and produces the wrong output. The
helper `tally` sums an array by keeping a running `total`, and the sum
itself is right: the fees add up to 15. But look at the second line:
the caller's own `total`, which arrived from the data model as 100,
now also reads 15.

This is the most surprising rule in Scriban functions (lesson 4 and
this lesson's theory both flag it): a plain assignment inside a `func`
body writes to the GLOBAL scope. The function's bookkeeping variable
and the caller's `total` are the same slot, so every call to `tally`
tramples it.

Fix `tally` so its bookkeeping stays private: prefix the function's
internal variable with `$` to make it function-local — everywhere it
appears inside the body, including the `ret`. The fees line must stay
`15`, and the grand total must survive as `100`:

    Fees: 15
    Grand total: 100
