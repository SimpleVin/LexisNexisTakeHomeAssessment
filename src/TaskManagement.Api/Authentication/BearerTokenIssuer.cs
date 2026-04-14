using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskManagement.Application.Common.Constants.Identity;

namespace TaskManagement.Api.Authentication;

public sealed class BearerTokenIssuer(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    public string CreateToken(string role, Guid teamMemberId, TimeSpan? lifetime = null)
    {
        lifetime ??= TimeSpan.FromHours(1);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var member = teamMemberId.ToString();
        var claims = new Claim[]
        {
            new(JwtRegisteredClaimNames.Sub, member),
            new(IdentityClaimTypes.TeamMemberId, member),
            new(ClaimTypes.Role, role),
        };
        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            expires: DateTime.UtcNow.Add(lifetime.Value),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
