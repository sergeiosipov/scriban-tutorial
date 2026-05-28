Extract date components individually using the **PascalCase**
properties (`.Year`, `.Month`, `.Day`, `.Hour`, `.Minute`).

Given a parsed timestamp, build the string:

    Y2024 M3 D15 @ 13:45

Where `Y2024` is `"Y" + d.Year`, `M3` is `"M" + d.Month`, etc. Times
use a colon between hour and zero-padded minute. (Minute is 45, no
zero-padding needed for that value.)

With the data below (`ts = "2024-03-15 13:45:00"`) the output should
match exactly.
