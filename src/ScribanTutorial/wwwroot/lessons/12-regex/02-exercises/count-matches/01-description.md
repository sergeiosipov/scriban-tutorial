Count how many words in `text` contain at least one digit. Use
`regex.matches` with a pattern that matches a word containing a digit,
then take its `.size`.

The pattern: a word boundary, one or more word characters that include
at least one digit. The classic shape is `\b\w*\d\w*\b` — a word with
at least one digit anywhere inside.

With `text = "log line 23 at 4pm and again on day 7"` the expected
output is `3` (the words `23`, `4pm`, `7`).
