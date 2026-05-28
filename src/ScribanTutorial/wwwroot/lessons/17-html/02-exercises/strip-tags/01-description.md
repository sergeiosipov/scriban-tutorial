Extract a plain-text preview from a snippet of HTML. The `snippet` is
a small fragment with markup; pipe it through `html.strip` to get the
text-only version.

With `snippet = "<p>Hello <em>world</em>, this is <b>Scriban</b>!</p>"`
the expected output is `Hello world, this is Scriban!`.
