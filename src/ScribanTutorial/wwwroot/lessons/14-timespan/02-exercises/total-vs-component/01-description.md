Print both the **component** and **total** views of a single timespan
to make the distinction concrete.

Given a timespan built from `seconds = 4500` (75 minutes), produce two
lines:

    component: <Minutes>m <Seconds>s
    total:     <TotalMinutes> min / <TotalSeconds> sec

With `seconds = 4500` the expected output is:

    component: 15m 0s
    total:     75 min / 4500 sec

The `.Minutes` value is the clock-style "minutes-within-the-hour" (so
75 minutes splits into 1 hour 15 minutes — `Minutes = 15`).
`.TotalMinutes` is the whole duration in minutes — `75`.
