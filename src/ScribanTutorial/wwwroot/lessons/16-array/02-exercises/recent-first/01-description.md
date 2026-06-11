An `events` log is appended oldest-first, so the most recent entry sits
LAST. Render the three most recent events, newest first: pipe through
`array.reverse` to flip the order, then `array.limit 3` to keep the
newest three, then `array.join ', '` to print them on one line.

With `events = ["boot", "login", "upload", "deploy", "shutdown"]` the
expected output is `shutdown, deploy, upload`.
