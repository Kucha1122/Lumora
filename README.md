# Lumora

Aplikacja do współdzielenia schowka (tekst + obrazki) i plików w czasie rzeczywistym pomiędzy Windows / macOS / Android w obrębie sieci lokalnej, z podziałem na środowiska (rooms) publiczne i prywatne (hasło).

## Tech Stack

- .NET 10, ASP.NET Core Minimal APIs
- Entity Framework Core 10 + MS SQL Server
- MediatR (CQRS), FluentValidation
- Avalonia UI (Windows tray client)
- xUnit + FluentAssertions

## Struktura

- `src/Domain` — encje, value objects, zero zależności zewnętrznych
- `src/Application` — commands, queries, handlery (CQRS)
- `src/Infrastructure` — EF Core, SignalR, storage
- `src/Api` — endpointy Minimal API
- `src/Contracts` — DTO współdzielone między klientem a serwerem
- `src/Client.Core` — logika kliencka niezależna od platformy (crypto, sync, transport)
- `src/Client.Desktop` — klient trayowy (Avalonia, Windows)
- `tests/` — testy jednostkowe i integracyjne

Zobacz `CLAUDE.md` po szczegóły architektury i konwencji.

## Uruchomienie

```
dotnet build
dotnet test
dotnet run --project src/Api
```

Konfiguracja sekretów (connection string, klucze) — patrz `src/Api/README-secrets.md`.
