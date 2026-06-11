The data model carries two flags, `a` and `b`. Combine them with the
boolean operators (`!`, `&&`, `||`) so the template renders the exclusive
OR — `true` when exactly one of `a` and `b` is `true`, `false` otherwise.

With `a = true, b = true` the expression should produce `false` — exactly
the case where a plain `&&` or `||` gives the wrong answer.

Replace the `???` with a boolean expression that does NOT use `if` /
`else` — just the three operators on `a` and `b`.

Heads up: your template is also checked against hidden inputs, so solve
it with real logic rather than printing the expected text.
