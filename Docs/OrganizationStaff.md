# Owner-managed staff accounts

Owners of approved organizations can create Organizer login accounts linked to their own organization. They choose an active non-Owner role from the database, enter a temporary password, and share it directly. Identity hashes the password; responses never return it. Existing email addresses are rejected. User and membership creation share a database transaction.

## Database change owned by you

Users.IsFirstLogin is a non-nullable bool/SQL bit. Set the default/backfill to false for existing accounts; owner-created staff accounts explicitly start true. There are no new tables. The user creates/reviews/applies migrations; none were generated or edited here.

Visual Studio Package Manager Console:

```powershell
Add-Migration AddUserIsFirstLogin -Project Shared.Infrastructure -StartupProject EventProject -Context EventProjectDbContext -OutputDir Migrations
# Review the migration: only the intended IsFirstLogin addition, with existing accounts set to false.
Update-Database -Project Shared.Infrastructure -StartupProject EventProject -Context EventProjectDbContext
```

## Endpoints

- GET /api/organizations/me/staff: list this organization's members, including inactive members and the protected Owner row.
- GET /api/organizations/me/staff/roles: active assignable organization roles, excluding Owner.
- POST /api/organizations/me/staff: {fullName, email, phoneNumber, temporaryPassword, organizationRoleId}; creates staff and returns safe account/membership details (201).
- PATCH /api/organizations/me/staff/{membershipId}/role: {organizationRoleId}.
- PATCH /api/organizations/me/staff/{membershipId}/access: {isActive}; also rotates the account security stamp so existing JWTs remain invalid even after reactivation.
- GET /api/organizer/auth/session: current organization and membership, including membership.isFirstLogin. This endpoint remains available during first login but checks active membership and profile permission.
- POST /api/organizer/auth/change-password: {currentPassword, newPassword}; checks active membership, uses Identity validators/hashing, disallows reusing the current password, and clears IsFirstLogin atomically with the password change. Sign in again afterwards because the security stamp changes. This endpoint is rate limited.

All staff management endpoints require a completed first login and an approved organization's Owner, verified against current database state. They derive organization ID from the authenticated account. Cross-organization member IDs return 404; Owner rows cannot be edited or deactivated; the Owner role cannot be assigned. Existing profile/marketplace policies deny first-login users. Future protected organization endpoints must use these policies (or equivalent database-backed first-login checks), not merely RequireRole("Organizer"). Authentication/session and password-change endpoints intentionally permit first-login accounts.

## Frontend

/organizer/staff shows account creation, team members, role editing and activation controls. Only approved Owners see its navigation link. Role options come from the API. Temporary passwords never appear in action state, logs or client storage.

/organizer/change-password lives outside the workspace layout to avoid redirect loops. Login redirects first-login staff there; the workspace guard checks the live session endpoint and blocks bypassing that page. Successful password changes remove the organizer cookie and return to login with a confirmation. Admin cookies remain separate.

## Verification limits

Source inspection only. No tests, builds, type checks, application startup or database operations were run. Existing baseline mock fixtures were adjusted for the session response; no new tests were executed. Apply your migration before running the updated application.