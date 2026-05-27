Use range expressions outside of a `for` loop — they're values you can
index into, join, or pass through array filters.

Given `start = 3, end = 7`, print two values on one line separated by
` / `:

1. The **third** element of the range `start..end` (inclusive on both
   ends).
2. The same range, exclusive on the upper bound (`start..<end`), joined
   into a comma-separated string.

With `start=3, end=7` the output should be `5 / 3,4,5,6`.
