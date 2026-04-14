solution.md

```md
# Design and trade-offs

## Deliverables

| Deliverable | Location |
|-------------|----------|
| All source code | `src/` and `tests/` |
| Instructions to run locally | [README.md](README.md) |
| Design and trade-offs | This document (`solution.md`) |

Per the brief, a short video walkthrough may be submitted instead of, or in addition to, this document.

## Scope covered

The take-home asked for a task management API using EF Core InMemory with seeded data and support for:

- viewing and searching tasks
- filtering by status, assignee, and priority
- creating, updating, and deleting tasks
- assigning tasks to team members
- setting and updating task priorities

These requirements are satisfied through the `WorkItems` API and the in-memory persistence setup.

## Notes on approach

This assessment could have been implemented successfully as a single Web API project with direct EF Core usage. I chose a slightly more structured approach to demonstrate separation of concerns, validation, and testability while keeping the core scope focused on the brief.

A few additional features were included beyond the minimum requirements, such as team member CRUD, soft delete, and audit logging. These are not required by the brief, but were added to show design thinking and to make the API feel more complete.

## Architecture

The solution uses a pragmatic Clean Architecture split across four projects:

- `TaskManagement.Domain` — entities and enums
- `TaskManagement.Application` — use cases, DTOs, validators, and repository interfaces
- `TaskManagement.Infrastructure` — EF Core InMemory, repositories, mapping, seed data, and audit support
- `TaskManagement.Api` — controllers, authentication, Swagger, and exception handling

Dependency direction is kept one-way so the API and use cases do not depend directly on persistence details.

This structure adds a bit more ceremony than a single-project solution, but it improves:
- separation of concerns
- testability
- clarity of read vs write behavior
- flexibility if the persistence layer changes later

For a small exercise, this is more structure than strictly necessary, but still reasonable for demonstrating engineering practices expected at lead level.

## CQRS

MediatR is used to separate reads and writes into explicit queries and commands.

For this assignment, CQRS helps keep handlers small and focused:
- queries handle retrieval and filtering
- commands handle create, update, and delete flows

The trade-off is more files and indirection. For a much smaller codebase, a simpler application service approach could also have been valid.

## Data model and API design

The core business entity is a work item. A work item supports:
- title
- description
- status
- priority
- assignee
- assigner
- audit fields

Team members are managed separately and can be referenced by `assigneeId` on work items.

Key API decisions:
- `GET /api/work-items` supports searching by `q` and filtering by `status`, `priority`, `assigneeId`, `createdFrom`, and `createdTo`
- filters combine with `AND`
- `q` searches title only using case-insensitive substring matching
- `POST` defaults status to `New` and priority to `Low` when omitted
- `PUT` performs a full replace of the editable work-item fields
- unknown `assigneeId` returns `400 Bad Request` because the request is invalid, rather than `404 Not Found` for the work item itself

## Validation

FluentValidation is used for request validation.

This keeps validation logic out of controllers and helps produce consistent error responses for:
- required fields
- invalid email formats
- malformed requests

Referential validation such as checking whether an assignee exists happens in the application layer through repository interfaces.

## Soft delete and audit

Soft delete was added for both `TeamMember` and `WorkItem`.

Implementation choices:
- rows are marked with `DeletedAt` and `DeletedById`
- EF Core global query filters hide soft-deleted records from normal queries
- delete operations do not physically remove rows

An append-only `AuditLogEntry` store was also added to capture create, update, and delete events with a JSON snapshot payload.

Why include this:
- it demonstrates traceability and mutation history
- it fits naturally with task management systems
- it shows how cross-cutting concerns can be added without bloating controllers

Trade-off:
- these features are beyond the minimum brief and add complexity
- for a simpler submission, they could have been omitted

## Authentication and authorization

Authentication was not explicitly required by the brief, but a lightweight development token flow was added.

In Development:
- a token can be minted through `/api/dev/token`
- the token identifies the acting team member and role

This supports:
- deriving `assignerId` from the authenticated user on create
- populating audit fields such as `createdById`, `updatedById`, and `deletedById`
- restricting delete operations to `Admin`

Trade-off:
- this improves traceability and realism
- it also adds setup and documentation overhead for a take-home exercise

## Error handling

A single middleware layer translates exceptions into consistent HTTP responses.

Current mappings:
- not found errors → `404`
- bad request errors → `400`
- validation failures → `400` with grouped validation messages
- unexpected errors → `500`

For authentication and authorization:
- `401` returns Problem Details for missing or invalid Bearer tokens
- `403` returns Problem Details when the user is authenticated but not allowed to perform the action

This keeps controllers thin and centralizes error formatting.

## EF Core InMemory

The brief required EF Core InMemory, so the solution uses `UseInMemoryDatabase`.

Benefits:
- easy setup
- no external dependencies
- fast startup for review and testing

Trade-offs:
- data is lost on restart
- behavior does not fully match a relational database
- some query behavior that works in-memory may need adjustment when moving to SQL Server or PostgreSQL

For this assignment, the simplicity and speed of setup outweigh those limitations.

## Duplicated enums between layers

Application and Domain use separate enum types with matching values.

Reason for this choice:
- it keeps the Application layer independent from Domain persistence types
- it avoids leaking entity-layer types into request/response contracts

Trade-off:
- it introduces duplication and mapping code

A reasonable alternative would be:
- sharing enums through a small shared kernel project, or
- allowing Application to reference Domain directly

For this exercise, I preferred stronger layer independence over minimizing duplication.

## Testing

Integration tests were added for the core assessment flows:
- listing seeded work items
- searching by `q`
- filtering by `status`, `assigneeId`, and `priority`
- creating, updating, and deleting tasks

I chose integration tests rather than only unit tests because they give better confidence that routing, validation, handlers, persistence, and serialization work together correctly.

Trade-off:
- integration tests are slightly slower and require more setup
- for this API, the broader coverage was worth it

## What I would do next in production

If this were being developed beyond a take-home exercise, the next steps would be:

- move to a real relational database with migrations
- add pagination and sorting for list endpoints
- strengthen authentication and authorization
- add structured logging and observability
- introduce richer domain rules, such as controlled status transitions
- expand test coverage around edge cases and security
- add CI/CD and environment-specific configuration

## Summary

The implementation fully covers the assessment requirements using ASP.NET Core Web API with EF Core InMemory and seeded data.

I chose a slightly more structured design than the minimum required in order to demonstrate:
- separation of concerns
- validation
- testability
- clear API behavior
- reasonable extensibility

Where that added complexity, I have called it out explicitly as a trade-off.