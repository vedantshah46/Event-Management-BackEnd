# Event Organization accounts

## Scope

Public company registration creates one Organizer user, one pending EventOrganization and one Owner membership atomically. Admins approve or reject registrations. An Organizer can belong to one organization; each organization can have many members. Staff invitations, profile updates, suspension controls and UPI editing APIs are deferred to subsequent steps. No organization frontend or CrewMember changes are included.

Existing Admin APIs and the Admin frontend contract remain unchanged. JWT issuance now supports the real platform role. The configured issuer/audience have deliberately been preserved so existing Admin tokens do not need to change solely because of this implementation.

## Tables and relationships

All tables use dbo. Shared.Infrastructure continues to own Users and the single EventProjectDbContext/migration history.

| New table | Key and constraints |
| --- | --- |
| EventOrganizations | Guid Id; required company/contact/address fields; integer EventOrganizationStatusId FK; index on EventOrganizationStatusId/CreatedAtUtc |
| OrganizationMemberships | Guid Id; FKs to organization, Users and OrganizationRoles; unique UserId enforces one organization per Organizer; organization/active index |
| EventOrganizationStatus | Exactly EventOrganizationStatusId (enum-backed PK), EventOrganizationStatus (unique display name), IsDeleted; JSON-managed master; restrict-delete references from organizations/history |
| OrganizationRoles | Stable integer Id, unique Code, Name, IsActive; JSON-managed master data |
| OrganizationRolePermissions | Composite key OrganizationRoleId/PermissionCode; FK to OrganizationRoles; JSON-managed master data |
| OrganizationPaymentDetails | Organization ID is both PK and FK, giving at most one payment-details row; UpiId and updater/timestamp; no row required at registration |
| OrganizationStatusHistory | Guid Id; organization and actor FKs; previous/new statuses, timestamp, reason; organization/time index |

Cross-account and organization relationships restrict cascading deletes. No role/UserRole tables are added to ASP.NET Identity. An Organizer is one platform role; Owner/Staffing Manager/Attendance Manager are organization membership roles.

States: PendingApproval, Approved, Rejected, Suspended. Registration always assigns PendingApproval and OWNER on the server. Review transitions are PendingApproval -> Approved or PendingApproval -> Rejected. Re-review/resubmission and suspension endpoints are not yet implemented.

## Module boundaries

Modules/Organizations has presentation, Core and Infrastructure projects. Application services coordinate registration and review; repositories handle queries/persistence. OrganizationAccountService uses UserManager/SignInManager, preserving Identity password hashing and lockout. A single transaction spans user creation, company creation, membership and status history. Duplicate email races map to 409; failed work rolls back. Review reads the current row under transaction-scoped SQL update/serializable locks, so simultaneous reviews cannot both succeed without a version column.

OrganizationModelConfiguration implements the shared IModuleModelConfiguration interface. The host registers it with DI; the shared context invokes registered configurations without referencing the Organizations module. Model cache keys include the registered module types so model-only tests and the complete application do not share incompatible cached models. Use the API host as startup project for migrations so the module is registered at design time.

## JSON master data workflow

Files are in Modules/Organizations/EventProject.Modules.Organizations.Infrastructure/MasterData:

- EventOrganizationStatus.json: IDs 1 PendingApproval, 2 Approved, 3 Rejected, 4 Suspended; names and ordering for the UI.
- OrganizationRoles.json: roles with fixed IDs 1, 2 and 3.
- OrganizationRolePermissions.json: mappings to supported permission codes.

The JSON files are embedded resources and read by OrganizationMasterData during model construction. Model mappings use HasData. EF compares these values with the previous migration snapshot when YOU generate a new migration. InsertData/UpdateData/DeleteData operations in that migration are applied by your Update-Database command. Database update does not independently inspect arbitrary JSON or infer ungenerated changes.

Workflow for every future master change:

1. Edit the relevant JSON and rebuild. Do not use --no-build with stale binaries.
2. Generate a new migration yourself.
3. Review its schema/data operations.
4. Apply it yourself with Update-Database or dotnet ef database update.

Keep IDs stable. Renaming an ID causes a delete/insert rather than a rename. Retire referenced roles with IsActive=false instead of deleting rows; membership FKs intentionally prevent deleting roles in use. Keep an active OWNER and profile-read access for every active role. Role codes are stable application identifiers, while Name is the display label. New permission codes must also be added to the supported permission constants. Invalid/duplicate IDs, codes, role references, or unknown permissions fail validation. No timestamps or random IDs are generated for master data.

JSON is authoritative for these master rows. Directly editing them in SSMS can cause drift and should be avoided. This design is for small, versioned master data, not operational users/organizations or secrets.

Reference: https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding#model-managed-data

## Your migration commands

Stop debugging/running the API before building or migrating: Windows otherwise locks its output DLLs. No running API process was stopped by the assistant; automated verification used isolated build output.

