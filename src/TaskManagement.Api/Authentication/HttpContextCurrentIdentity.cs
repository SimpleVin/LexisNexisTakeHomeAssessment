using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TaskManagement.Application.Common.Constants.Identity;
using TaskManagement.Application.Common.Models.Interface.Identity;

namespace TaskManagement.Api.Authentication;

public sealed class HttpContextCurrentIdentity(IHttpContextAccessor httpContextAccessor) : ICurrentIdentity
{
    public Guid? TeamMemberId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var raw = principal.FindFirst(IdentityClaimTypes.TeamMemberId)?.Value
                ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }
}
