Dodaj nowy endpoint API o nazwie i celu podanym przez użytkownika w argumencie: $ARGUMENTS

Wygeneruj kompletny feature zgodnie z CLAUDE.md:
1. Command lub Query (w zależności czy to zapis czy odczyt)
2. Handler w Application, korzystający z interfejsu repozytorium (nie bezpośrednio z DbContext)
3. Validator (FluentValidation) jeśli command przyjmuje dane wejściowe
4. Endpoint w kontrolerze API — cienki, tylko wywołanie mediatora
5. Deleguj napisanie testów do subagenta `test-writer` zamiast pisać je sam w tym wątku

Nie czytaj całego repo od zera — użyj istniejącego podobnego feature'a jako wzorca
(znajdź go przez Grep po nazwie podobnego handlera), to oszczędza kontekst i trzyma
spójność stylu.
