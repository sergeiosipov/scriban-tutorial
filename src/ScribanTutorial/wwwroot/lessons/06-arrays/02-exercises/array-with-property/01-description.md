The data model gives you three integers `a`, `b`, `c`. Build a fresh
array `squares` of their squares, then attach a named property
`squares.title = "powers"` to the array — the "arrays-as-objects"
pattern: the array carries both the elements and a piece of metadata
about them.

Then print `<title>: <s0>, <s1>, <s2>` on one line. With the data below
(3, 4, 5) the output should read:

    powers: 9, 16, 25

Note: a literal array `[expr, expr, expr]` built inside the template is
mutable, but arrays that came through the data model from JSON are
read-only — that's why we build a fresh `squares` rather than attaching
a property to the input.
