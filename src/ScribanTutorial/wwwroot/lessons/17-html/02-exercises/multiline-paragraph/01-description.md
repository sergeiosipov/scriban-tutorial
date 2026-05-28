Convert a multi-line message into an HTML paragraph that preserves
line breaks via `<br />` tags.

Use `html.newline_to_br` on the input, then wrap the result in
`<p>...</p>`.

With `body = "line A\nline B"` the expected output is:

    <p>line A<br />
    line B</p>
