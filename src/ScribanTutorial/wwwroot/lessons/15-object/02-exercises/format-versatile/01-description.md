Use `object.format` to format two different value types with .NET-native
format strings on one line.

The data has a number `n = 255` and a date `d = "2024-03-15"`. Print:

    hex=<00FF> month=<2024-03>

Where:
- `<00FF>` is `n` formatted with `"X4"`.
- `<2024-03>` is `d` (parsed first) formatted with `"yyyy-MM"`.

Remember `object.format` uses **.NET native** format strings — `X4` for
hex, `yyyy-MM` for year-month (no `%` prefix).
