The template below puts `{{ name }}` inside angle brackets but the data
model's value has a leading and trailing space — so the bare tag would
produce `<  ada  >`. Trim **both** sides of the tag so the output reads
`<ada>` with no inner whitespace.
