# Sale Tips Checks

Real HTTP, JWT permission policies and a disposable PostgreSQL database. No SRI,
email, inventory workers or production database connections.

Only accepts localhost:15432, database `checks`, user `checks`.

```powershell
docker run --rm -d --name grimorio-tip-checks -e POSTGRES_USER=checks -e POSTGRES_PASSWORD=checks -e POSTGRES_DB=checks -p 127.0.0.1:15432:5432 postgres:15
$env:TIPS_CHECKS_CONNECTION='Host=127.0.0.1;Port=15432;Database=checks;Username=checks;Password=checks'
dotnet run --project backend/tests/SaleTips.Checks/SaleTips.Checks.csproj
docker stop grimorio-tip-checks
```

Set `TIPS_CHECKS_ALLOW_RESET=true` only to recreate this guarded disposable database.
Tests cover validation, branch isolation, view/write permissions, concurrent retries,
editing, deletion audit, method/date filters, totals across pages, and unchanged
cash-session balances, sales, inventory and electronic documents.

Tips are one active record per sale/payment. Methods are the independent choices
Cash, Card and Transfer. Dates in the report refer to registration time (UTC,
displayed in the branch time zone), not the date of the original sale. The existing
Billing.Cash.View and Billing.Cash.Charge permissions control access.
