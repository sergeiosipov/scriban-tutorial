Define a **parametric** function `make_url` with the following signature:

- `host` (required)
- `scheme` defaulting to `"https"`
- `port` defaulting to `443`
- `path` defaulting to `"/"`

It returns a URL string of the shape `<scheme>://<host>:<port><path>`.

Then call it from the data model so that `scheme` and `path` use their
defaults, but `port` is overridden to `8080` via a named argument. Use
the parentheses call form for clarity.

The placeholders hide the parameter list, the call form, and the named
argument syntax. None of these are spelled out in this description on
purpose — refer back to the lesson if needed.
