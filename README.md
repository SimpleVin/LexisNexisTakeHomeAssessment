# Task Management API

ASP.NET Core 8 Web API for team task tracking. Uses **EF Core In-Memory** (no external database), **Clean Architecture** (Domain, Application, Infrastructure, Api), and **CQRS** via **MediatR** with **FluentValidation**.

## Take-home assessment checklist

Minimum requirements from the brief are mapped as follows:

| Requirement | Implementation |
|-------------|----------------|
| View and search tasks | `GET /api/work-items`; search via `q` (title substring, case-insensitive) |
| Filter tasks (status, assignee, priority) | Same route: `status`, `assigneeId`, `priority`; optional `createdFrom` / `createdTo` (AND) |
| Create / update / delete tasks | `POST`, `PUT`, `DELETE /api/work-items` |
| Assign tasks to team members | `assigneeId` on create/update (must exist in `/api/team-members`) |
| Set and update task priorities | `priority` on create/update |
| EF Core InMemory | `UseInMemoryDatabase`; no external DB |
| Seed example data on startup | `SeedData` (team members + work items) applied when the database is created |

**Deliverables:** this repo includes **source**, **[README.md](README.md)** (run instructions), and **[solution.md](solution.md)** (design and trade-offs). Optional video walkthrough can replace or supplement `solution.md` per the employer instructions.

Extras beyond the minimum (team members CRUD, soft delete, append-only audit, assigner derived from the authenticated user on create, dev audit viewer) are described in [solution.md](solution.md).

## Tests

```bash
dotnet test TaskManagement.sln
```

Integration tests (`tests/TaskManagement.Api.Tests`) exercise the **assessment** behaviors: list + seed, search `q`, filters (`status`, `assigneeId`, `priority`), and create/update/delete with assignee + priority. They run **sequentially** against an in-memory host (parallelism disabled for shared state).

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Run locally

```bash
dotnet restore
dotnet run --project src/TaskManagement.Api/TaskManagement.Api.csproj
```

If `dotnet build` fails with **file lock** errors on DLLs under `src/TaskManagement.Api/bin`, stop the running API process (or close the debugger) and build again.

