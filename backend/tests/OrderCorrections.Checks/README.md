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
