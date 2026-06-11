Print the Windows path `C:\Users\you\notes.txt` exactly as written, as a
single expression.

In a regular string — single- or double-quoted — the parser treats `\` as
the start of an escape, so every backslash would have to be doubled:
`'C:\\Users\\you\\notes.txt'`. Use a **verbatim** string (backticks)
instead: escape processing is off, and the backslashes pass through
untouched.
