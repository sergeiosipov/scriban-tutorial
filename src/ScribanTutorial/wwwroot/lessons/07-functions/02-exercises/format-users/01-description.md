Consolidating exercise — uses arrays, objects, and functions together.

The data carries a `users` array of `{name, score}` records. Define an
inline function `format(u)` that takes a user record and returns the
string `"<name>: <score>"`. Then pipe the users array through
`array.each` with `@format` as the per-element function — that yields
an array of formatted strings — and finally pipe the result through
`array.join` with `" | "` as the separator to produce a single line.

For the data below the output should read:

    Ada: 90 | Babbage: 75 | Carl: 85

This exercise touches: array iteration, object member access, inline
function definition, function-pointer reference, and the pipe call
style. If any of these still feel hazy, jump back to the relevant
lesson before solving.
