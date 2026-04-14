namespace TaskManagement.Application.Common.Constants.Identity;

/// <summary>JWT / claims-principal claim types for the team member id (wired to <c>ICurrentIdentity</c> in the API host).</summary>
public static class IdentityClaimTypes
{
    public const string TeamMemberId = "team_member_id";
}