- HTTP (Development): see `src/TaskManagement.Api/Properties/launchSettings.json` for the port (default in this repo: **http://localhost:5111**).
- OpenAPI / Swagger: **http://localhost:5111/swagger** (Development only).

## Seed data and test IDs

Use these **stable GUIDs** when calling the API (InMemory database is recreated on startup; seed is always the same).

### JWT identities (Development)

All business routes require **`Authorization: Bearer <token>`**. In **Development**, mint a token:

```http
POST http://localhost:5111/api/dev/token
Content-Type: application/json

{
  "role": "User",
  "teamMemberId": "11111111-1111-1111-1111-111111111104"
}
```

- **`role`**: `"User"` (read/write except delete) or `"Admin"` (includes **DELETE** / soft-delete).
- **`teamMemberId`** (**required**): must be an existing team member id (the JWT subject; no default).

| Role | Example `teamMemberId` | Name (seed) | Notes |
|------|------------------------|-------------|--------|
| `User` | `11111111-1111-1111-1111-111111111104` | Token User (seed) | Or any seeded member id |
| `Admin` | `11111111-1111-1111-1111-111111111105` | Token Admin (seed) | Use for **DELETE** when you need Admin |

**PowerShell — get a token and call the API**

```powershell
$base = "http://localhost:5111"
$body = @{ role = "User"; teamMemberId = "11111111-1111-1111-1111-111111111104" } | ConvertTo-Json
$token = (Invoke-RestMethod -Uri "$base/api/dev/token" -Method Post -Body $body -ContentType "application/json").accessToken
Invoke-RestMethod -Uri "$base/api/work-items" -Headers @{ Authorization = "Bearer $token" }
```

**curl** (bash; the first line uses [jq](https://jqlang.org/) to read `accessToken`)

```bash
TOKEN=$(curl -s -X POST http://localhost:5111/api/dev/token \
  -H "Content-Type: application/json" \
  -d '{"role":"Admin","teamMemberId":"11111111-1111-1111-1111-111111111105"}' | jq -r .accessToken)
curl -s http://localhost:5111/api/work-items -H "Authorization: Bearer $TOKEN"
```

Without **jq**, paste the token from the JSON response of `POST /api/dev/token` into the header manually.

### Seeded team members

| Id | Name | Email |
|----|------|--------|
| `11111111-1111-1111-1111-111111111101` | Alex Rivera | alex@example.com |
| `11111111-1111-1111-1111-111111111102` | Jordan Lee | jordan@example.com |
| `11111111-1111-1111-1111-111111111103` | Sam Patel | sam@example.com |
| `11111111-1111-1111-1111-111111111104` | Token User (seed) | token-user@example.com |
| `11111111-1111-1111-1111-111111111105` | Token Admin (seed) | token-admin@example.com |

### Seeded work items

| Id | Title | Assignee id | Assigner id | Status | Priority |
|----|-------|-------------|-------------|--------|----------|
| `22222222-2222-2222-2222-222222222201` | Draft API specification | `…101` (Alex) | `…102` (Jordan) | InProgress | High |
| `22222222-2222-2222-2222-222222222202` | Seed database | `…102` (Jordan) | — | Todo | Medium |
| `22222222-2222-2222-2222-222222222203` | Write README | — | — | (unset) | Low |

### Try these queries (with a valid Bearer token)

- List all: `GET /api/work-items`
- Search title: `GET /api/work-items?q=draft`
- By status: `GET /api/work-items?status=InProgress`
- By assignee: `GET /api/work-items?assigneeId=11111111-1111-1111-1111-111111111101`
- By priority: `GET /api/work-items?priority=High`
- One item: `GET /api/work-items/22222222-2222-2222-2222-222222222201`

## API overview

Create **team members** first (or use seed ids above), then reference their `id` as **`assigneeId`** on work items. **Assigner** on create is always the authenticated team member (`assignerId` is not sent in the body). **PUT** keeps the existing **assigner** unchanged. Mutations record **`createdById` / `updatedById` / `deletedById`** from the JWT when present.

### Team members — `/api/team-members`

| Method | Route | Description |
|--------|--------|-------------|
| GET | `/api/team-members` | List members. Optional `q`: name substring (case-insensitive). |
| GET | `/api/team-members/{id}` | Get one member; **404** if missing or **soft-deleted**. |
| POST | `/api/team-members` | Create. **201** + `Location`. Body: `name` and `email` (required; valid email format). |
| PUT | `/api/team-members/{id}` | Full replace `name` and `email` (both required). **404** if missing. |
| DELETE | `/api/team-members/{id}` | **Soft delete** (**Admin** only). **200** + JSON envelope (`success`, optional `message`, `errors`). **403** + **Problem Details** if authenticated as **User**. **404** if already deleted or unknown id. Work items that referenced this member as **assignee** or **assigner** get those FKs cleared. |

Responses use an **`ApplicationResult`** envelope for most operations (`success`, `data`, `errors`) or **`ApplicationUnitResult`** for deletes (`success`, `message`, `errors`). Audit-style fields on DTOs include `createdAt`, `updatedAt`, `createdById`, `updatedById`, `deletedAt`, `deletedById` (populated from the authenticated team member where applicable).

### Work items — `/api/work-items`

| Method | Route | Description |
|--------|--------|-------------|
| GET | `/api/work-items` | List tasks. Query: `q` (title substring, case-insensitive), `status`, `priority`, `assigneeId`, `createdFrom`, `createdTo` (ISO 8601, UTC). Filters combine with **AND**. |
| GET | `/api/work-items/{id}` | Get one task; **404** if missing. |
| POST | `/api/work-items` | Create. **201** + `Location`. Body: `title` (required), `description`, `status` (optional; defaults to **New**), `priority` (optional; defaults to **Low**), optional `assigneeId`. **Assigner** is always the authenticated user. **400** if `assigneeId` is not a real member. |
| PUT | `/api/work-items/{id}` | Full replace of body fields; **assigner** on the row is unchanged. **404** if missing. |
| DELETE | `/api/work-items/{id}` | **Soft delete** (**Admin** only). **200** + JSON envelope (`success`, optional `message`, `errors`). **403** + **Problem Details** if authenticated as **User**. **404** if missing or already deleted. |

**Append-only audit log:** each create/update/soft-delete for `TeamMember` and `WorkItem` writes a row to `AuditLogEntries` with a JSON **payload** snapshot and **`actorId`** when a JWT identity is present. In **Development** only, `GET /api/dev/audit-log-entries` returns audit rows (newest first); seed data alone may not produce audit rows until you mutate entities through the API.

Expected failures from handlers return the **application envelope** with an HTTP status (e.g. **404** for `not_found`). **Problem Details** (`application/problem+json`) apply for **401** (missing or invalid Bearer token), **403** (signed in but not allowed), **FluentValidation**, and unhandled exceptions (messages stay short; see Swagger and logs for behavior).

## Example requests

Mint a token first (see [Seed data and test IDs](#seed-data-and-test-ids)), then add the header **`Authorization: Bearer <accessToken>`** to the examples below.

```http
POST http://localhost:5111/api/team-members
Authorization: Bearer <accessToken>
Content-Type: application/json

{
  "name": "Taylor Kim",
  "email": "taylor@example.com"
}
```

```http
GET http://localhost:5111/api/work-items?q=draft&status=InProgress
Authorization: Bearer <accessToken>
```

```http
POST http://localhost:5111/api/work-items
Authorization: Bearer <accessToken>
Content-Type: application/json

{
  "title": "Review pull request",
  "description": "Focus on error handling",
  "priority": "Medium",
  "assigneeId": "11111111-1111-1111-1111-111111111101"
}
```

## Solution layout

- `src/TaskManagement.Domain` — persistence entities and enums  
- `src/TaskManagement.Application` — `UseCases/` (MediatR), `Common/Contracts` (DTOs, requests), `Common/Models/Interface/<area>/` (repository ports), area folders for supporting models; **no reference to Domain**  
- `src/TaskManagement.Infrastructure` — EF Core InMemory, `Persistence/Repositories/{TeamMember|WorkItem}`, `Persistence/Mapping/{TeamMember|WorkItem}` (entity ↔ DTO), seed, audit interceptor  
- `src/TaskManagement.Api` — HTTP host, controllers, exception middleware  
- `tests/TaskManagement.Api.Tests` — integration tests for assessment behaviors  

See [solution.md](solution.md) for design notes and trade-offs.
