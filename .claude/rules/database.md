---
paths:
  - src/Infrastructure/**
  - "**/Migrations/**"
---

# Konwencje EF Core

Ten plik ładuje się TYLKO gdy Claude czyta/edytuje pliki w Infrastructure
lub migracjach — nie zajmuje kontekstu w reszcie sesji.

## Repository pattern

- Interfejs repozytorium w Application/Abstractions/ (np. IRoomRepository)
- Implementacja w Infrastructure/Persistence/Repositories/
- Jeden repozytorium per agregat — nie rób generycznego IRepository<T>, to gubi semantykę zapytań domenowych
- IUnitOfWork z metodą SaveChangesAsync — handler wywołuje repozytoria, potem jawnie SaveChangesAsync na końcu (nie auto-save w repo)

## Zasady

- NIE modyfikuj wygenerowanych plików migracji ręcznie — usuń i wygeneruj ponownie
- Każda migracja z `DropColumn`/`DropTable` wymaga wcześniejszego kroku migracji danych
- Dodanie NOT NULL na istniejącej tabeli → zawsze z wartością domyślną
- Konfiguracje encji: `IEntityTypeConfiguration<T>`, jeden plik per encja,
  folder `Persistence/Configurations/`

## Known issues

- Migracja czasem failuje przy równoległym uruchomieniu testów integracyjnych —
  uruchamiaj `dotnet ef database update` sekwencyjnie, nie w CI matrix
