The template was meant to print `Release 2.1;Changelog updated;Done`,
but the multi-line comment in the middle is missing its closing `##`.
Scriban does not raise an error — the comment silently runs to the
closing `}}` of the code block, swallowing the `'Changelog updated;'`
statement, so the template renders `Release 2.1;Done` instead.

Add the closing `##` right after the comment text (before the `;` that
separates it from the next statement) so the swallowed statement runs
again.
