using System.Net;
using Lumora.Contracts.Updates;
using Lumora.Server.Api.Security;
using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Updates;
using Lumora.Server.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Lumora.Server.Api.Endpoints;

public static class UpdateEndpoints
{
    /// <summary>Same ceiling as Drive blobs (DriveEndpoints.MaxBlobSizeBytes) — an APK is far
    /// smaller in practice, this just reuses the existing streamed-upload limit.</summary>
    private const long MaxApkSizeBytes = 512L * 1024 * 1024;

    public static void MapUpdateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/updates/android").WithTags("Updates");

        // Public — no auth. A phone checking for updates hasn't joined any room yet.
        group.MapGet("/latest", GetLatest);
        group.MapGet("/latest/apk", DownloadLatestApk);
        group.MapGet("/install", InstallPage);

        // CI-only — shared secret, not a room JWT. See UpdatePublishSecretFilter.
        group.MapPost("/blobs", UploadApkBlob).DisableAntiforgery().RequireUpdatePublishSecret();
        group.MapPost("/", Publish).RequireUpdatePublishSecret();
    }

    private static async Task<Results<Ok<AndroidReleaseDto>, NotFound>> GetLatest(ISender sender, CancellationToken ct)
    {
        var release = await sender.Send(new GetLatestAndroidReleaseQuery(), ct);
        return release is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ToDto(release));
    }

    private static async Task<Results<FileStreamHttpResult, NotFound>> DownloadLatestApk(
        ISender sender, IBlobStore blobStore, CancellationToken ct)
    {
        var release = await sender.Send(new GetLatestAndroidReleaseQuery(), ct);
        if (release is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            var stream = await blobStore.OpenReadAsync(UpdateBlobs.Namespace, BlobId.From(release.BlobId), ct);
            return TypedResults.Stream(stream, "application/vnd.android.package-archive", $"lumora-{release.Version}.apk");
        }
        catch (FileNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Ok<Guid>, BadRequest<string>>> UploadApkBlob(
        HttpRequest request, IBlobStore blobStore, CancellationToken ct)
    {
        if (request.ContentLength is null || request.ContentLength > MaxApkSizeBytes)
        {
            return TypedResults.BadRequest($"APK przekracza maksymalny rozmiar {MaxApkSizeBytes} B.");
        }

        var blobId = BlobId.New();
        await blobStore.SaveAsync(UpdateBlobs.Namespace, blobId, request.Body, ct);
        return TypedResults.Ok(blobId.Value);
    }

    private static async Task<Results<Ok<PublishAndroidReleaseRequest>, ValidationProblem>> Publish(
        PublishAndroidReleaseRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(
            new PublishAndroidReleaseCommand(request.Version, request.VersionCode, request.BlobId, request.SizeBytes, request.ReleaseNotes),
            ct);

        if (!result.IsSuccess)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [result.Error!] });
        }

        return TypedResults.Ok(request);
    }

    /// <summary>
    /// A page a person opens on their PC to scan a QR code with their phone for first
    /// install (plan §Etap 2). The QR is rendered client-side by the phone's browser via
    /// api.qrserver.com, pointed at this same server's /latest/apk — no server-side QR
    /// encoding dependency, and no secret ever appears in the URL.
    /// </summary>
    private static async Task<ContentHttpResult> InstallPage(HttpRequest request, ISender sender, CancellationToken ct)
    {
        var release = await sender.Send(new GetLatestAndroidReleaseQuery(), ct);

        var apkUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/updates/android/latest/apk";
        var encodedApkUrl = WebUtility.UrlEncode(apkUrl);

        var body = release is null
            ? "<p>Nie opublikowano jeszcze żadnej wersji Androida.</p>"
            : $"""
              <p>Najnowsza wersja: <strong>{WebUtility.HtmlEncode(release.Version)}</strong> (build {release.VersionCode})</p>
              <img src="https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={encodedApkUrl}" alt="QR do pobrania APK" />
              <p><a href="{WebUtility.HtmlEncode(apkUrl)}">{WebUtility.HtmlEncode(apkUrl)}</a></p>
              """;

        const string shell = """
                              <!DOCTYPE html>
                              <html lang="pl">
                              <head>
                                <meta charset="utf-8" />
                                <title>Lumora — instalacja na Androidzie</title>
                                <meta name="viewport" content="width=device-width, initial-scale=1" />
                                <style>
                                  body { font-family: sans-serif; background: #1B1F3B; color: #EDEFF7; text-align: center; padding: 2rem; }
                                  img { background: #fff; padding: 8px; border-radius: 8px; }
                                  a { color: #F4B942; }
                                </style>
                              </head>
                              <body>
                                <h1>Lumora na Androida</h1>
                                <p>Zeskanuj kod telefonem, żeby pobrać i zainstalować APK. Włącz „Instaluj z nieznanych źródeł” dla przeglądarki, jeśli system o to zapyta.</p>
                                __BODY__
                              </body>
                              </html>
                              """;

        return TypedResults.Text(shell.Replace("__BODY__", body), "text/html");
    }

    private static AndroidReleaseDto ToDto(GetLatestAndroidReleaseResult r) =>
        new(r.Version, r.VersionCode, r.SizeBytes, r.ReleaseNotes, r.PublishedAt);
}
