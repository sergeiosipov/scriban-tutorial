The data model gives you three integers `a`, `b`, `c`. Build a fresh
array `squares` of their squares, then attach a named property
`squares.title = "powers"` to the array — the "arrays-as-objects"
pattern: the array carries both the elements and a piece of metadata
about them.

Then print `<title>: <s0>, <s1>, <s2>` on one line. With the data below
(3, 4, 5) the output should read:

    powers: 9, 16, 25

Note: arrays that came through the data model from JSON are perfectly
writable as *lists* — element writes like `arr[1] = x` and
append-by-index both work on them. What they refuse is **attached named
properties**: `arr.title = ...` on a data-model array raises a
"readonly member" error. That's why the arrays-as-objects pattern
starts from a fresh array literal built inside the template, like
`squares` here.
