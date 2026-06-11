Define a **simple** function called `shout` that takes one string
argument and returns it uppercased with a `"!"` appended. Then call it
on the `name` field from the data model.

The body is pre-filled for you: it uses `string.upcase` to uppercase the
input and `string.append '!'` to attach the exclamation mark. The
placeholders hide the **structural** pieces of a function definition —
the opening keyword that names it, the closing keyword, and the keyword
that hands the value back to the caller. Fill those in (no
`(parameters)` list here — this is the simple form, so the argument
arrives as `$0`).
