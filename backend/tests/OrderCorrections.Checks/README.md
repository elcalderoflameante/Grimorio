# Administrator Order Corrections

Uses a guarded disposable PostgreSQL database and a local HTTP/JWT server. No SRI,
mail, real inventory or production services. Only localhost:15432, checks/checks.

```powershell
docker run --rm -d --name grimorio-correction-checks -e POSTGRES_USER=checks -e POSTGRES_PASSWORD=checks -e POSTGRES_DB=checks -p 127.0.0.1:15432:5432 postgres:15
$env:CORRECTION_CHECKS_CONNECTION='Host=127.0.0.1;Port=15432;Database=checks;Username=checks;Password=checks'
dotnet run --project backend/tests/OrderCorrections.Checks/OrderCorrections.Checks.csproj
docker stop grimorio-correction-checks
```

Set `CORRECTION_CHECKS_ALLOW_RESET=true` only to reset this disposable test database.
Checks cancellation across preparation states, audit history, concurrent retries,
option-dependent quantities/prices, original reserved ingredients recorded as waste,
atomic rollback after shortage, paid-line protection, other paid lines unchanged,
administrator/branch isolation, validation, and ledger/stock consistency.
Also covers combo task replacement, draft saving and confirmation ignoring cancelled history,
forced database failure rollback, promotion discount recalculation and original
ingredient quantities retained after catalog recipe changes and maximum-length actor names.

Also verifies sale-note conversion through the authorized HTTP endpoint: partial-payment
snapshots, closed cash sessions, concurrent retries, original payment replay, customer
and branch validation, audit history, and atomic rollback of document/sequence/payment
changes after a forced database failure. No live SRI, certificate signing or email calls.

Table transfer checks cover preparation states, unchanged items/payments/inventory,
source/destination map state, occupied/inactive/foreign tables, stale requests,
concurrent retries and competing transfers/new orders, paid-order protection,
audit history, forced persistence rollback and station table labels.

Delegated correction checks use a non-administrator cashier with the dedicated
POS.Orders.CorrectItems permission. They verify audited cancellation, prepared-dish
option changes and waste, denial without the claim or database grant, immediate
revocation, inactive role/permission/user, branch isolation, paid-line protection,
and separation from whole-order and legacy cancellation permissions.
