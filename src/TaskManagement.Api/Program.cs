using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TaskManagement.Api.Authentication;
using TaskManagement.Api.Authorization;
using TaskManagement.Api.Middleware;
using TaskManagement.Api.Swagger;
using TaskManagement.Application;
using TaskManagement.Application.Common.Constants.Identity;
using TaskManagement.Application.Common.Models.Interface.Identity;
using TaskManagement.Application.Common.Models.Interface.TeamMember;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<BearerTokenIssuer>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentIdentity, HttpContextCurrentIdentity>();

var jwtForBearer = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException($"Configuration section '{JwtOptions.SectionName}' is missing or invalid.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtForBearer.SigningKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtForBearer.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtForBearer.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await UnauthorizedProblemDetailsWriter.WriteAsync(
                    context.Response,
                    context.HttpContext.RequestAborted);
            },
            OnTokenValidated = context =>
            {
                var raw = context.Principal?.FindFirst(IdentityClaimTypes.TeamMemberId)?.Value
                    ?? context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                if (!Guid.TryParse(raw, out _))
                {
                    context.Fail("Invalid token.");
                }

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy => policy.RequireRole("Admin"));
});

builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ForbiddenProblemDetailsAuthorizationResultHandler>();

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields =
        HttpLoggingFields.RequestMethod
        | HttpLoggingFields.RequestPath
        | HttpLoggingFields.ResponseStatusCode
        | HttpLoggingFields.Duration;
    options.RequestHeaders.Clear();
    options.ResponseHeaders.Clear();
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Task Management API",
        Version = "v1",
        Description =
            "Team task tracking API. Uses EF Core In-Memory. "
            + "Create team members first if you need assignee IDs for work items. "
            + "Authenticate with a Bearer JWT (see /api/dev/token in Development; **teamMemberId** is required in the body). "
            + "Each token identifies a team member (`team_member_id` claim) used as created/updated/deleted-by, assigner on work-item **create**, and audit actor. "
            + "Seed includes Token User (…104) and Token Admin (…105) for dev tokens. "
            + "Role **User** can read and mutate data; **Admin** is required for DELETE (soft-delete). **401** / **403** responses use short **Problem Details** (`application/problem+json`) for authentication and authorization failures.",
    });

    options.DocInclusionPredicate((documentName, _) => documentName == "v1");

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    options.TagActionsBy(api =>
    {
        if (!string.IsNullOrEmpty(api.GroupName))
        {
            return new[] { api.GroupName };
        }

        return api.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller) &&
               !string.IsNullOrEmpty(controller)
            ? new[] { controller }
            : new[] { "Api" };
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Bearer token (includes role and team_member_id). In Development, obtain one from POST /api/dev/token.",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });

    options.DocumentFilter<AnonymousSwaggerRoutesDocumentFilter>();
});

var app = builder.Build();

app.Services.EnsureTaskManagementDatabaseCreated();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapPost("/api/dev/token", async (
                DevTokenRequest body,
                BearerTokenIssuer issuer,
                ITeamMemberRepository teamMembers,
                CancellationToken cancellationToken) =>
            {
                if (body.Role is not ("Admin" or "User"))
                {
                    return Results.BadRequest(new { error = "Role must be Admin or User." });
                }

                if (body.TeamMemberId == Guid.Empty)
                {
                    return Results.BadRequest(new { error = "teamMemberId is required." });
                }

                var teamMemberId = body.TeamMemberId;
                if (!await teamMembers.ExistsTeamMemberById(teamMemberId, cancellationToken))
                {
                    return Results.BadRequest(new { error = "teamMemberId must reference an existing team member." });
                }

                var accessToken = issuer.CreateToken(body.Role, teamMemberId);
                return Results.Ok(new DevTokenResponse(accessToken, 3600));
            })
        .AllowAnonymous()
        .WithTags("Development");

    app.MapGet("/api/dev/audit-log-entries", async (AppDbContext db, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        {
            var log = loggerFactory.CreateLogger("TaskManagement.Api.Development");
            try
            {
                var rows = await db.AuditLogEntries.AsNoTracking()
                    .OrderByDescending(x => x.OccurredAt)
                    .Select(x => new
                    {
                        x.Id,
                        EntityType = x.EntityType.ToString(),
                        x.EntityId,
                        Action = x.Action.ToString(),
                        x.OccurredAt,
                        x.ActorId,
                        x.PayloadJson,
                    })
                    .ToListAsync(cancellationToken);
                return Results.Ok(rows);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to load audit log entries.");
                return Results.Problem(
                    detail: "Failed to load audit log entries.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .AllowAnonymous()
        .WithTags("Development");
}

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
    .AllowAnonymous();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

internal sealed record DevTokenRequest(string Role, Guid TeamMemberId);

internal sealed record DevTokenResponse(string AccessToken, int ExpiresInSeconds);

public partial class Program;
