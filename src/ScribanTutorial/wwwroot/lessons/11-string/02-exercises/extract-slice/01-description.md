Extract the file extension from a path like `/home/ada/notes.txt` —
without slicing at all.

`string.index_of` only returns the FIRST index, and a path can contain
more than one `.`, so an index-and-slice approach doesn't reach the
last dot. Instead: split the path on `'.'` with `string.split`, take
the last element via `array.last` (covered in lesson 16, but used here
as a forward reference), then pipe the result through
`string.prepend '.'` to add the leading dot back.

With `path = "/home/ada/report.final.pdf"` the expected output is `.pdf`.
