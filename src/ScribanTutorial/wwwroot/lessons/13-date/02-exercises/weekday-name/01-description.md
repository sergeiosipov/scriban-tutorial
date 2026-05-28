Format the weekday name and the day-of-year for a date together.
Given a parsed date, produce:

    <FullWeekday>, day <N> of year

Where `<FullWeekday>` is e.g. `Monday`, `Tuesday`, ... and `<N>` is the
1-based day of the year.

Use `date.to_string` with `%A` for the weekday, and the `.DayOfYear`
property for the day number.

With `d = "2024-03-15"` the expected output is `Friday, day 75 of year`.
