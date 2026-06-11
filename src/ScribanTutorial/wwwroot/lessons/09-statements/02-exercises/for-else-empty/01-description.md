Render the search `results`, one per line, each prefixed with `- `. The
catch: the data model's `results` array is empty, so the loop body never
runs and nothing would print. Give the loop a fallback branch — the
loop-flavoured cousin of `if`'s `else` — so the template emits a
friendly message instead.

Expected output: `No matching results.`
