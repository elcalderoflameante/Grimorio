# Order Creation Access Checks

```powershell
dotnet run --project backend/tests/OrderCreationAccess.Checks/OrderCreationAccess.Checks.csproj
```

Runs a local ephemeral HTTP server with JWT authentication, the real POS controller,
permission policies and administrator bypass. A recording mediator replaces order
persistence: no database, inventory, SRI or email services are used.

Checks delivery/takeout for combined waiter/cashier roles in either order, explicit
cash-charge permission, waiter/admin access, table-only waiters, revoked cashier
permission, missing create permission, anonymous access and server-side branch/user
propagation. The existing creation policy remains required.
