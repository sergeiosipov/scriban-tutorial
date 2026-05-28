Use `array.cycle` to stripe a list of items with alternating row
classes. Loop over `items` and emit one line per item of the shape:

    <class>: <item>

Where `<class>` alternates between `"odd"` and `"even"` (cycled with
`array.cycle ["odd", "even"]`).

With `items = ["apple", "banana", "cherry", "date"]` the expected
output is:

    odd: apple
    even: banana
    odd: cherry
    even: date
