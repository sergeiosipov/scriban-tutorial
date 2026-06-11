This template should print the midpoint between `low` and `high` —
add the two, divide by two. But the parentheses got lost, and operator
precedence does the rest: in `low + high / 2` the division binds
tighter, so with `low = 10, high = 16` it prints `18` instead of the
midpoint.

Add parentheses so the addition happens before the division. The fixed
template renders `13`.

Heads up: your template is also checked against hidden inputs with
different numbers, so solve it with real logic rather than printing the
expected text.
