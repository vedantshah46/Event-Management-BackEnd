# Backend architecture

One ASP.NET Core host runs a modular monolith. Initial projects establish Identity and shared foundations; other modules are added incrementally.

## Dependencies

- Module presentation -> its Core.
- Module Infrastructure -> its Core and Shared.Infrastructure.
- Module Core -> Shared.Core and Shared.DTOs.
- Shared.Infrastructure -> Shared.Core.
- API host -> module presentation and Infrastructure.

Core contains Domain and Application feature folders when those features are implemented. Infrastructure contains persistence and technical service implementations. Controllers are discovered from module assemblies by the API host.

Platform roles: Admin, Organizer, CrewMember. Each account has one platform role. Organization roles are separate permissions; an Organizer account belongs to one organization.

Planned modules: Identity, Organizations, CrewMembers, Events, Staffing, Attendance, Finance. Shared code must remain business-neutral.

One SQL Server database and one EF Core context/migration history are configured. Shared.Infrastructure owns the common Identity account model and EventProjectDbContext to avoid circular dependencies. Admin login and profile endpoints are implemented in the Identity module. The health endpoint reports process health only, not database readiness. The user exclusively creates and applies migrations.

See AdminApi.md for the first Admin slice, schema and setup. Migrations and live SQL verification are pending user execution. Finish Admin capabilities before Organizations and CrewMembers.

The executable API project is located directly at BackEnd/EventProject; there is no Host folder.

Shared/Shared.DTOs contains dependency-free DTOs reused across modules. Module-specific DTOs stay in their module Core/Application folders. Shared.Core does not depend on Shared.DTOs; domain types remain separate from transport contracts.

Database schema policy: every current and future table uses dbo. Do not introduce identity or per-module SQL schemas. Project/module names remain independent of SQL schema names.

## Organization module

Organizations owns company registration, membership roles/permissions and Admin review. Shared account infrastructure supports Admin and Organizer role tokens. Shared DbContext consumes DI-registered IModuleModelConfiguration implementations so Shared never references a module. Organization master roles/permissions are embedded JSON mapped through HasData; see OrganizationAccounts.md for the user-owned migration workflow. All tables use dbo.
