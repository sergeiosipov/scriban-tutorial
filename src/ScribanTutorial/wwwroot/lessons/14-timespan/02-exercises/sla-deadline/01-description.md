A support ticket opened at `opened = "2026-06-10 09:00:00"` must be
resolved within `sla_days = 3` days. Compute the due moment by adding
a timespan to a date: build the interval with `timespan.from_days`,
add it to the parsed date with `+`, and format the result as
`Due YYYY-MM-DD HH:MM` via `date.to_string`.

Date + timespan is the supported direction (timespan + timespan is
not — see *Combining timespans* in the theory). Keep the
`timespan.from_days` call parenthesised: it makes the precedence
obvious, and parentheses become mandatory the moment a call sits on
the *left* of an operator (greedy argument parsing — see the *Date
arithmetic* section in lesson 13).

Expected output: `Due 2026-06-13 09:00`.
