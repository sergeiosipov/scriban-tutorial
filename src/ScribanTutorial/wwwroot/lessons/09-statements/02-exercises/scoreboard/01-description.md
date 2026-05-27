Consolidating exercise — uses `for`, `if`, `for.last`, comma-joined
statements, and an outer `capture` to pre-render the scoreboard before
piping it through a final filter.

The data carries `players`, an array of `{name, score}` records. Build a
captured scoreboard string of the shape `"name1: score1, name2: score2,
..."` (no trailing comma), but **skip any player whose score is 0** —
they don't appear in the list at all.

Then take the captured string and prepend `"SCOREBOARD: "` via
`string.prepend`.

With the data below (Ada=90, Babbage=0 skipped, Carl=85, Dora=70), the
expected output is:

    SCOREBOARD: Ada: 90, Carl: 85, Dora: 70
