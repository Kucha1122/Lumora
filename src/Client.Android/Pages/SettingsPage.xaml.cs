using Lumora.Client.Core.Rooms;

namespace Lumora.Client.Android.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly ActiveRoomStore activeRoom;

    public SettingsPage(ActiveRoomStore activeRoom)
    {
        InitializeComponent();
        this.activeRoom = activeRoom;
        activeRoom.ActiveRoomChanged += _ => MainThread.BeginInvokeOnMainThread(UpdateStatus);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ServerAddressBox.Text = ServerSettings.LoadBaseAddress().ToString();
        UpdateStatus();
        VersionLabel.Text = $"Lumora {AppInfo.Current.VersionString} (build {AppInfo.Current.BuildString})";
    }

    private void UpdateStatus()
    {
        var room = activeRoom.ActiveRoom;
        StatusLabel.Text = room is null
            ? "Nie połączono z żadną przestrzenią"
            : $"Aktywna przestrzeń: {room.DisplayName}{(room.IsPrivate ? " 🔒" : "")}";
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var address = ServerAddressBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        ServerSettings.SaveBaseAddress(address);

        // HttpClient.BaseAddress is set once at DI composition (MauiProgram.cs) and
        // LumoraApiClient wraps that instance immutably — changing it at runtime would mean
        // rebuilding the whole service graph. Simplest correct behavior for v1: ask for a
        // restart, same as changing appsettings.json requires on Client.Desktop.
        await DisplayAlert(null, "Zapisano. Uruchom aplikację ponownie, aby zastosować nowy adres.", "OK");
    }
}
