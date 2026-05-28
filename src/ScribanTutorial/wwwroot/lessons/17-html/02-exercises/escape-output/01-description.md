Escape a string for safe inclusion in HTML markup. The `user_input`
field contains characters that would be interpreted as markup if
emitted raw.

Wrap it in a `<p>...</p>` element and use `html.escape` to safely
include the value.

With `user_input = "She said \"<3>\" & smiled"` the expected output is:

    <p>She said &quot;&lt;3&gt;&quot; &amp; smiled</p>
