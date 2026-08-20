Wykonaj pełny przegląd aktualnych zmian przed PR:

1. `git diff main --stat` żeby zobaczyć zakres zmian (nie czytaj całego diffa do głównego kontekstu)
2. Deleguj do subagenta `code-reviewer` przegląd konwencji i architektury
3. Jeśli zmiany dotyczą Infrastructure/Migrations — deleguj też do `ef-migration-checker`
4. Zbierz wyniki obu subagentów i przedstaw jako jedną skonsolidowaną listę do naprawy,
   posortowaną: blokujące → warto poprawić → opcjonalne

Nie wklejaj surowego outputu subagentów 1:1 — zsyntetyzuj.
