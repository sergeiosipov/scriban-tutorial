Combine the ternary `? :` with `&&` and `||` to make a multi-condition
decision in one expression.

Given a user record with `is_admin` and `account_active`, print one of:

- `"VIP"` when both `is_admin` AND `account_active` are true
- `"NEW"` when `account_active` is true but the user is not admin
- `"BLOCKED"` when `account_active` is false

You can use **nested ternaries** to make this fit on one line:
`<cond1> ? <a> : <cond2> ? <b> : <c>`. With the data below
(admin=false, active=true) the output should read `NEW`.

Heads up: your template is also checked against hidden inputs that land
in the other branches, so solve it with real logic rather than printing
the expected text.
