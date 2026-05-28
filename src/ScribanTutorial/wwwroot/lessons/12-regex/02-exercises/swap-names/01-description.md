Use `regex.replace` with capture-group back-references to rewrite a
list of names from `"First Last"` form to `"Last, First"`.

Each name in the data is a single string `name` of the form
`"<first> <last>"`. Match `(\w+)\s+(\w+)` and replace with `$2, $1`.

With `name = "Ada Lovelace"` the expected output is `Lovelace, Ada`.
