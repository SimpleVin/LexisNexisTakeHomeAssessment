using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.Authorization;
using TaskManagement.Api.Extensions;
using TaskManagement.Application.Common.Contracts.Requests.TeamMember;
using TaskManagement.Application.UseCases.TeamMember.CreateTeamMember;
using TaskManagement.Application.UseCases.TeamMember.DeleteTeamMember;
using TaskManagement.Application.UseCases.TeamMember.GetTeamMemberById;
using TaskManagement.Application.UseCases.TeamMember.ListTeamMembers;
using TaskManagement.Application.UseCases.TeamMember.UpdateTeamMember;
using TaskManagement.Application.Common.Contracts.DTO.TeamMember;
using TaskManagement.Application.Common.Contracts.Pagination;
using TaskManagement.Application.Common.Contracts.Results;

namespace TaskManagement.Api.Controllers;

/// <summary>
/// Team members: people who can be assignees or assigners on work items.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = "Team members")]
[Route("api/team-members")]
public sealed class TeamMembersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// List active (non-deleted) team members, optionally filtered by name.
    /// </summary>
    /// <param name="q">Optional case-insensitive substring match on name.</param>
    /// <param name="page">1-based page index (default 1).</param>
    /// <param name="pageSize">Page size (default 20, max 100).</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApplicationResult<PagedResult<TeamMemberDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApplicationResult<PagedResult<TeamMemberDto>>>> SearchTeamMembers(
        [FromQuery] string? q,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTeamMembersQuery(q, page, pageSize), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get one team member by id. Returns 404 if not found or soft-deleted.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApplicationResult<TeamMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApplicationResult<TeamMemberDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationResult<TeamMemberDto>>> GetTeamMemberById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTeamMemberByIdQuery(id), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Create a team member. Email is required and must be a valid address.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApplicationResult<TeamMemberDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApplicationResult<TeamMemberDto>>> CreateTeamMember([FromBody] CreateTeamMemberRequest body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateTeamMemberCommand(body.Name, body.Email), cancellationToken);
        return result.ToCreatedAtActionResult(nameof(GetTeamMemberById), dto => new { id = dto.Id });
    }

    /// <summary>
    /// Replace name and email for a team member.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApplicationResult<TeamMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationResult<TeamMemberDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationResult<TeamMemberDto>>> UpdateTeamMember(Guid id, [FromBody] UpdateTeamMemberRequest body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateTeamMemberCommand(id, body.Name, body.Email), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Soft-delete a team member. Clears assignee/assigner references on work items that pointed at this member.
    /// Returns a JSON envelope with success or errors. Requires role Admin.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApplicationUnitResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApplicationUnitResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTeamMemberById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTeamMemberCommand(id), cancellationToken);
        return result.ToActionResult();
    }
}
