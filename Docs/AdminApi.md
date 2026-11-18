# Admin API: first implementation step

## Scope

Implemented: shared account persistence model, admin password login, short-lived JWTs, authenticated current-admin profile, explicit operator provisioning command. No admin public registration exists. Organization and CrewMember workflows are deferred. This is the authentication foundation, not the entire admin panel API. Admin management, dashboard metrics, audit trails, password recovery, MFA and browser session/refresh design remain future work.

## Database model (not applied)

The shared infrastructure owns the common Identity account model and EventProjectDbContext. This avoids a dependency from Shared into a module. Identity services and controllers remain in the Identity module. ASP.NET Core Identity acts as the account repository; wrapping its complete API in a generic repository would add no value. IAdminAccountStore is a focused application-facing abstraction.

| Table | Purpose |
| --- | --- |
| dbo.Users | Guid key; FullName (150), Email and NormalizedEmail (256), PasswordHash, PlatformRole (20), IsActive, CreatedAtUtc, security/concurrency stamps, lockout fields, and other Identity account fields |
| dbo.UserClaims | Identity claims linked to a user |
| dbo.UserLogins | Identity external-login supporting table; no external login endpoint yet |


Users have one required PlatformRole: Admin, Organizer, or CrewMember. A SQL check constraint restricts these values. Normalized email and normalized username have unique indexes. Identity uses ConcurrencyStamp for account update concurrency. No Roles or UserRoles tables are created. No separate AdminProfile is necessary until there are admin-specific profile fields.

## Security behavior

- Identity hashes passwords. Provisioned passwords must be 12–256 characters and satisfy Identity's uppercase/lowercase/digit/non-alphanumeric requirements.
- Five wrong passwords lock the account for 15 minutes. Login returns a generic 401 for missing, wrong-role, inactive, locked, or incorrect-password accounts.
- Login has a shared per-process limit of 20 requests/minute and no request queue. This initial limiter must be reviewed for deployment scale and trusted proxy configuration.
- JWTs expire after 15 minutes. Signature, issuer, audience, lifetime, algorithm, and current admin account/security stamp are validated.
- No refresh endpoint exists yet; sign in again after expiry. No public API can provision or promote an admin.
- Signing keys and provisioning passwords are not committed. Do not store browser bearer tokens in localStorage; browser session handling is a later frontend design step.
- The health endpoint checks process health only.

## Setup: commands for the user only

Run these yourself from BackEnd. No migration or database creation command has been executed by the assistant. An existing database still needs the Identity tables from your migration.

### 1. Store a local JWT signing key

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$jwtSigningKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
dotnet user-secrets set 'Jwt:SigningKey' $jwtSigningKey --project EventProject
Remove-Variable jwtSigningKey
```

User secrets are for local development. Use deployment secret storage in production. Missing or short signing keys fail startup validation.

### 2. Create, review, and apply your migration

Use a compatible dotnet-ef 10.0.13 installation. If not installed, install it yourself using your preferred tool scope. From BackEnd:

```powershell
dotnet ef migrations add InitialAdminIdentity --project Shared/Shared.Infrastructure --startup-project EventProject --context EventProjectDbContext --output-dir Persistence/Migrations
# Inspect the generated migration before applying it.
dotnet ef database update --project Shared/Shared.Infrastructure --startup-project EventProject --context EventProjectDbContext
```

SSMS should show dbo.Users, dbo.UserClaims, dbo.UserLogins and __EFMigrationsHistory afterward. No EnsureCreated, startup migration or automatic database creation is used. Review constraints, unique indexes, and the absence of role join tables in your migration.

### 3. Provision the first admin explicitly

This command writes an admin account to your already-migrated database. Run it yourself after step 2. It refuses to overwrite or promote an existing account. Values below use session environment variables; no default credentials are supplied.

```powershell
$env:AdminProvisioning__Email = Read-Host 'Admin email'
$env:AdminProvisioning__FullName = Read-Host 'Admin full name'
$adminPassword = Read-Host 'Admin password' -AsSecureString
try {
    $env:AdminProvisioning__Password = [System.Net.NetworkCredential]::new('', $adminPassword).Password
    dotnet run --project EventProject --launch-profile https -- --provision-admin
}
finally {
    Remove-Item Env:AdminProvisioning__Email -ErrorAction SilentlyContinue
    Remove-Item Env:AdminProvisioning__FullName -ErrorAction SilentlyContinue
    Remove-Item Env:AdminProvisioning__Password -ErrorAction SilentlyContinue
    Remove-Variable adminPassword -ErrorAction SilentlyContinue
}
```

The command creates only an account, never tables. Normal API startup does not provision accounts.

### 4. Run and test

```powershell
dotnet run --project EventProject --launch-profile https
```

In Postman:

1. POST https://localhost:7180/api/admin/auth/login with JSON containing your email and password. Expect 200 with accessToken, expiresAtUtc, tokenType and admin profile.
2. GET https://localhost:7180/api/admin/me with Authorization: Bearer <accessToken>. Expect 200 and your profile, without password hash or security stamp.
3. Repeat GET without a token: expect 401.
4. Submit an invalid email format: expect 400; a wrong password: 401.
5. Repeat wrong passwords five times for an existing admin; a correct password must remain rejected during the 15-minute lockout. The HTTP rate limit can also return 429.

```powershell
dotnet test EventProject.sln
```

Automated tests cover login service behavior, EF model metadata, signed JWT validation, and HTTP authentication/authorization. HTTP tests substitute an account store and do not connect to SQL Server. Live SQL mappings, Identity password lockout and admin provisioning still need verification after you apply your migration.

## References

- https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0
- https://github.com/advisories/GHSA-2fmv-rv2c-59x7 (patched cryptography package explicitly referenced to avoid an affected transitive dependency)

## Swagger UI

The Development launch profile opens https://localhost:7180/swagger. Login through POST /api/admin/auth/login, copy accessToken, click Authorize and paste only the token (the UI supplies Bearer). Swagger is available only in Development. No database change is needed to display documentation.

## UserTokens excluded

The EF model excludes IdentityUserToken<Guid> at the user's request. JWT login and profile access do not use persisted Identity tokens. Identity token-storage operations, including stored authenticator keys, recovery codes and external authentication tokens, must not be used without first implementing a supported storage design. UserClaims and UserLogins remain in the model and use dbo.

If the initial migration has already been applied, the user should create a new migration removing identity.UserTokens, review it and apply it. Existing migrations and the model snapshot are intentionally left untouched by the assistant.

## Database schema policy

All current and future tables must use dbo. The DbContext default schema is dbo, and Users, UserClaims and UserLogins are explicitly mapped to dbo. The user creates and applies migrations transferring existing tables from their old schema to dbo; review that existing rows are preserved instead of dropping/recreating tables. Existing migrations and snapshots are not modified by the assistant.
