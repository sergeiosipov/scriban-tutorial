Apply the "defence-in-depth" pattern for untrusted input: strip any
HTML the input contained, then escape the remainder for safe inclusion
in markup.

The pipeline: `html.strip` first (removes whole tags AND the text
between them — `<script>alert(1)</script>` becomes an empty string),
then `html.escape` (turns any remaining specials into entities).

Wrap the result in `<p>...</p>`.

With `untrusted = "Hello <script>alert(1)</script> & friends"` the
expected output is `<p>Hello  &amp; friends</p>` — `<script>alert(1)
</script>` was stripped entirely (the script body is GONE, not just
the tags), leaving a double space where it used to live, and the
surviving `&` was escaped to `&amp;`. That's a stronger defence than
just escaping: the alert call can never run.
