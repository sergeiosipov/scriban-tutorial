How many days remain before a deadline? The data has two ISO date
strings: `today = "2026-06-10"` and `deadline = "2026-07-01"`.

Parse both, subtract the earlier from the later — date minus date
returns a `TimeSpan` — and read its `.TotalDays`. Print
`<n> days left`.

Keep the two parsed dates in intermediate variables: the inline form
`date.parse a - date.parse b` trips over greedy argument parsing (see
the *Date arithmetic* section in the theory).

Expected output: `21 days left`.
