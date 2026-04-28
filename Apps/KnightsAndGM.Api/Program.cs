using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using KnightsAndGM.Api;
using KnightsAndGM.Api.Infrastructure;
using KnightsAndGM.Api.Realtime;
using KnightsAndGM.Api.Seed;
using KnightsAndGM.Api.Security;
using KnightsAndGM.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<EncounterService>();
builder.Services.AddSingleton<SeedCampaignFactory>();
builder.Services.AddSingleton<CampaignApplicationService>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var connectionString = builder.Configuration["CampaignStorage:ConnectionString"];
if (string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddSingleton<IGameRepository, InMemoryGameRepository>();
}
else
{
    builder.Services.AddSingleton<IGameRepository>(serviceProvider =>
        new PostgresGameRepository(connectionString, serviceProvider.GetRequiredService<SeedCampaignFactory>()));
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/campaign"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials().SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/register", async (RegisterRequest request, CampaignApplicationService service, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Username))
    {
        return Results.BadRequest(new { error = "Username is required." });
    }

    try
    {
        return Results.Ok(await service.RegisterAsync(request, cancellationToken));
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

app.MapPost("/auth/login", async (LoginRequest request, CampaignApplicationService service, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Username))
    {
        return Results.BadRequest(new { error = "Username is required." });
    }

    try
    {
        return Results.Ok(await service.LoginAsync(request, cancellationToken));
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

app.MapGet("/campaigns/{campaignId:guid}/map", async (Guid campaignId, ClaimsPrincipal user, CampaignApplicationService service, CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.GetMapAsync(campaignId, CampaignApplicationService.GetUserId(user), cancellationToken));
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
}).RequireAuthorization();

app.MapGet("/campaigns/{campaignId:guid}/parties/{partyId:guid}", async (Guid campaignId, Guid partyId, ClaimsPrincipal user, CampaignApplicationService service, CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.GetPartyAsync(campaignId, partyId, CampaignApplicationService.GetUserId(user), cancellationToken));
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
}).RequireAuthorization();

app.MapGet("/campaigns/{campaignId:guid}/characters", async (Guid campaignId, ClaimsPrincipal user, CampaignApplicationService service, CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.GetCharactersAsync(campaignId, CampaignApplicationService.GetUserId(user), cancellationToken));
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
}).RequireAuthorization();

app.MapPost("/campaigns/{campaignId:guid}/characters", async (Guid campaignId, SaveCharacterRequest request, ClaimsPrincipal user, CampaignApplicationService service, CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.CreateCharacterAsync(campaignId, CampaignApplicationService.GetUserId(user), request, cancellationToken));
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
}).RequireAuthorization();

app.MapPut("/campaigns/{campaignId:guid}/characters/{characterId:guid}", async (Guid campaignId, Guid characterId, SaveCharacterRequest request, ClaimsPrincipal user, CampaignApplicationService service, CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.UpdateCharacterAsync(campaignId, characterId, CampaignApplicationService.GetUserId(user), request, cancellationToken));
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
}).RequireAuthorization();

app.MapPost("/campaigns/{campaignId:guid}/parties/{partyId:guid}/move", async (Guid campaignId, Guid partyId, MovePartyRequest request, ClaimsPrincipal user, CampaignApplicationService service, IHubContext<CampaignHub> hubContext, CancellationToken cancellationToken) =>
{
    try
    {
        var response = await service.MovePartyAsync(campaignId, partyId, CampaignApplicationService.GetUserId(user), request, cancellationToken);
        await hubContext.Clients.Group(campaignId.ToString()).SendAsync("PartyMoved", response, cancellationToken);
        if (response.NewlyDiscoveredHexId.HasValue)
        {
            await hubContext.Clients.Group(campaignId.ToString()).SendAsync("HexDiscovered", new
            {
                campaignId,
                hexId = response.NewlyDiscoveredHexId.Value,
                partyId
            }, cancellationToken);
        }

        return Results.Ok(response);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
}).RequireAuthorization();

app.MapPost("/campaigns/{campaignId:guid}/hexes/{hexId:guid}/notes", async (Guid campaignId, Guid hexId, CreateNoteRequest request, ClaimsPrincipal user, CampaignApplicationService service, IHubContext<CampaignHub> hubContext, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Body))
    {
        return Results.BadRequest(new { error = "Note body is required." });
    }

    try
    {
        var note = await service.CreateNoteAsync(campaignId, hexId, CampaignApplicationService.GetUserId(user), request.Body, cancellationToken);
        await hubContext.Clients.Group(campaignId.ToString()).SendAsync("NoteCreated", note, cancellationToken);
        return Results.Ok(note);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
}).RequireAuthorization();

app.MapGet("/campaigns/{campaignId:guid}/gm/encounters", async (Guid campaignId, ClaimsPrincipal user, CampaignApplicationService service, CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.GetEncountersAsync(campaignId, CampaignApplicationService.GetUserId(user), cancellationToken));
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
}).RequireAuthorization();

app.MapHub<CampaignHub>("/hubs/campaign");

app.Run();

public partial class Program;
