For each value in the `samples` array, print its `object.typeof` and
`object.kind` on one line, comma-separated. The data has four sample
values of different shapes.

The output should be one line per sample of the form `typeof/kind`,
each on its own line.

With the data below the expected output is:

    string/string
    number/long
    number/double
    array/array

Why `long` and not `int`? JSON integers come into this app as 64-bit
`long` values via the runtime's `JsonToScriban` converter. Scriban
literals like `y = 42` would be `int`; the `42` from the JSON sample
array is `long`. The lesson 15 theory shows the `int` case using
literals.
