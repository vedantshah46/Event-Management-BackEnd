# Development checklist

- [x] Approve module-based backend structure.
- [x] Adopt CrewMember terminology.
- [x] Create host, Shared, and Identity project boundaries.
- [x] Configure module controller discovery and process health endpoint.
- [x] Design initial Identity mappings and configure EF Core / SQL Server.
- [x] User created and applied the initial Identity migration.
- [ ] Implement Identity and organization permissions.
- [ ] Implement events, staffing, offers, and bookings.
- [ ] Implement attendance and finance.
- [x] Build and verify the first Admin Bootstrap 5 frontend slice.
- [ ] Complete release validation.

Major-phase transitions require user confirmation. Commission and other unresolved financial policies must not receive invented defaults.

- [x] Add Shared.DTOs project and reference it from Identity.Core.

## Database ownership

The user exclusively creates and applies all migrations. The assistant must not generate migrations, run database updates, or use EnsureCreated or automatic startup migrations. Prepare code and provide commands for the user instead.

- [x] Configure Development DefaultConnection for the user-created EventStaffingDb on LocalDB.
- [x] Initial Admin SQL persistence verified by the user; organization SQL verification pending.

## Admin-first delivery

- [x] Admin login and authenticated profile API.
- [x] Explicit operator-only admin provisioning command (not executed).
- [x] Identity password lockout and JWT validation configuration.
- [x] User applied the initial migration and provisioned the first admin.
- [ ] Verify SQL-backed login and lockout after user migration.
- [x] User chose Admin frontend first, then Organization backend; additional Admin management remains deferred.

- [x] Exclude UserTokens from the EF model as requested; user handles the schema-removal migration.

- [x] Map Users to dbo.Users; user handles schema-transfer migration.

- [x] Set dbo as the default and map all current tables to dbo. All future tables must use dbo. User handles existing-table schema transfers via migrations.

## Organization accounts and JSON master data

- [x] Approved account creation and Admin review plan.
- [x] Add Organizations presentation, Core and Infrastructure projects.
- [x] Map six organization tables with dbo-only schemas, unique memberships and concurrency protection.
- [x] Embed role/permission JSON and load it through deterministic, validated HasData mappings.
- [x] Implement registration, Organizer login, own profile/membership, Admin list/detail/approve/reject.
- [x] Preserve Admin JWT/API contracts while supporting Organizer role tokens.
- [x] Run 23 unit/HTTP tests without SQL or migration execution.
- [ ] User generates, reviews and applies organization schema/master-data migration.
- [ ] User verifies live registration, transactions, login and reviews in Swagger/SSMS.
- [ ] Implement staff invitations, updates and UPI editing in a subsequent step.
