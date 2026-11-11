# Historical Stock Balances

Runs the real movement query on PostgreSQL. Requires the isolated local
`grimorio_checks` database with the current schema. It never reads appsettings
or `.env`; remote hosts and other database names are rejected. All fixtures
are inserted in a transaction and rolled back, including when an assertion fails.

Set `STOCK_BALANCE_CHECKS_CONNECTION` locally, or pass the path to an ignored
JSON file with `host`, `port`, `database`, `username`, and `password`. Do not commit credentials.

```powershell
dotnet run --project backend/tests/StockMovementBalances.Checks -- path/to/local-checks.json
```

Covers complete history before type/date/limit filters, branch/article/warehouse
isolation, soft deletion, base-unit conversions, precision, restorations,
transfers, negative balances, timestamp ties and read-only behavior.

Frontend browser check (Vite running; Playwright path can be supplied through
`PLAYWRIGHT_MODULE`, dev URL through `STOCK_TEST_URL`):

```powershell
node frontend/tests/stockMovementBalances.browser.mjs
```

The browser check mounts the actual component with synthetic authentication and
intercepts every API request. It never contacts the application's database.
