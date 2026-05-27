Define a simple function `cube` that returns `$0 * $0 * $0`. Capture
it under a new name `cb` using the function-pointer prefix, so that
both `cube` and `cb` refer to the same callable.

Then call `cb` on the data model's `n` field, and on the literal `2`,
joined by `" / "`. With `n = 4` the output should read `64 / 8`.

Hint: without the function-pointer prefix, assigning `cb = cube`
**invokes** `cube` with no arguments instead of capturing the function
itself. The lesson covers the single character that switches the
meaning from "call" to "reference."
