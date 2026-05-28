Use `object.default` to provide fallbacks for missing OR empty fields.

The data has `name = ""` (set but empty) and `role = null` (unset).
For both, `object.default` should return the literal `"Anonymous"`.

Print:

    name: Anonymous
    role: Anonymous
