The data model represents a user record with a missing primary email but
a backup on file. Print whichever email is non-null. Use the null-
coalescing operator `??` — `a ?? b` evaluates to `a` if `a` is not null,
otherwise `b`.

With the data below, the output should be `ada@example.com`. (If you also
swap `primary_email` to a real address, the same template should print
that one instead — try it in your head before submitting.)

Heads up: your template is also checked against hidden inputs, so solve
it with real logic rather than printing the expected text.
