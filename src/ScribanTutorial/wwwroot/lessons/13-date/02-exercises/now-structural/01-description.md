`date.now` changes on every render, so you can't assert its exact
value — but you can assert its *structure*, the same trick lesson 10
uses for `math.uuid`.

Print `recent=<bool>` where `<bool>` checks that the current year is
2024 or later: take `date.now`, read its `.Year` property, and compare
with `>= 2024`.

Expected output:

    recent=true
