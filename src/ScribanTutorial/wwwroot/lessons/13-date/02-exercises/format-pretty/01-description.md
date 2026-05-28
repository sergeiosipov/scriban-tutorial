Format a date for a human-readable header. Given `d = "2024-03-15"`,
produce the string:

    Fri 15 Mar 2024

That is: abbreviated weekday (`%a`), day with no leading zero (`%-d`
isn't portable here — use `%d` which gives `15`, since 15 has no
leading zero anyway), abbreviated month (`%b`), 4-digit year (`%Y`).
Use `date.to_string` with the pattern `%a %d %b %Y`.
