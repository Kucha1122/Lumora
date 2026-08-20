---
paths:
  - src/Api/**
---

# Konwencje API

## Endpointy
- Cienkie — tylko walidacja wejścia (przez model binding) + wywołanie mediatora
- Zwracaj `Results<Ok<T>, ValidationProblem, NotFound>` (typed results), nie `IActionResult`

## Nazewnictwo
- Command: `Create<Encja>Command`, `Update<Encja>Command`
- Query: `Get<Encja>Query`, `List<Encje>Query`
- DTO wejściowe: `Create<Encja>Request`
- DTO wyjściowe: `<Encja>Dto`

## Auth
- `[Authorize]` na poziomie endpointu, nie kontrolera — jawność ważniejsza niż DRY
