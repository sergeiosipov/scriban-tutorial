Pass a regex pattern to `regex.split` to split `"this is a text"` on
whitespace. Use a **verbatim** string (backticks) for the pattern so the
backslash in `\s` doesn't have to be escaped.

This exercise relies on two ideas that get their own lessons later, but
the gist for now:

- **Pipes (`|`)** send the value on the left into the function on the
  right as its first argument — so `"abc" | string.upcase` is the same as
  `string.upcase "abc"`. Full treatment in
  [lesson 7](/scriban-tutorial/lesson/07-functions).
- **`regex.split`** is one of the built-in functions in the `regex.*`
  module; it splits a string on the pattern. Full treatment in
  [lesson 12](/scriban-tutorial/lesson/12-regex).

`\s+` is the regex pattern for "one or more whitespace characters."
