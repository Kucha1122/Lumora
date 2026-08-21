using Lumora.Client.Android.Updates;
using Lumora.Client.Core.Rooms;

namespace Lumora.Client.Android.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly ActiveRoomStore activeRoom;
    private readonly UpdateService updateService;
    private Lumora.Contracts.Updates.AndroidReleaseDto? pendingRelease;

    public SettingsPage(ActiveRoomStore activeRoom, UpdateService updateService)
    {
        InitializeComponent();
        this.activeRoom = activeRoom;
        this.updateService = updateService;
        activeRoom.ActiveRoomChanged += _ => MainThread.BeginInvokeOnMainThread(UpdateStatus);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ServerAddressLabel.Text = ServerSettings.LoadBaseAddress().ToString();
        UpdateStatus();
        VersionLabel.Text = $"Lumora {AppInfo.Current.VersionString} (build {AppInfo.Current.BuildString})";
        await CheckForUpdateAsync();
    }

    private async Task CheckForUpdateAsync()
    {
        try
        {
            var check = await updateService.CheckAsync(CancellationToken.None);
            pendingRelease = check.Release;
            UpdateCard.IsVisible = check.IsAvailable;
            if (check.IsAvailable)
            {
                UpdateStatusLabel.Text = $"Dostępna wersja {check.Release!.Version} (build {check.Release.VersionCode})";
            }
        }
        catch
        {
            // Server unreachable — updater silently stays quiet, same as any other page's
            // best-effort background check (see ClipboardPage/DrivePage reload failures).
        }
    }

    private async void OnUpdateClicked(object? sender, EventArgs e)
    {
        if (pendingRelease is null)
        {
            return;
        }

        UpdateButton.IsEnabled = false;
        try
        {
            await updateService.DownloadAndInstallAsync(global::Android.App.Application.Context, CancellationToken.None);
        }
        catch (Exception ex)
        {
            await DisplayAlert(null, $"Aktualizacja nie powiodła się: {ex.Message}", "OK");
        }
        finally
        {
            UpdateButton.IsEnabled = true;
        }
    }

    private void UpdateStatus()
    {
        var room = activeRoom.ActiveRoom;
        StatusLabel.Text = room is null
            ? "Nie połączono z żadną przestrzenią"
            : $"{room.DisplayName}{(room.IsPrivate ? " 🔒" : " 🌐")}";
    }
}
