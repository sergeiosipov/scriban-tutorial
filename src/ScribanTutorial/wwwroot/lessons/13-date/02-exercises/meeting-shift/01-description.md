A meeting scheduled for `start = "2026-03-05 09:00:00"` slips by
`delay_hours = 2` hours and `delay_minutes = 30` minutes.

Parse the timestamp, push it back with `date.add_hours` and
`date.add_minutes` (chained in a pipe, like `add_days` in the earlier
exercise), and print the new time as `Moved to HH:MM` using
`date.to_string`.

Expected output: `Moved to 11:30`.
