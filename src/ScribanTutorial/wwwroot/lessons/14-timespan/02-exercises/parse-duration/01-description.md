Parse a textual timespan and report it in plain English. The data has
`raw = "2.03:30:00"` — two days, three hours, thirty minutes.

Use `timespan.parse` to parse the value, then build a sentence:

    2 days, 3 hours, 30 minutes

Use `.Days`, `.Hours`, `.Minutes` for the component breakdown.
