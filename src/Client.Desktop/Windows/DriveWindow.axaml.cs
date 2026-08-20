using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Lumora.Client.Core.Drive;
using Lumora.Client.Core.Rooms;
using Lumora.Client.Core.Transport;
using Lumora.Contracts.Drive;

namespace Lumora.Client.Desktop.Windows;

public partial class DriveWindow : Window
{
    private readonly LumoraApiClient api;
    private readonly ActiveRoomStore activeRoom;
    private readonly Guid deviceId;
    private readonly List<DriveFileDto> loadedFiles = [];

    public DriveWindow(LumoraApiClient api, ActiveRoomStore activeRoom, Guid deviceId)
    {
        InitializeComponent();
        Icon = TrayIconFactory.BrandIcon;
        this.api = api;
        this.activeRoom = activeRoom;
        this.deviceId = deviceId;
        Opened += async (_, _) =>
        {
            try
            {
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                // An unhandled exception here (e.g. server unreachable) would otherwise
                // crash the whole tray app — this is an async void event handler.
                RoomLabel.Text = $"Nie udało się pobrać danych: {ex.Message}";
            }
        };
    }

    private async Task ReloadAsync()
    {
        var room = activeRoom.ActiveRoom;
        if (room is null)
        {
            return;
        }

        RoomLabel.Text = $"Przestrzeń: {room.DisplayName}{(room.IsPrivate ? " 🔒" : "")}";

        var files = await api.ListDriveFilesAsync(room.RoomId, CancellationToken.None);
        loadedFiles.Clear();
        loadedFiles.AddRange(files);

        FilesList.ItemsSource = files.Select(f => Describe(f, room)).ToList();
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

    private async void OnUploadClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var room = activeRoom.ActiveRoom;
            if (room is null)
            {
                return;
            }

            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false });
            if (files.Count == 0)
            {
                return;
            }

            var file = files[0];
            await using var stream = await file.OpenReadAsync();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            var plaintext = buffer.ToArray();

            var content = room.IsPrivate
                ? Client.Core.Crypto.PayloadCipher.Encrypt(room.EncKey!, plaintext)
                : plaintext;

            using var uploadStream = new MemoryStream(content);
            var blobId = await api.UploadBlobAsync(room.RoomId, uploadStream, CancellationToken.None);

            var metadata = new DriveFileMetadata(file.Name, "application/octet-stream")
                .EncryptFor(room.IsPrivate ? room.EncKey : null);

            await api.RegisterDriveFileAsync(
                room.RoomId,
                new RegisterDriveFileRequest(blobId, metadata, content.Length, deviceId),
                CancellationToken.None);

            await ReloadAsync();
        }
        catch (Exception ex)
        {
            RoomLabel.Text = $"Wysyłanie nie powiodło się: {ex.Message}";
        }
    }

    private async void OnDownloadClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var room = activeRoom.ActiveRoom;
            if (room is null || FilesList.SelectedIndex < 0 || FilesList.SelectedIndex >= loadedFiles.Count)
            {
                return;
            }

            var file = loadedFiles[FilesList.SelectedIndex];
            var metadata = DriveFileMetadata.DecryptFrom(file.EncryptedMetadata, room.IsPrivate ? room.EncKey : null);

            var target = await StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions { SuggestedFileName = metadata.FileName });
            if (target is null)
            {
                return;
            }

            using var downloadStream = await api.DownloadBlobAsync(room.RoomId, file.BlobId, CancellationToken.None);
            using var buffer = new MemoryStream();
            await downloadStream.CopyToAsync(buffer);
            var content = buffer.ToArray();

            var plaintext = room.IsPrivate ? Client.Core.Crypto.PayloadCipher.Decrypt(room.EncKey!, content) : content;

            await using var outStream = await target.OpenWriteAsync();
            await outStream.WriteAsync(plaintext);
        }
        catch (Exception ex)
        {
            RoomLabel.Text = $"Pobieranie nie powiodło się: {ex.Message}";
        }
    }

    private async void OnDeleteClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var room = activeRoom.ActiveRoom;
            if (room is null || FilesList.SelectedIndex < 0 || FilesList.SelectedIndex >= loadedFiles.Count)
            {
                return;
            }

            var file = loadedFiles[FilesList.SelectedIndex];
            await api.DeleteDriveFileAsync(room.RoomId, file.Id, CancellationToken.None);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            RoomLabel.Text = $"Usuwanie nie powiodło się: {ex.Message}";
        }
    }
}
