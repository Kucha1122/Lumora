namespace Lumora.Server.Api.Security;

/// <summary>
/// Rejects a request unless the JWT's "roomId" claim matches the {roomId} route value —
/// a token minted for room A must never open room B's clipboard or drive.
/// </summary>
public static class RoomScopeFilterExtensions
{
    public static TBuilder RequireRoomScope<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter(async (context, next) =>
        {
            var routeRoomId = context.HttpContext.GetRouteValue("roomId")?.ToString();
            var tokenRoomId = context.HttpContext.User.FindFirst("roomId")?.Value;

            if (routeRoomId is null || tokenRoomId is null
                || !string.Equals(routeRoomId, tokenRoomId, StringComparison.OrdinalIgnoreCase))
            {
                return Results.Forbid();
            }

            return await next(context);
        });

        return builder;
    }
}
