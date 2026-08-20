using Avalonia.Controls;
using Avalonia.Interactivity;
using Lumora.Client.Core.Rooms;
using Lumora.Client.Core.Transport;
using Lumora.Client.Desktop.Rooms;
using Lumora.Contracts.Rooms;

namespace Lumora.Client.Desktop.Windows;

public sealed record RoomListRow(string Slug, string DisplayName, bool IsPrivate, bool IsActive)
{
    public string Icon => IsPrivate ? "🔒" : "🌐";
}

public partial class RoomWindow : Window
{
    private readonly RoomSessionService session;
    private readonly LumoraApiClient api;
    private readonly ActiveRoomStore activeRoomStore;

    public RoomWindow(RoomSessionService session, LumoraApiClient api, ActiveRoomStore activeRoomStore)
    {
        InitializeComponent();
        this.session = session;
        this.api = api;
        this.activeRoomStore = activeRoomStore;

        PrivateCheckBox.IsCheckedChanged += (_, _) => CreatePasswordPanel.IsVisible = PrivateCheckBox.IsChecked == true;

        Opened += async (_, _) => await LoadRoomsAsync();
    }

    private async Task LoadRoomsAsync()
    {
        try
        {
            var activeSlug = activeRoomStore.ActiveRoom?.Slug;
            var rooms = await api.ListRoomsAsync(CancellationToken.None);
            RoomsList.ItemsSource = rooms
                .Select(r => new RoomListRow(r.Slug, r.DisplayName, r.IsPrivate, r.Slug == activeSlug))
                .ToList();
        }
        catch (Exception ex)
        {
            ShowError($"Nie udało się pobrać listy przestrzeni: {ex.Message}");
        }
    }

    private void OnJoinModeClicked(object? sender, RoutedEventArgs e) => SetMode(create: false);

    private void OnCreateModeClicked(object? sender, RoutedEventArgs e) => SetMode(create: true);

    private void SetMode(bool create)
    {
        CreatePanel.IsVisible = create;
        RoomsList.IsVisible = !create;
        JoinModeButton.Classes.Set("active", !create);
        CreateModeButton.Classes.Set("active", create);
        ErrorText.IsVisible = false;
    }

    private async void OnRoomJoinClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: RoomListRow room })
        {
            return;
        }

        if (!room.IsPrivate)
        {
            var error = await session.JoinAsync(room.Slug, password: null, CancellationToken.None);
            if (error is not null)
            {
                ShowError(error);
                return;
            }

            // Stay open and refresh in place — the "✓ Aktualna" badge moving to this row
            // is the confirmation that the switch actually happened.
            ErrorText.IsVisible = false;
            await LoadRoomsAsync();
            return;
        }

        var prompt = new PasswordPromptWindow(
            room.DisplayName, password => session.JoinAsync(room.Slug, password, CancellationToken.None));

        await prompt.ShowDialog(this);

        if (prompt.Succeeded)
        {
            ErrorText.IsVisible = false;
            await LoadRoomsAsync();
        }
    }

    private async void OnCreateSubmitClicked(object? sender, RoutedEventArgs e)
    {
        var slug = SlugBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(slug))
        {
            ShowError("Podaj slug przestrzeni.");
            return;
        }

        var isPrivate = PrivateCheckBox.IsChecked == true;
        var password = isPrivate ? CreatePasswordBox.Text : null;

        if (isPrivate && string.IsNullOrEmpty(password))
        {
            ShowError("Prywatna przestrzeń wymaga hasła.");
            return;
        }

        var displayName = DisplayNameBox.Text?.Trim() is { Length: > 0 } n ? n : slug;

        CreateSubmitButton.IsEnabled = false;
        var error = await session.CreateAsync(slug, displayName, isPrivate, password, CancellationToken.None);
        CreateSubmitButton.IsEnabled = true;

        if (error is not null)
        {
            ShowError(error);
            return;
        }

        // Same as joining — switch back to the room list so the new room shows up marked
        // "✓ Aktualna" instead of just vanishing the window.
        SetMode(create: false);
        await LoadRoomsAsync();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
