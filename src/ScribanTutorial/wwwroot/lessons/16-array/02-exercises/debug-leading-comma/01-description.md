The template below builds a comma-separated string from an array by
appending each item inside a loop. It runs without error, but the
output is wrong — there is a stray `, ` before the first element.

**Diagnose:** the loop always executes `csv | string.append ', '` first,
then appends the item. On iteration 1 `csv` is still `''`, so the
separator is prepended with nothing before it.

**Fix it:** replace the entire loop with the array built-in that joins
items with a delimiter. Look for it in the *Combine* section of the
lesson.

Expected output: `scriban, template, dotnet`
