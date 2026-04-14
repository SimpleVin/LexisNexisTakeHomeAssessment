# Design and trade-offs

## Deliverables (per employer brief)

| Deliverable | Location |
|-------------|----------|
| All source code | `src/` (Domain, Application, Infrastructure, Api) and `tests/` |
| Instructions to run locally | [README.md](README.md) |
| Design and trade-offs | This document (`solution.md`). A short video walkthrough may be submitted **instead of or in addition to** this file if the employer allows. |

## Assessment requirements (core scope)

The take-home asked for a **task management API** with EF Core **InMemory**, **seed data**, and:

| Requirement | Where it lives |
|---------------|----------------|
| View and search tasks | `GET /api/work-items`, `q` searches **title** (case-insensitive) |
| Filter by status, assignee, priority | Same endpoint: `status`, `assigneeId`, `priority` (+ optional `createdFrom` / `createdTo`) |
| Create / update / delete tasks | `POST`, `PUT`, `DELETE` under `/api/work-items` |
| Assign tasks to team members | `assigneeId` on create/update (validated against team members) |
| Set and update priority | `priority` on create/update |
| Technical: InMemory + seed | `UseInMemoryDatabase`, `SeedData` + `EnsureCreated` |

Everything below **Architecture** includes **additional** design (Clean Architecture, CQRS, team-member CRUD, soft delete, audit log, assigner tied to the authenticated user on create) that goes **beyond** the minimum brief. The **minimum** is satisfied by the **work items** API and persistence above; extras are documented for transparency, not as required deliverables.

## Architecture

The solution follows **Clean Architecture** with four projects and a strict dependency direction:

- **Domain** — EF-oriented entities (`WorkItem`, `TeamMember`) and enums. No application or framework references.
- **Application** — `UseCases/` (MediatR commands/queries and handlers), `Common/Contracts` (DTOs and HTTP request bodies), `Common/Models/Interface/<area>/` (e.g. `ITeamMemberRepository`), **FluentValidation**, and area folders for application-only types (e.g. work-item enums, list criteria). It does **not** reference Domain so contracts stay free of persistence entity types; enums are duplicated with matching numeric values and mapped in Infrastructure.
- **Infrastructure** — EF Core **InMemory** `DbContext`, repositories under **`Persistence/Repositories/{TeamMember|WorkItem}`**, **entity ↔ DTO** mappers under **`Persistence/Mapping/{TeamMember|WorkItem}`**, **seed data** (`HasData`), and **audit** interceptor.
- **Api** — thin **controllers** (`IMediator`), JSON **enum strings**, **Swagger**, and **exception middleware** mapping domain/application errors to HTTP status codes.

This satisfies **SOLID** in a pragmatic way for a small service: single-purpose handlers, persistence behind interfaces (**D**), and small, focused validators.

## CQRS

**MediatR** separates reads and writes into explicit request types. For this scope, the extra files are justified by clarity and testability. If the project shrinks later, handlers could be folded behind application services without changing HTTP routes.

## Team members API

Team members are a separate aggregate with their own **CQRS** handlers and **`ITeamMemberRepository`**. Work-item create/update validates `assigneeId` through **`ITeamMemberRepository.Exists`**, not the work-item repository, so persistence concerns stay separated.

## Soft delete and audit trail

- **TeamMember** and **WorkItem** use **`DeletedAt` / `DeletedById`** (nullable) plus **`CreatedAt` / `UpdatedAt` / `CreatedById` / `UpdatedById`** on the row. EF **global query filters** hide soft-deleted rows from normal queries. Deletes are **soft** only (no `Remove()` on these entities in repositories).
- **Append-only** table **`AuditLogEntry`** records **Created**, **Updated**, or **Deleted** (including soft delete) with **`PayloadJson`** (simple JSON snapshot). **`AuditSaveChangesInterceptor`** runs on `SaveChanges` and sets **`ActorId`** from the authenticated team member when an HTTP request is in scope (otherwise null). In **Development**, **`GET /api/dev/audit-log-entries`** lists rows for local inspection (not part of the core assessment API).

Stable **seed ids**, **JWT / dev token** usage, and copy-paste examples live in [README.md](README.md) under **Seed data and test IDs**.

- **`AssignerId`** on work items is set on **create** to the **authenticated** team member (not supplied in the request body). **Updates** do not change **assigner**; only **assignee** and other fields are replaced. Deleting a **member** clears **`AssigneeId`** and/or **`AssignerId`** on work items that referenced that member.

## REST and validation

- **PUT** performs a **full replace** of the work item shape (explicit fields, including nullable `status` and `assigneeId`).
- **POST** defaults **status** to **New** when omitted (`WorkItemStatusDefaults`); **priority** defaults to **Low** when omitted (`WorkItemPriorityDefaults`).
- **Assignee** foreign key is validated in handlers: unknown id → **400** (`BadRequestException`), not **404**, because the primary resource is still the work item.

## Search and filters

- `q` matches **title only** (case-insensitive substring), per product decision.
- Filters (`status`, `priority`, `assigneeId`, `createdFrom`, `createdTo`) are combined with **AND**. Items with `status: null` only appear when no `status` filter is applied.

## Error handling

A single **middleware** converts:

- `NotFoundException` → **404** Problem Details  
- `BadRequestException` → **400** Problem Details  
- `ValidationException` → **400** with grouped `errors`  
- anything else → **500** (logged)

**401 Unauthorized** (JWT bearer challenge) uses **`JwtBearer` `OnChallenge`** to return **`application/problem+json`** with a brief generic **`detail`**. **403 Forbidden** (authenticated user, failed authorization) is handled by **`ForbiddenProblemDetailsAuthorizationResultHandler`**, which returns the same shape with a short **`detail`** (no remediation hints in the body).

## EF Core InMemory

Required by the assessment. Trade-offs:

- Data is **not** persisted across process restarts.
- Some behaviors differ from a real relational provider (e.g. `ToLower()` in LINQ is acceptable here for the InMemory provider and simplicity).

## Duplicated enums (Application vs Domain)

Chosen to keep **Application** independent of **Domain** types. The cost is **duplication** and a **mapping** layer in Infrastructure. An alternative is a tiny **shared kernel** project for enums only, or referencing Domain from Application (common in textbook Clean Architecture). The current approach optimizes for “API + use cases do not depend on persistence shapes.”

## What would come next in production

- Real database (SQL Server/Postgres) with migrations.
- Authentication/authorization and tenant scoping.
- Pagination for list endpoints and sorting options.
- Richer domain rules (state transitions) if requirements grow.
