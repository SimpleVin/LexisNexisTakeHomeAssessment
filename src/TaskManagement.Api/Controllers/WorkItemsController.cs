using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.Authorization;
using TaskManagement.Api.Extensions;
using TaskManagement.Application.UseCases.WorkItem.CreateWorkItem;
using TaskManagement.Application.UseCases.WorkItem.DeleteWorkItem;
using TaskManagement.Application.UseCases.WorkItem.GetWorkItemById;
using TaskManagement.Application.UseCases.WorkItem.ListWorkItems;
using TaskManagement.Application.Common.Contracts.Requests.WorkItem;
using TaskManagement.Application.UseCases.WorkItem.UpdateWorkItem;
using TaskManagement.Application.Common.Contracts.DTO.WorkItem;
using TaskManagement.Application.Common.Contracts.Pagination;
using TaskManagement.Application.Common.Contracts.Results;
using TaskManagement.Application.WorkItem.Models;

namespace TaskManagement.Api.Controllers;

/// <summary>
/// Work items (tasks): list with search and filters, CRUD, assignee/assigner, and priority.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = "Work items")]
[Route("api/work-items")]
public sealed class WorkItemsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// List work items. Combine filters with AND. Soft-deleted items are omitted.
    /// </summary>
    /// <param name="q">Search: case-insensitive substring match on title only.</param>
    /// <param name="status">Filter by status (omit for any status, including unset).</param>
    /// <param name="priority">Filter by priority.</param>
    /// <param name="assigneeId">Filter by assignee team member id.</param>
    /// <param name="createdFrom">Inclusive lower bound on CreatedAt (UTC, ISO 8601).</param>
    /// <param name="createdTo">Inclusive upper bound on CreatedAt (UTC, ISO 8601).</param>
    /// <param name="page">1-based page index (default 1).</param>
    /// <param name="pageSize">Page size (default 20, max 100).</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApplicationResult<PagedResult<WorkItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApplicationResult<PagedResult<WorkItemDto>>>> SearchWorkItems(
        [FromQuery] string? q,
        [FromQuery] WorkItemStatus? status,
        [FromQuery] WorkItemPriority? priority,
        [FromQuery] Guid? assigneeId,
        [FromQuery] DateTimeOffset? createdFrom,
        [FromQuery] DateTimeOffset? createdTo,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetWorkItemsQuery(q, status, priority, assigneeId, createdFrom, createdTo, page, pageSize),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get a single work item by id. Returns 404 if not found or soft-deleted.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApplicationResult<WorkItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApplicationResult<WorkItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationResult<WorkItemDto>>> GetWorkItemById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWorkItemByIdQuery(id), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Create a work item. Optional assignee must reference an existing team member when provided.
    /// Assigner is always the authenticated user. Priority defaults to Low when omitted; status defaults to New when omitted.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApplicationResult<WorkItemDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApplicationResult<WorkItemDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApplicationResult<WorkItemDto>>> CreateWorkItem(
        [FromBody] CreateWorkItemRequest body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateWorkItemCommand(body.Title, body.Description, body.Status, body.Priority, body.AssigneeId),
            cancellationToken);
        return result.ToCreatedAtActionResult(nameof(GetWorkItemById), dto => new { id = dto.Id });
    }

    /// <summary>
    /// Replace a work item (full body). Returns 404 if not found.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApplicationResult<WorkItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApplicationResult<WorkItemDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationResult<WorkItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationResult<WorkItemDto>>> UpdateWorkItem(
        Guid id,
        [FromBody] UpdateWorkItemRequest body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateWorkItemCommand(
                id,
                body.Title,
                body.Description,
                body.Status,
                body.Priority,
                body.AssigneeId),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Soft-delete a work item (hidden from lists; 404 on subsequent get). Returns a JSON envelope with success or errors.
    /// Requires role Admin.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApplicationUnitResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApplicationUnitResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWorkItemById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteWorkItemCommand(id), cancellationToken);
        return result.ToActionResult();
    }
}
