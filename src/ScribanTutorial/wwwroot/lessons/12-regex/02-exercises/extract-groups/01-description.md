Pull the area code and local number out of a US-style phone string
using a regex with two capture groups, then format them.

Use `regex.match` with the pattern `(\d{3})-(\d{4})` (three digits, a
dash, four digits) against `phone`. The match returns an array of
length 3 — full match in `[0]`, then each capture group in `[1]` and
`[2]`. Print `area=<group1> local=<group2>`.

With `phone = "Tel: 555-1234 ext 22"` the expected output is
`area=555 local=1234`.
