# Sekrety lokalne

`ConnectionStrings:Lumora` i `RoomAuth:Pepper` nie są w żadnym pliku w repo — trzymamy je
w `dotnet user-secrets` (poza repo, w profilu użytkownika).

## Konfiguracja lokalnie

```
dotnet user-secrets set "ConnectionStrings:Lumora" "Server=...;Database=Lumora;User Id=sa;Password=...;TrustServerCertificate=True" --project src/Api
dotnet user-secrets set "RoomAuth:Pepper" "<losowy sekret, min. 32 znaki>" --project src/Api
```

`WebApplication.CreateBuilder` ładuje `user-secrets` automatycznie w środowisku `Development`
dzięki `UserSecretsId` w `Lumora.Server.Api.csproj` — nie trzeba nic dodawać w `Program.cs`.

## Produkcja (k3s)

Te same klucze wstrzykiwane przez Kubernetes Secret jako zmienne środowiskowe
(`ConnectionStrings__Lumora`, `RoomAuth__Pepper`) — nigdy przez `appsettings.json`.

## Jeśli coś trafi do repo przez pomyłkę

Usunięcie pliku w kolejnym commicie nie wystarczy — sekret zostaje w historii gita.
Rotować sekret (nowe hasło do bazy, nowy pepper) i dopiero potem ewentualnie czyścić historię.
