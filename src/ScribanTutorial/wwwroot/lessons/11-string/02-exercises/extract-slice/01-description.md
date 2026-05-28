Extract the file extension from a path. Given a string like
`/home/ada/notes.txt`, find the index of the last `.` and slice from
there to the end.

`string.index_of` only returns the FIRST index, so to find the LAST `.`,
you'll need a small trick: split the path on `"."` and take the last
element via `array.last` (covered in lesson 16, but used here as a
forward reference). Pipe the result through `string.prepend "."` to add
the leading dot back.

With `path = "/home/ada/report.final.pdf"` the expected output is `.pdf`.
