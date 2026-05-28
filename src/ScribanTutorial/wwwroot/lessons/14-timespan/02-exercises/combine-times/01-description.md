Combine two timespans into one. Given `h` hours and `m` minutes, build
them as two separate timespans, then assemble a single combined
timespan via the `.TotalSeconds` sum trick.

Print the combined timespan directly — Scriban formats it as
`HH:MM:SS` (or `d.HH:MM:SS` if ≥ 24h).

With `h = 1` and `m = 45`, the combined timespan is 1 hour 45 minutes,
which renders as `01:45:00`.
