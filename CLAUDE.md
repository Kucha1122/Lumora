# CLAUDE.md - Lumora

## Overview

Aplikacja do współdzielenia schowka (tekst + obrazki) i plików w czasie rzeczywistym pomiędzy Windows / macOS / Android w obrębie sieci lokalnej, z podziałem na środowiska (rooms) publiczne i prywatne (hasło).

## Tech Stack

- .NET 10, ASP.NET Core Minimal APIs
- Entity Framework Core 10 + PostgreSQL/MSSQL
- MediatR dla CQRS
- FluentValidation
- xUnit + FluentAssertions

## Struktura

- `src/Api/` - endpointy, middleware, DI
- `src/Application/` - commands, queries, handlery
- `src/Domain/` - encje, value objects, zero zależności zewnętrznych
- `src/Infrastructure/` - EF Core, integracje zewnętrzne
- `tests/` - unit + integration

## Filozofia architektury (WHY)

Clean Architecture + CQRS:
- Domain nie zależy od niczego — to gwarantuje testowalność logiki biznesowej
  bez bazy danych i frameworka
- Application definiuje interfejsy, Infrastructure je implementuje — odwrócenie
  zależności pozwala podmieniać implementacje (np. bazę) bez ruszania logiki
- Api jest cienkie — endpoint tylko woła mediatora, cała logika w handlerze

## Komendy

- Build: `dotnet build`
- Test: `dotnet test`
- Run: `dotnet run --project src/Api`
- Migracja: `dotnet ef migrations add <Nazwa> -p src/Infrastructure -s src/Api`

## Konwencje

- Handler: `<Akcja><Encja>Handler`, jeden plik = jedna klasa
- Async zawsze z `CancellationToken` jako ostatnim parametrem
- DTO wychodzące z API: `record`, nie `class`

### Wzorce, których UŻYWAMY

- MediatR dla każdego command/query
- Result<T> zamiast wyjątków dla błędów walidacji biznesowej
- Primary constructors dla DI
- Korzystamy z file-scoped namespaces

### Wzorce, których NIE używamy (nie sugeruj ich)

- AutoMapper (mapowania jawne)
- Wyjątki jako kontrola przepływu logiki biznesowej

## Zasady pracy

- Nowy handler = test jednostkowy w tym samym commicie
- Nie zgaduj wymagań biznesowych — pytaj, gdy specyfikacja niejasna
- Nie twierdź "testy przechodzą" bez realnego uruchomienia i wklejenia wyniku

## Więcej kontekstu (na żądanie, nie czytaj z góry)

- `.claude/rules/architecture.md` - szczegóły warstw
- `.claude/rules/testing.md` - konwencje testów
- `.claude/rules/modules/*.md` - reguły per moduł (path-scoped)
