`for … else` is the loop-flavoured fallback: the `else` branch runs
only when the iterable is empty, saving you a separate
`if (array.size events) > 0` guard around the loop.

The data model supplies a non-empty `events` array — confirming the
`for` body works — but a hidden test case sends an empty array to
verify the fallback runs too.

Your task: fill in the single `???` with the Scriban keyword that
introduces the empty-iterable branch.

Expected output for the visible data:

    June 2026:
    - Sprint Review (2026-06-15)
    - Demo Day (2026-06-28)
