The `products` array holds four item names. Render them as an HTML grid
two columns wide, using the loop construct built for exactly this job.
Each item lands in a `<td class="colN">` cell, and every two cells get
wrapped in a `<tr class="rowM">` row. With the data below the expected
output is:

```
<tr class="row1"><td class="col1">Lamp</td><td class="col2">Desk</td></tr>
<tr class="row2"><td class="col1">Chair</td><td class="col2">Rug</td></tr>
```
