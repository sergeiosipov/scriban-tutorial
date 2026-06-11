Real pipelines do two things: **select** the lines you care about,
then **transform** each one. This exercise chains three modules:

- `string.contains` — filter predicate (lesson 11)
- `array.filter` with a `@`-prefixed function reference — keep only
  `ERROR` lines (lesson 16)
- `regex.replace` with a verbatim backtick pattern — redact IPv4
  addresses (lesson 12)

Fill in the two `???` blanks:

1. The backtick-delimited regex pattern that matches an IPv4 address
   (four groups of `\d+` separated by literal `.`).
2. The `@`-prefixed function reference passed to `array.filter`.

Expected output:

    ERROR [IP] auth-failed
    ERROR [IP] timeout
