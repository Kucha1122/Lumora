using Lumora.Server.Infrastructure.Updates;
using Microsoft.Extensions.Options;

namespace Lumora.Server.Api.Security;

/// <summary>
/// Gates POST /updates/android* with a shared secret header instead of the room JWT scheme —
/// there is no room, no user, and no JoinRoom handshake in this flow, only a CI pipeline
/// pushing a build. Deliberately bypasses [Authorize]/RequireAuthorization() entirely.
/// </summary>
public static class UpdatePublishSecretFilterExtensions
{
    private const string HeaderName = "X-Update-Publish-Secret";

    public static TBuilder RequireUpdatePublishSecret<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter(async (context, next) =>
        {
            var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<UpdatePublishOptions>>();
            var provided = context.HttpContext.Request.Headers[HeaderName].ToString();

            if (string.IsNullOrEmpty(provided) ||
                !CryptographicallyEqual(provided, options.Value.Secret))
            {
                return Results.Unauthorized();
            }

            return await next(context);
        });

        return builder;
    }

    private static bool CryptographicallyEqual(string a, string b)
    {
        var bytesA = System.Text.Encoding.UTF8.GetBytes(a);
        var bytesB = System.Text.Encoding.UTF8.GetBytes(b);

        // FixedTimeEquals throws on mismatched lengths rather than returning false, and a
        // wrong-length secret must fail the same way a wrong-content one does.
        return bytesA.Length == bytesB.Length &&
            System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }
}
