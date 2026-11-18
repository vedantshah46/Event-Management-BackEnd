# EventProject backend

Requires the .NET SDK selected by global.json. The first Admin API slice uses SQL Server LocalDB and the existing EventStaffingDb database configured in EventProject/appsettings.Development.json.

See Docs/AdminApi.md for database tables, user-run migration commands, local secrets, explicit first-admin provisioning and API test steps. The user exclusively creates and applies migrations; the app never creates or migrates the database automatically.

From BackEnd, after completing the setup guide:

```powershell
dotnet build EventProject.sln
dotnet test EventProject.sln
dotnet run --project EventProject --launch-profile https
```

Process health: https://localhost:7180/health (does not verify database readiness).
Admin login: POST /api/admin/auth/login.
Current admin: GET /api/admin/me (Bearer token required).

If the local HTTPS certificate is not trusted, run `dotnet dev-certs https --trust` yourself.

Organization account APIs and JSON-managed master data are documented in Docs/OrganizationAccounts.md. Apply your new organization migration before testing these routes. Stop the running API before rebuilding its normal output.
