Iterate over the range `1..6`, but use the `offset:` parameter to skip
the first two values and `reversed` to walk what remains back-to-front.
Print the values separated by single spaces. Expected:

```
6 5 4 3
```

Note the order of operations: `offset:` (and `limit:`) trim the sequence
first, then `reversed` flips the iteration direction.
