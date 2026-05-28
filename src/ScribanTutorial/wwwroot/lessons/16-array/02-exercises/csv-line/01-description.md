Build a CSV line from a `fields` array. Pipe through `array.each` with
`@string.strip` to clean each cell, then `array.join` with `","` to
join them.

With `fields = [" a ", " b", "c "]` the expected output is `a,b,c`.
