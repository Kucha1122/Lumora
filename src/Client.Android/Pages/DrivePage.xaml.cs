using System.Windows.Input;
using Lumora.Client.Core.Crypto;
using Lumora.Client.Core.Drive;
using Lumora.Client.Core.Rooms;
using Lumora.Client.Core.Transport;
using Lumora.Contracts.Drive;

namespace Lumora.Client.Android.Pages;

public sealed record DriveRow(string Description, ICommand DownloadCommand, ICommand DeleteCommand);

public partial class DrivePage : ContentPage
{
    private readonly LumoraApiClient api;
    private readonly ActiveRoomStore activeRoom;
    private readonly IDeviceIdentity deviceIdentity;

    public DrivePage(LumoraApiClient api, ActiveRoomStore activeRoom, IDeviceIdentity deviceIdentity)
    {
        InitializeComponent();
        this.api = api;
        this.activeRoom = activeRoom;
        this.deviceIdentity = deviceIdentity;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var room = activeRoom.ActiveRoom;
        if (room is null)
        {
            RoomLabel.Text = "Brak aktywnej przestrzeni";
            return;
        }

        RoomLabel.Text = $"Przestrzeń: {room.DisplayName}{(room.IsPrivate ? " 🔒" : "")}";

        try
        {
            var files = await api.ListDriveFilesAsync(room.RoomId, CancellationToken.None);
            FilesList.ItemsSource = files.Select(f => new DriveRow(
                Description: Describe(f, room),
                DownloadCommand: new Command(async () => await DownloadAsync(f, room)),
                DeleteCommand: new Command(async () => await DeleteAsync(f, room))
            )).ToList();
        }
        catch (Exception ex)
        {
            await DisplayAlert(null, $"Nie udało się pobrać listy plików: {ex.Message}", "OK");
        }
    }

    private static string Describe(DriveFileDto file, RoomProfile room)
    {
        try
        {
            var metadata = DriveFileMetadata.DecryptFrom(file.EncryptedMetadata, room.IsPrivate ? room.EncKey : null);
            return $"{metadata.FileName} ({file.SizeBytes} B)";
        }
        catch
        {
            return $"(nie udało się odszyfrować metadanych, {file.SizeBytes} B)";
        }
    }

    private async void OnUploadClicked(object? sender, EventArgs e)
    {
        var room = activeRoom.ActiveRoom;
        if (room is null)
        {
            return;
        }

        try
        {
            var picked = await FilePicker.Default.PickAsync();
            if (picked is null)
            {
                return;
            }

            await using var stream = await picked.OpenReadAsync();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            var plaintext = buffer.ToArray();

            var content = room.IsPrivate ? PayloadCipher.Encrypt(room.EncKey!, plaintext) : plaintext;

            using var uploadStream = new MemoryStream(content);
            var blobId = await api.UploadBlobAsync(room.RoomId, uploadStream, CancellationToken.None);

            var metadata = new DriveFileMetadata(picked.FileName, "application/octet-stream")
                .EncryptFor(room.IsPrivate ? room.EncKey : null);

            await api.RegisterDriveFileAsync(
                room.RoomId,
                new RegisterDriveFileRequest(blobId, metadata, content.Length, deviceIdentity.Id),
                CancellationToken.None);

            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert(null, $"Wysyłanie nie powiodło się: {ex.Message}", "OK");
        }
    }

    private async Task DownloadAsync(DriveFileDto file, RoomProfile room)
    {
        try
        {
            var metadata = DriveFileMetadata.DecryptFrom(file.EncryptedMetadata, room.IsPrivate ? room.EncKey : null);

            using var downloadStream = await api.DownloadBlobAsync(room.RoomId, file.BlobId, CancellationToken.None);
            using var buffer = new MemoryStream();
            await downloadStream.CopyToAsync(buffer);
            var content = buffer.ToArray();

            var plaintext = room.IsPrivate ? PayloadCipher.Decrypt(room.EncKey!, content) : content;

            // App-scoped external storage (Android/data/pl.lumora.client/files/Download) — no
            // runtime permission needed, visible in most file managers. A public Downloads/
            // folder via MediaStore is left for a later pass if this proves inconvenient.
            var dir = global::Android.App.Application.Context.GetExternalFilesDir("Download")!.AbsolutePath;
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, metadata.FileName);
            await File.WriteAllBytesAsync(path, plaintext);

            await DisplayAlert(null, $"Zapisano: {path}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(null, $"Pobieranie nie powiodło się: {ex.Message}", "OK");
        }
    }

    private async Task DeleteAsync(DriveFileDto file, RoomProfile room)
    {
        try
        {
            await api.DeleteDriveFileAsync(room.RoomId, file.Id, CancellationToken.None);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert(null, $"Usuwanie nie powiodło się: {ex.Message}", "OK");
        }
    }
}
