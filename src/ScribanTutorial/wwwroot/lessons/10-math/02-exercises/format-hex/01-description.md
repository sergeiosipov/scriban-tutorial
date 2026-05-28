Format the `color` integer as a 6-digit hexadecimal string suitable for
CSS — uppercase letters, prefixed by `#`. Use `math.format` with a `.NET`
numeric format string for the hex part, and string concatenation to add
the `#`.

With `color = 16711680` (which is `FF0000`) the expected output is
`#FF0000`.
