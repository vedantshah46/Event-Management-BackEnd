# Backend project rules

- Use dbo for every SQL Server table. Do not introduce identity or per-module database schemas.
- The user exclusively creates and applies all migrations. Do not generate/edit migration files or model snapshots, run database updates, or use EnsureCreated or automatic startup migrations. Prepare application/model changes and provide commands for the user instead.
- Use CrewMember terminology, not Worker, for platform roles and new code.
- Work incrementally and obtain user confirmation before moving to another major phase.

- Master data is managed by module-owned, embedded JSON files loaded into EF HasData. Keep stable IDs and deterministic values. The user creates a new migration after JSON changes and applies it; do not add runtime seed/upsert or automatic migration execution.

## User-controlled verification

Do not run tests or builds unless the user explicitly authorizes them again. This includes dotnet test/build, npm build/test, Playwright, type-check commands, and scripts that indirectly trigger tests or builds. Review changes through source inspection and clearly state that execution checks were not run. The user exclusively creates and applies all database migrations.

## Status master convention

All status master tables must use dbo and exactly these three columns: <StatusMasterName>Id (stable enum-backed primary key), <StatusMasterName> (status display name), and IsDeleted (soft-delete flag). Example: dbo.EventOrganizationStatus has EventOrganizationStatusId, EventOrganizationStatus, and IsDeleted. Do not add generic Id, Code, Name, IsActive or SortOrder columns to status masters. Reference the named ID with foreign keys. Keep frontend/backend enum IDs aligned with embedded JSON. Deleted statuses cannot be assigned to new records or transitions, but existing references and history remain readable; never physically delete referenced master rows. The user exclusively creates and applies migrations.
## Primary keys and concurrency columns

For every new table, use a table/entity-specific primary key name, such as EventId for Events, rather than a generic Id. Name foreign keys consistently with the referenced key. This rule applies to future tables; do not rename existing keys unless explicitly requested. Do not introduce RowVersion, SQL rowversion/timestamp columns, or equivalent row-version concurrency columns on any table. Do not require row-version values or If-Match headers in API/frontend contracts. Existing migration history and model snapshots remain user-managed; do not edit them to remove historical columns.