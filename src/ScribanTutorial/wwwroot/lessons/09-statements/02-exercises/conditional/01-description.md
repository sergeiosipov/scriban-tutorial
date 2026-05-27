Print one of three statuses based on the `score` field, using an
`if` / `else if` / `else` chain:

- `"A"` when `score >= 90`
- `"B"` when `score >= 80`
- `"C"` when `score >= 70`
- `"D"` otherwise

The expected output for the data below (`score = 85`) is `B`.

Note: Scriban evaluates the chain top to bottom and uses the FIRST true
branch — the other tests don't fire. So `>= 80` matching is enough; the
`else` branches won't run even though `85` is also "below 90".
