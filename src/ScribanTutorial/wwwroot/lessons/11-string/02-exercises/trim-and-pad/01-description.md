Format a `label` so it sits inside a fixed-width 10-character column
on the right, with surrounding whitespace stripped first. The output
should be exactly `[<padded>]` where `<padded>` is `label` after:

1. `string.strip` to remove the leading/trailing spaces in the raw data
2. `string.pad_left 10` to right-align it in a 10-char field

With `label = "  ok  "` the expected output is `[        ok]`.
