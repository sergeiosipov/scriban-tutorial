Build a timespan from `minutes`, then format it as `HH:MM` (zero-padded
hours and minutes in component form).

The minutes value won't necessarily be a multiple of 60. Use
`timespan.from_minutes` to build the interval, then use `math.format`
on `.Hours` and `.Minutes` with the `"D2"` (decimal padded to 2 digits)
format to zero-pad each.

With `minutes = 95` the expected output is `01:35`.
