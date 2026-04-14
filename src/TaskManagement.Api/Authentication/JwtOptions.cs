using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Api.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "";

    [Required]
    public string Audience { get; set; } = "";

    [Required]
    [MinLength(32)]
    public string SigningKey { get; set; } = "";
}
