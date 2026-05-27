A customer record where every field below `user` could be missing.
Print the user's preferred display name following this priority chain,
with the first non-null winning:

1. `user.profile.preferred_name` if set.
2. Otherwise `user.contact.email` (a contact handle).
3. Otherwise `user.login`.
4. Otherwise the literal `"anonymous"`.

Use `?.` on each level that could be absent (`profile`, `contact`) so
the chain produces `null` instead of erroring, and `??` to chain the
four alternatives. The whole expression goes on a single line — `??`
needs its right operand on the same line as the operator.

For the data below, only `user.id` exists, so all three named
alternatives resolve to `null` and the literal `"anonymous"` wins.
