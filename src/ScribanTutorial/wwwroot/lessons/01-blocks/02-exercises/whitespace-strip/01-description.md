The template below renders `name` inside angle brackets, with a single
space padding on each side of the `{{ }}` tag. Rendered as-is, the output
would be `< ada >` — the padding spaces leak through. Add the **greedy**
whitespace strippers (`-`) on both sides of the tag so they eat the
padding, and the output reads `<ada>` with no inner whitespace.

Replace each `???` with a `-`.
