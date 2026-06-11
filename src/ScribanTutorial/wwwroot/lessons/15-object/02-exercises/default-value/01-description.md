Use `object.default` to provide fallbacks for missing OR empty fields.

The data has `name = "Ada"` (a real value) and `role = ""` (set but
empty). `object.default` passes `name` through untouched, but treats
the empty string — just like `null` — as missing, so `role` falls back
to the literal `"Anonymous"`.

Print:

    name: Ada
    role: Anonymous