In Visual Studio Package Manager Console:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
Add-Migration AddOrganizationAccounts -Project Shared.Infrastructure -StartupProject EventProject -Context EventProjectDbContext -OutputDir Migrations
# Review the generated migration, including role and permission InsertData operations.
Update-Database -Project Shared.Infrastructure -StartupProject EventProject -Context EventProjectDbContext
```

Or, from BackEnd in a normal terminal:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef migrations add AddOrganizationAccounts --project Shared/Shared.Infrastructure --startup-project EventProject --context EventProjectDbContext --output-dir Migrations
# Review before applying.
dotnet ef database update --project Shared/Shared.Infrastructure --startup-project EventProject --context EventProjectDbContext
```

Choose one command route; do not create the same migration twice. The CLI requires an installed compatible dotnet-ef tool; Package Manager Console commands require Microsoft.EntityFrameworkCore.Tools if not already available in your Visual Studio setup. EF Design is already referenced by the startup project. No migration or model snapshot was generated or modified by the assistant.

## API endpoints

| Method | Route | Access |
| --- | --- | --- |
| POST | /api/organizations/register | Anonymous, rate limited |
| POST | /api/organizer/auth/login | Anonymous, rate limited; Organizer accounts only |
| GET | /api/organizations/me | Active Organizer membership with profile permission |
| GET | /api/organizations/me/membership | Same; returns current role/permissions |
| GET | /api/admin/organizations?page=1&pageSize=20&status=PendingApproval | Admin; pageSize 1-100 |
| GET | /api/admin/organizations/{id} | Admin |
| POST | /api/admin/organizations/{id}/approve | Admin |
| POST | /api/admin/organizations/{id}/reject | Admin; reason required |

Status IDs serialize as numbers. Admin review does not accept or require a version value or If-Match header. Already-reviewed organizations return 409. A rejected organization cannot be approved through this endpoint without a future resubmission workflow.

OrganizationProfile policy checks membership, role and permissions against the current database. ApprovedOrganization additionally requires Approved status and is available for future marketplace endpoints. These checks do not rely on stale organization claims in a JWT. Pending/rejected/suspended organizations can access their own status/profile while their membership is active; they cannot use approval-gated marketplace operations. There are no marketplace endpoints in this step.

The existing authentication configuration validates signature, issuer, audience, expiry, the current account's platform role and security stamp. Existing Admin behavior is preserved. An Admin credential cannot log in through the Organizer login endpoint, and Organizer tokens cannot authorize Admin endpoints.

Registration and Organizer login have per-process fixed-window rate limits of 10 and 20 requests/minute respectively. They are initial local defaults; distributed limiting is future deployment work.

UPI data is not accepted at registration or returned in profile/list responses. GSTIN is not required in this step. Staff invitation-based account creation is the next organization capability, not a shared-password workflow.

## Swagger verification after YOUR migration

1. Restart EventProject with its https launch profile and open https://localhost:7180/swagger.
2. Register a test company with POST /api/organizations/register:

```json
{
  "ownerFullName": "Company Owner",
  "email": "owner@example.com",
  "password": "CHOOSE_YOUR_OWN_STRONG_PASSWORD",
  "confirmPassword": "CHOOSE_YOUR_OWN_STRONG_PASSWORD",
  "organizationName": "Ahmedabad Live Events",
  "contactEmail": "contact@example.com",
  "contactPhone": "+919876543210",
  "address": "Your company address",
  "city": "Ahmedabad",
  "state": "Gujarat",
  "postalCode": "380001"
}
```

Use actual test values and a password meeting the existing Identity rules. Do not commit credentials. Expect 201 with organizationId, userId and PendingApproval. Verify all four operational inserts (user, organization, membership, initial history) and three role/fourteen permission master rows in SSMS.

3. Repeat registration with the same email: expect 409 and no second organization.
4. Log in through POST /api/organizer/auth/login. Expect 200 even while pending. Authorize Swagger with its accessToken. Check /me and /me/membership: only the caller's organization and OWNER role are returned.
5. Attempt an Admin organization endpoint with the Organizer token: expect 403.
6. Log in through the existing Admin login, replace Swagger authorization with that token, list pending companies, inspect one and approve it. Expect Approved and a review history row with the Admin actor. For rejection use a different pending company and a nonempty reason.
7. Switch back to the Organizer token and read /me: status now shows Approved without issuing a new token.
8. Check invalid registration input (400), unknown organization ID for Admin (404), duplicate review (409), and missing tokens (401).
9. Recheck the existing Admin frontend login/profile to confirm regression behavior.

## Automated verification and limits

Tests use in-memory account/repository/transaction doubles, real JWT middleware/authorization and EF model metadata. They never create accounts or schema in SQL Server and do not execute migrations. They cover registration ownership/status, duplicate registration, inactive memberships, role isolation, cross-organization access, approval policies, rejection reasons/history, pending-state checks, JSON seed rows and dbo constraints. SQL-backed transactional rollback, actual Identity registration/lockout and actual migration execution must be verified after your migration. Existing Admin tests remain part of the suite.

To avoid output locks while an older API instance is running, the assistant used:

