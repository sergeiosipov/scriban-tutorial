A more practical use of `this`: build up an object's fields from inside
a `with` block. The data model gives you a partial `user` record with a
first and last name. Inside `with user`, set `this.full_name` to
`first + " " + last`, then print `user.full_name` after the block.

`with` is covered properly in lesson 9 — for now, the syntax is:

```scriban
with <some_object>
  this.<field> = <value>
end
```

Inside the block, plain `first` and `last` read the wrapped object's
fields, and `this.full_name = ...` writes back to it. After `end`, the
object retains the new field.

Expected output: `Ada Lovelace`.
