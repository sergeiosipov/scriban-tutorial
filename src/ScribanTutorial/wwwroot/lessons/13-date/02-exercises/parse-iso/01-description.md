Parse an ISO-8601 timestamp string and print just the date portion in
`YYYY/MM/DD` form (slash-separated, not dash-separated).

Use `date.parse` (the no-pattern form handles ISO 8601 natively), then
`date.to_string` with `%Y/%m/%d`.

With `iso = "2024-12-25T10:30:00"` the expected output is `2024/12/25`.
