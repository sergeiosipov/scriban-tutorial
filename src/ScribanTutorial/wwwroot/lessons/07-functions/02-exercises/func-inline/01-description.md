Define an **inline** function `frame` that takes a string `s` and a
single character `c`, and returns `c + s + c` — i.e. it sandwiches the
string between two copies of the character. Then call `frame` on the
data model's `text`, using `"*"` as the frame character.

The placeholders hide the function name, its parameter list, and the
right-hand-side expression that constitutes the body. No `func`/`end`
keywords here — the inline form is a single statement that begins with
the function name.

Watch the parameter order — `frame(s, c)` and `frame(c, s)` differ.
