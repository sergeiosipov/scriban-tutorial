The host passed configuration as a single `settings` object, but the
template body reads nicer with its fields used bare — `site_name`
instead of `settings.site_name`. Copy every member of `settings` into
the current scope with one statement, then render the summary line.

Expected output: `Atlas: Templates made simple (2026)`.
