Take the raw text in `message` and run it through a pipe chain of
string built-ins to produce a marquee-style header:

1. Strip surrounding whitespace.
2. Uppercase the result.
3. Prepend `"*** "`.
4. Append `" ***"`.

Each step is a single `| function` segment. The order matters — the
value flowing through the chain has to be a string at every step, so
strip and upcase have to come before the prepend/append decorations.

With `message = "  hello world  "` the output should read
`*** HELLO WORLD ***`.
