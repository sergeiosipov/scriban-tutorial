Build a URL slug from a `title` field. Pipe it through:

1. `string.downcase` (lowercase everything)
2. `string.strip` (drop leading/trailing whitespace)
3. `string.handleize` (replace non-alphanumerics with `-`)

With `title = "  Hello, World! 2025  "` the expected output is
`hello-world-2025`.

Note that `string.handleize` already does lowercase-conversion and
non-alphanumeric replacement on its own — chaining `downcase` and
`strip` here is defensive; many real templates would just use
`handleize` alone.
