# Event draft phase — proposed implementation

The Events backend and draft frontend pages are implemented. The user reported applying the database migration before frontend implementation. The assistant has not run builds, tests or database operations.

Approved organization members with events.manage can create, list, view and edit their own organization's fully populated drafts. The backend derives organization/actor IDs from the authenticated session and checks active membership and completed first login. Publishing, payments, cancellations, deletion and CrewMember applications belong to later phases.

## dbo.Events columns

- EventId (primary key; never generic Id for a new table)
- EventOrganizationId (FK to the existing EventOrganizations key)
- EventStatusId (FK to EventStatus.EventStatusId)
- Name
- Description
- VenueName
- Address
- City
- State
- PostalCode
- StartsAtUtc
- EndsAtUtc
- RequiredCrewMembers
- HourlyPayRate
- AreBreaksPaid
- ApplicationsCloseAtUtc
- CreatedByUserId (FK to Users)
- UpdatedByUserId (FK to Users)
- CreatedAtUtc
- UpdatedAtUtc

No RowVersion column, equivalent row-version column, response version, hidden version field, or If-Match requirement. Future draft updates will not detect stale form submissions using a version token.

## dbo.EventStatus columns

Exactly EventStatusId, EventStatus, IsDeleted. Seed Draft through embedded JSON with stable backend/frontend enum IDs. Add further statuses when publishing is designed.

## Validation and pages

Event name required/max 200; description required/max 4000; venue and address required; future start; end after start with overnight events allowed; positive whole-number crew requirement; positive decimal INR hourly rate; explicit paid-break selection; application close no later than start. UI uses India time, persistence uses UTC. Partial autosave is not included.

GET/POST /api/organizations/me/events, GET/PUT /api/organizations/me/events/{eventId}. Pages: /organizer/events, /organizer/events/new, /organizer/events/{eventId}, /organizer/events/{eventId}/edit. Navigation and pages use events.manage; APIs independently enforce authorization and organization scope. Only Draft events can be edited.

Implementation order: backend/module/model/API changes, user-created and applied migration, frontend integration, user manual verification. The assistant does not run builds, tests or migrations.
## Backend implementation and your migration

Modules/Events contains presentation, Core and Infrastructure projects, registered in EventProject and EventProject.sln. The existing shared DbContext owns migration history; Events contributes IModuleModelConfiguration. Organization FKs reference the current EventOrganizations.Id; the new Events primary key is EventId. Status IDs serialize numerically and the matching frontend enum is in FrontEnd/src/types/events.ts.

Visual Studio Package Manager Console:

```powershell
Add-Migration AddDraftEvents -Project Shared.Infrastructure -StartupProject EventProject -Context EventProjectDbContext -OutputDir Migrations
# Review: dbo.Events, dbo.EventStatus, Draft seed row, FKs, checks and indexes.
# Include any other intentional pending model changes, such as removing the old organization RowVersion.
Update-Database -Project Shared.Infrastructure -StartupProject EventProject -Context EventProjectDbContext
```

No migration/snapshot was generated or edited by the assistant. No build, test, type check, application startup or database command was run.

Additional reference endpoint: GET /api/organizations/me/events/statuses returns EventStatusId, EventStatus and IsDeleted fields (camelCase in JSON), ordered by ID and read from the database. Like the other endpoints, it requires an approved organization and events.manage.

Example POST/PUT body (replace the example dates with future event dates):

```json
{
  "name": "Riverfront evening event",
  "description": "Guest assistance and event setup.",
  "venueName": "Riverfront venue",
  "address": "Venue address",
  "city": "Ahmedabad",
  "state": "Gujarat",
  "postalCode": "380001",
  "startsAtUtc": "2027-01-15T20:00:00+05:30",
  "endsAtUtc": "2027-01-16T02:00:00+05:30",
  "requiredCrewMembers": 20,
  "hourlyPayRate": 150.00,
  "areBreaksPaid": false,
  "applicationsCloseAtUtc": "2027-01-15T18:00:00+05:30"
}
```

Send timestamps with an explicit offset or Z; stored/returned timestamps are normalized to UTC. End and application-close are validated against start. A past application closing time is allowed on a draft by the current agreed rule; publishing validation is a later phase. Missing paid-break choice is rejected, as are invalid crew counts, overlong/blank fields, nonpositive pay and pay with more than two fractional digits. APIs ignore client-supplied organization, status or audit fields and derive them server-side.

List accepts page (default 1), pageSize (default 20, max 100), and optional numeric eventStatusId. Responses use eventId, eventOrganizationId and numeric eventStatusId with the master label in eventStatus. Unknown or another organization's event returns 404. Missing tokens return 401, insufficient access returns 403, invalid requests return 400, and edits of a non-Draft or deleted-Draft master return 409. Successful creation returns 201 and a Location pointing to event details; PUT returns 200. Both operations use a database transaction.

There is no version or If-Match contract. Concurrent draft edits may overwrite earlier edits. Repeated POST requests can create separate drafts, so clients should not automatically retry creation after uncertain network outcomes.