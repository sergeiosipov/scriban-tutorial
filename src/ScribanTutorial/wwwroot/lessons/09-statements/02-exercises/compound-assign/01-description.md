A `stats` object carries `hits`, `misses`, and `score`. Apply the
following compound assignments **in order**, then print all three
values on one line as `<hits>/<misses>/<score>`:

1. `stats.hits` += `1`
2. `stats.misses` -= `2`
3. `stats.score` *= `1.5` (note this promotes `score` to a float)

With the initial data below the output should read:

    11/3/15
