using Lumora.Server.Api.Endpoints;
using Lumora.Server.Application;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Domain.Rooms;
using Lumora.Server.Domain.ValueObjects;
using Lumora.Server.Infrastructure;
using Lumora.Server.Infrastructure.Auth;
using Lumora.Server.Infrastructure.Persistence;
using Lumora.Server.Infrastructure.Realtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSignalR();

var pepper = builder.Configuration["RoomAuth:Pepper"]
    ?? throw new InvalidOperationException("RoomAuth:Pepper musi być ustawiony (Kubernetes Secret w produkcji).");
var signingKeyBytes = new RoomAuth(Microsoft.Extensions.Options.Options.Create(new RoomAuthOptions { Pepper = pepper })).SigningKeyBytes();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // SignalR sends the token via query string, not the Authorization header.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments(ClipboardHub.Route))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Applies pending migrations automatically only in dev — production runs migrations
    // as an explicit deploy step, not on every pod start.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LumoraDbContext>();
    await db.Database.MigrateAsync();
}

// The public room must exist without any admin action — "domyślnie ma być dostępna
// przestrzeń publiczna" (plan §Model bezpieczeństwa). Idempotent: runs on every startup.
using (var seedScope = app.Services.CreateScope())
{
    var rooms = seedScope.ServiceProvider.GetRequiredService<IRoomRepository>();
    var unitOfWork = seedScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    var publicSlug = RoomSlug.Create("public").Value!;

    if (!await rooms.SlugExistsAsync(publicSlug, CancellationToken.None))
    {
        var room = Room.CreatePublic(publicSlug, "Publiczna", DateTimeOffset.UtcNow).Value!;
        rooms.Add(room);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapRoomEndpoints();
app.MapClipboardEndpoints();
app.MapDriveEndpoints();
app.MapHub<ClipboardHub>(ClipboardHub.Route);

app.Run();

public partial class Program;
