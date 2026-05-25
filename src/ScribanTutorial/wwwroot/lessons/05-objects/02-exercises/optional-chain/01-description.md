The data model has a `user` object without an `address`. Print
`user.address.city` defensively — chain through `address` with the
optional operator (`?.`) and fall back to `unknown` when the chain
resolves to null (`??`).
