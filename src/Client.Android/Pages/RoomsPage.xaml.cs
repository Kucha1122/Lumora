using System.Windows.Input;
using Lumora.Client.Core.Rooms;
using Lumora.Client.Core.Transport;

namespace Lumora.Client.Android.Pages;

public sealed record RoomRow(string Icon, string DisplayName, string StatusText, ICommand JoinCommand);

public partial class RoomsPage : ContentPage
{
    private readonly RoomSessionService session;
    private readonly LumoraApiClient api;
    private readonly ActiveRoomStore activeRoomStore;

    public RoomsPage(RoomSessionService session, LumoraApiClient api, ActiveRoomStore activeRoomStore)
    {
        InitializeComponent();
        this.session = session;
        this.api = api;
        this.activeRoomStore = activeRoomStore;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadRoomsAsync();
    }

    private async Task LoadRoomsAsync()
    {
        try
        {
            var activeSlug = activeRoomStore.ActiveRoom?.Slug;
            var rooms = await api.ListRoomsAsync(CancellationToken.None);

            RoomsList.ItemsSource = rooms.Select(r => new RoomRow(
                Icon: r.IsPrivate ? "🔒" : "🌐",
                DisplayName: r.DisplayName,
                StatusText: r.Slug == activeSlug ? "✓ Aktualna" : "Dołącz",
                JoinCommand: new Command(async () => await JoinAsync(r.Slug, r.DisplayName, r.IsPrivate))
            )).ToList();
        }
        catch (Exception ex)
        {
            ShowError($"Nie udało się pobrać listy przestrzeni: {ex.Message}");
        }
    }

    private async Task JoinAsync(string slug, string displayName, bool isPrivate)
    {
        string? password = null;
        if (isPrivate)
        {
            // MAUI's DisplayPromptAsync has no native password-masking option — acceptable
            // tradeoff for v1; revisit with a custom entry dialog if this proves annoying.
            password = await DisplayPromptAsync(displayName, "Podaj hasło do przestrzeni:", "Dołącz", "Anuluj");
            if (password is null)
            {
                return; // user cancelled
            }
        }

        var error = await session.JoinAsync(slug, password, CancellationToken.None);
        if (error is not null)
        {
            ShowError(error);
            return;
        }

        ErrorText.IsVisible = false;
        await LoadRoomsAsync();
    }

    private void OnJoinModeClicked(object? sender, EventArgs e) => SetMode(create: false);

    private void OnCreateModeClicked(object? sender, EventArgs e) => SetMode(create: true);

    private void SetMode(bool create)
    {
        CreatePanel.IsVisible = create;
        RoomsList.IsVisible = !create;
        ErrorText.IsVisible = false;
    }

    private void OnPrivateCheckedChanged(object? sender, CheckedChangedEventArgs e) =>
        CreatePasswordBox.IsVisible = e.Value;

    private async void OnCreateSubmitClicked(object? sender, EventArgs e)
    {
        var slug = SlugBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(slug))
        {
            ShowError("Podaj slug przestrzeni.");
            return;
        }

        var isPrivate = PrivateCheckBox.IsChecked;
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

        SetMode(create: false);
        await LoadRoomsAsync();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
