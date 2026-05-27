Pass an **anonymous** function as the block argument to a built-in.

The data carries an array `items`. Transform it by squaring each
element, using `array.each` with a `do ... end` block. The block
receives each element as `$0` and should return the squared value.

The result of `array.each` is an array, so the template prints it in
Scriban's standard array form. With `items = [3, 5, 7]` the output
should read `[9, 25, 49]`.

The placeholders hide the start and end keywords of the anonymous
function block.
