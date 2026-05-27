Use `capture` to render a sentence built from data-model fields into a
variable `headline`. The captured text should read:

    Welcome to <site>, <user>!

Then apply a two-step pipe to `headline`: first `string.upcase`, then
`string.append " ***"`. So the final output adds emphasis around an
already-rendered template fragment.

With the data below the expected output is:

    WELCOME TO ATLAS, ADA! ***
