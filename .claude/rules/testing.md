---
paths:
  - tests/**
---

# Konwencje testów

## Nazewnictwo
`Metoda_Scenariusz_OczekiwanyWynik`

## Zasady
- Unit testy: logika Domain + Application, mockowanie przez NSubstitute - handler nie powinien dotykać prawdziwego EF Core w testach jednostkowych
- Integration testy: `WebApplicationFactory`, osobny projekt `tests/IntegrationTests/`
- Do testów integracyjnych używaj realnej bazy danych(Testcontainers)
- Asercje: FluentAssertions, nie `Assert.Equal`
- Nie testuj frameworka (EF Core, MediatR) — testuj logikę zespołu

## Przykład wzorca
Zobacz `tests/UnitTests/Application/Products/CreateProductHandlerTests.cs`
jako kanoniczny wzorzec — nie opisuj wzorca prozą, wskaż plik.
