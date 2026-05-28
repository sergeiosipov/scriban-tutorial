Walk an object whose keys aren't known at template-authoring time. The
data has a `config` object — print each key/value as `key=value` on its
own line, in declaration order.

Use `object.keys` and a `for` loop, indexing the original object with
`config[key]` to fetch the value.

With the data below the expected output is:

    host=example.com
    port=8080
    ssl=true
