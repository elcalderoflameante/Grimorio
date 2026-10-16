# Password Reset Checks

This test host runs real HTTP/JWT requests and a kitchen WebSocket against an isolated
PostgreSQL database. It does not start production workers or send email/push messages.

Only `127.0.0.1:15432`, database `checks`, user `checks` is accepted. Create a disposable
PostgreSQL instance for these checks. Never point it at the application database.

```powershell
$env:PASSWORD_CHECKS_CONNECTION = 'Host=127.0.0.1;Port=15432;Database=checks;Username=checks;Password=checks'
dotnet run --project backend/tests/PasswordReset.Checks
```

To repeat the checks against the same disposable database, explicitly set
`PASSWORD_CHECKS_ALLOW_RESET=true`. This drops and recreates only the guarded checks
database. Without that flag the checks apply migrations and create test fixtures.

Covered: migration/model consistency, admin and branch restrictions, password hashing,
temporary-password access limited to changing the user's own password, disabled PIN
login until the change, session version revocation, immediate WebSocket disconnection,
device push-token deactivation, audit fields and concurrent credential updates.

Password reset requires the active `Administrador` role. The administrator delivers the
temporary password privately; it is not persisted in plaintext or returned by the API.
The first change is completed through the responsive web screen. Afterward the user
signs in again and can resume using the apps/PIN.

Live connections are aborted through the API's singleton registry, consistent with the
current single-instance deployment. JWT version checks use the database on every request.
Multiple API instances would additionally need distributed session-revocation events.