```powershell
dotnet test EventProject.sln --artifacts-path .artifacts/organization-validation
```

## Status master update (November 2026)

The C# OrganizationStatus enum and TypeScript EventOrganizationStatusId enum define matching stable IDs. Workflow decisions use enums; labels and filter options come from dbo.EventOrganizationStatus, seeded from EventOrganizationStatus.json. Status JSON must contain every supported enum ID exactly once, with a unique display name. The table has exactly EventOrganizationStatusId, EventOrganizationStatus and IsDeleted. Add new workflow statuses to both enums and JSON together. Retire referenced statuses with IsDeleted=true rather than deleting them. Registration and review require an active target status; existing records remain readable/filterable when a status is inactive.

GET /api/organizations/statuses returns [{id, code, name, isActive, sortOrder}] from the database, ordered by EventOrganizationStatusId. The API keeps its existing response shape: id/name map to the named master columns, code comes from the enum, isActive is the inverse of IsDeleted, and sortOrder is the integer ID; these are not extra database columns. Organization responses now return numeric eventOrganizationStatusId plus eventOrganizationStatusCode and eventOrganizationStatusName. Registration returns numeric eventOrganizationStatusId. The old status field is removed. Admin list filtering uses ?eventOrganizationStatusId=1 (not ?status=PendingApproval). Frontend and backend changes must be used together.

The organization Status string column becomes integer EventOrganizationStatusId referencing dbo.EventOrganizationStatus.EventOrganizationStatusId. History PreviousStatus/NewStatus become nullable PreviousEventOrganizationStatusId and required NewEventOrganizationStatusId, also referencing that master. History table name stays unchanged. All three foreign keys restrict deletion.

You must generate and review this migration yourself. No migration or snapshot was changed by the assistant. Because the existing columns contain strings, do not apply a generated drop/add or direct string-to-int alteration without preserving data:

1. Create and seed dbo.EventOrganizationStatus first (IDs above).
2. Add temporary nullable integer FK columns alongside all existing status string columns.
3. Populate those columns with the explicit enum mapping PendingApproval=1, Approved=2, Rejected=3, Suspended=4. Do not match old codes to display labels such as Pending approval. Preserve null previous history statuses. Abort the migration if any non-null old value cannot be mapped; do not default it silently.
4. Make organization/new-history IDs non-null only after backfill succeeds. Drop the old status check constraint and old status index, then remove the replaced string columns. Ensure final column names match the model.
5. Add the new indexes and three foreign keys after data has been mapped. Check the generated Down path also maps IDs back to the original enum codes before dropping the master if rollback needs to preserve existing data.

Visual Studio Package Manager Console commands for you:

```powershell
Add-Migration AddEventOrganizationStatusMaster -Project Shared.Infrastructure -StartupProject EventProject -Context EventProjectDbContext -OutputDir Migrations
# Review and adjust the migration to preserve existing string status values as described above.
Update-Database -Project Shared.Infrastructure -StartupProject EventProject -Context EventProjectDbContext
```

Editing JSON does not update the database by itself: generate a new migration and apply it for each JSON change. Build, test and migration commands were not executed by the assistant for this change.
## Removal of organization RowVersion

The organization entity, EF mapping, response DTO and frontend no longer contain RowVersion/version. Review has no If-Match header or hidden version input. Transaction-scoped database locks protect the one-time review transition; ordinary future edits will not use version-based conflict detection.

You must generate and apply a migration to drop EventOrganizations.RowVersion if it exists in your database. No existing migration or snapshot was edited, and no migration command was executed. Historical snapshots may still mention the removed column until you generate the new migration.

```powershell
Add-Migration RemoveOrganizationRowVersion -Project Shared.Infrastructure -StartupProject EventProject -Context EventProjectDbContext -OutputDir Migrations
# Review the generated changes yourself.
Update-Database -Project Shared.Infrastructure -StartupProject EventProject -Context EventProjectDbContext
```

Future table primary keys must be entity-specific (Events.EventId, for example). Existing generic keys are not renamed as part of this change.
## Company registration validation

POST /api/organizations/register requires owner full name, login email, password, confirmPassword, company name/email/phone/address/city/state/postal code. Required fields reject missing/empty/whitespace strings. Display names and errors use the visible form labels. Existing maximum field lengths remain enforced. Emails must be valid; phone allows an optional leading + and spaces/parentheses/dots/hyphens with 7–15 digits and a maximum of 20 characters; postal code requires exactly six ASCII digits. Passwords require 12–256 characters, uppercase, lowercase, a digit, and a special character. confirmPassword is required and must match password. Identity still independently validates/hashes the password and rejects duplicate login emails.

DTO validation executes through ApiController before account creation; the application service also validates the request before starting its registration transaction. Invalid requests return 400 with validation messages; duplicate account conflicts remain 409. The frontend sends confirmPassword but never returns either password in action state. ConfirmPassword is request-only and adds no database column. Required fields have visible asterisks and native required attributes. No migration is needed for this change. No builds or tests were executed.