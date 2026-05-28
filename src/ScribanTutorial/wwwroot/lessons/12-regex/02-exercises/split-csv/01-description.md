Split a CSV row on commas, but **tolerate spaces around each comma**.
Use `regex.split` with a pattern that matches `\s*,\s*` (optional
whitespace on each side of the comma).

The result is an array; print it as-is so Scriban renders the standard
array form.

With `row = "alpha , beta,gamma  ,delta"` the expected output is
`["alpha", "beta", "gamma", "delta"]`.
