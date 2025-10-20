# Kompleksowy Plan Testów dla 10xJournal

Na podstawie analizy kodu i instrukcji testowania, niniejszy plan testów priorytetyzuje testy integracyjne z Supabase, następnie testy E2E dla krytycznych przepływów użytkownika, a na końcu testy jednostkowe dla komponentów czystej logiki.

## Rekomendowane Narzędzia Testowe

### Testy Jednostkowe
- **xUnit**: Główny framework testowy dla wszystkich typów testów w .NET.
- **Moq**: Do mockowania zależności w testach jednostkowych, jeśli to konieczne.
- **FluentAssertions**: Dla bardziej czytelnych asercji.

### Testy Integracyjne
- **xUnit**: Do uruchamiania testów integracyjnych przeciwko Supabase.
- **Testcontainers**: Do uruchamiania izolowanych instancji Supabase podczas testów (rekomendowane dla CI).
- **Respawn**: Do czyszczenia stanu bazy danych między testami.

### Testy End-to-End (E2E)
- **Playwright for .NET**: Do automatyzacji testów w przeglądarce.
- **xUnit**: Jako runner dla testów E2E.

## Plan Testów Integracyjnych (Najwyższy Priorytet)

### 1. Testy Integracyjne Autentykacji

#### Rejestracja Użytkownika
- **Opis**: Weryfikacja procesu rejestracji użytkownika z Supabase.
- **Przypadki Testowe**:
  - Rejestracja nowego użytkownika z prawidłowymi danymi.
  - Próba rejestracji z istniejącym adresem e-mail.
  - Walidacja wymagań dotyczących hasła.
  - Test polityki RLS po rejestracji.
- **Setup**: Dedykowana instancja testowa Supabase.
- **Oczekiwany Rezultat**: Rekord użytkownika utworzony w tabelach `auth.users` i `public.profiles`.

#### Logowanie Użytkownika
- **Opis**: Weryfikacja autentykacji i obsługi tokenów.
- **Przypadki Testowe**:
  - Logowanie z prawidłowymi danymi.
  - Logowanie z nieprawidłowymi danymi.
  - Logowanie z nieaktywnym kontem.
  - Weryfikacja przepływu odświeżania tokena.
- **Setup**: Wcześniej utworzeni użytkownicy testowi w Supabase.
- **Oczekiwany Rezultat**: Uzyskanie i poprawne przechowanie ważnego tokena JWT.

#### Row Level Security (RLS)
- **Opis**: Weryfikacja, czy polityki RLS zapobiegają nieautoryzowanemu dostępowi do danych.
- **Przypadki Testowe**:
  - Użytkownik A nie może uzyskać dostępu do wpisów Użytkownika B.
  - Użytkownik A nie może modyfikować wpisów Użytkownika B.
  - Użytkownik A nie może uzyskać dostępu do profilu Użytkownika B.
  - Anonimowi użytkownicy nie mogą uzyskać dostępu do chronionych zasobów.
- **Setup**: Wielu użytkowników testowych z wcześniej wypełnionymi danymi.
- **Oczekiwany Rezultat**: Odmowa dostępu dla nieautoryzowanych operacji.

### 2. Testy Integracyjne Wpisów Dziennika

#### Operacje CRUD
- **Opis**: Testowanie wszystkich operacji bazodanowych na wpisach dziennika.
- **Przypadki Testowe**:
  - Utworzenie wpisu i weryfikacja jego zapisania.
  - Pobranie konkretnego wpisu.
  - Pobranie paginowanej listy wpisów.
  - Aktualizacja wpisu.
  - Usunięcie wpisu.
  - Weryfikacja, że usunięte wpisy nie są dostępne.
- **Setup**: Użytkownik testowy z wypełnionymi i pustymi stanami dziennika.
- **Oczekiwany Rezultat**: Wszystkie operacje poprawnie modyfikują stan bazy danych.

#### Zarządzanie "Streak" Użytkownika
- **Opis**: Testowanie obliczania i śledzenia serii wpisów.
- **Przypadki Testowe**:
  - Utworzenie wpisu i weryfikacja inkrementacji "streak".
  - Pominięcie dnia i weryfikacja resetu "streak".
  - Utworzenie wielu wpisów tego samego dnia i weryfikacja, że "streak" liczy się tylko raz.
- **Setup**: Użytkownik testowy z kontrolowaną historią wpisów.
- **Oczekiwany Rezultat**: Wartości "streak" są poprawnie aktualizowane.

### 3. Testy Integracyjne Profilu Użytkownika

#### Zarządzanie Profilem
- **Opis**: Testowanie zarządzania danymi profilu.
- **Przypadki Testowe**:
  - Pobranie profilu użytkownika.
  - Aktualizacja informacji profilowych.
  - Weryfikacja, że aktualizacje profilu są zapisywane.
- **Setup**: Użytkownik testowy z podstawowym profilem.
- **Oczekiwany Rezultat**: Zmiany w profilu odzwierciedlone w bazie danych.

#### Usuwanie Konta
- **Opis**: Testowanie funkcjonalności usuwania konta.
- **Przypadki Testowe**:
  - Usunięcie konta i weryfikacja, że wszystkie dane użytkownika zostały usunięte.
  - Weryfikacja, że usunięcie kaskadowe usuwa wszystkie powiązane dane.
  - Weryfikacja, że rekordy autentykacji zostały usunięte.
- **Setup**: Użytkownik testowy z wpisami i danymi profilu.
- **Oczekiwany Rezultat**: Wszystkie dane użytkownika całkowicie usunięte z bazy danych.

## Testy End-to-End (Wysoki Priorytet)

### 1. Rejestracja Użytkownika i Pierwszy Wpis
- **Opis**: Testowanie kompletnego przepływu nowego użytkownika.
- **Kroki Testu**:
  1. Nawigacja do strony docelowej.
  2. Kliknięcie przycisku "Zarejestruj się".
  3. Wypełnienie formularza rejestracyjnego.
  4. Weryfikacja, że wpis powitalny jest obecny.
  5. Utworzenie nowego wpisu.
  6. Weryfikacja, że wpis pojawił się na liście.
- **Zależności**: Czyste środowisko testowe.
- **Oczekiwany Rezultat**: Nowy użytkownik może się zarejestrować i utworzyć swój pierwszy wpis.

### 2. Zarządzanie Wpisami Dziennika
- **Opis**: Testowanie cyklu życia wpisu dziennika.
- **Kroki Testu**:
  1. Zalogowanie jako istniejący użytkownik.
  2. Wyświetlenie listy wpisów.
  3. Utworzenie nowego wpisu.
  4. Edycja istniejącego wpisu.
  5. Usunięcie wpisu.
  6. Weryfikacja, że lista wpisów odzwierciedla zmiany.
- **Zależności**: Użytkownik testowy z istniejącymi wpisami.
- **Oczekiwany Rezultat**: Użytkownik może pomyślnie zarządzać swoimi wpisami.

### 3. Ustawienia Konta i Usuwanie
- **Opis**: Testowanie przepływów zarządzania kontem.
- **Kroki Testu**:
  1. Zalogowanie jako istniejący użytkownik.
  2. Nawigacja do strony ustawień.
  3. Aktualizacja informacji profilowych.
  4. Eksport danych dziennika.
  5. Usunięcie konta.
  6. Weryfikacja niemożności zalogowania się po usunięciu.
- **Zależności**: Użytkownik testowy z kompletnym profilem i wpisami.
- **Oczekiwany Rezultat**: Użytkownik może zarządzać swoim kontem i je usunąć.

## Plan Testów Jednostkowych (w miarę potrzeb)

Skupienie się wyłącznie na komponentach czystej logiki bez zewnętrznych zależności:

### 1. Narzędzia do Przetwarzania Tekstu
- **Opis**: Testowanie wszelkich narzędzi do formatowania lub przetwarzania tekstu.
- **Przypadki Testowe**:
  - Poprawne formatowanie dat.
  - Generowanie tytułu wpisu z treści.
  - Formatowanie tekstu wyświetlania "streak".
- **Mockowanie**: Niepotrzebne (czyste funkcje).

### 2. Logika Walidacji Formularzy
- **Opis**: Testowanie reguł walidacji po stronie klienta.
- **Przypadki Testowe**:
  - Walidacja formatu e-mail.
  - Walidacja siły hasła.
  - Walidacja treści wpisu.
- **Mockowanie**: Niepotrzebne (czysta logika).

### 3. Zarządzanie Stanem po Stronie Klienta
- **Opis**: Testowanie transformacji stanu.
- **Przypadki Testowe**:
  - Sortowanie wpisów.
  - Filtrowanie wpisów.
  - Obliczenia paginacji.
- **Mockowanie**: Niepotrzebne (czysta logika).

## Wymagania Dotyczące Danych Testowych

### Dane do Testów Integracyjnych
- Dedykowana instancja testowa Supabase z:
  - Wieloma użytkownikami testowymi.
  - Różnymi wpisami z różnymi datami.
  - Różnymi wartościami "streak".
  - Przypadkami brzegowymi (bardzo długie wpisy, znaki specjalne).

### Dane do Testów E2E
- Czyste środowisko testowe, które resetuje się między przebiegami testów.
- Dane logowania dla wcześniej utworzonych kont testowych.
- Przykładowa treść wpisów.

## Uwagi do Strategii Testowania

### Konfiguracja Środowiska Testowego
1. **Dedykowany Projekt Testowy Supabase**:
   - Utworzenie osobnego projektu Supabase do testów.
   - Zastosowanie wszystkich migracji, aby schemat pasował do produkcji.
   - Przygotowanie skryptów do seedowania danych testowych.

2. **Reset Bazy Danych Testowej**:
   - Implementacja resetu bazy danych między przebiegami testów.
   - Użycie snapshotów lub migracji do przywrócenia znanego stanu.

### Integracja z CI/CD
1. **Workflow GitHub Actions**:
   - Uruchamianie testów integracyjnych przy tworzeniu PR i merge'ach do `main`.
   - Uruchamianie testów E2E tylko przy merge'ach do `main`.
   - Generowanie raportów pokrycia kodu testami.

2. **Równoległe Uruchamianie Testów**:
   - Konfiguracja testów do równoległego uruchamiania, gdy to możliwe.
   - Izolacja testów modyfikujących współdzielony stan.

### Aspekty Testowania Bezpieczeństwa
1. **Testowanie Polityk RLS**:
   - Kompleksowe testy dla wszystkich polityk RLS.
   - Weryfikacja poprawnego egzekwowania autentykacji.

2. **Walidacja Danych Wejściowych**:
   - Testowanie zapobiegania SQL Injection.
   - Testowanie ochrony przed XSS.

### Aspekty Wydajnościowe
1. **Wydajność Zapytań**:
   - Testowanie z realistycznymi wolumenami danych.
   - Weryfikacja użycia indeksów w typowych zapytaniach.

2. **Czasy Odpowiedzi API**:
   - Benchmarkowanie typowych wywołań API do Supabase.
   - Ustawienie budżetów wydajnościowych dla krytycznych operacji.

### Dokumentacja
1. Dokumentowanie wszystkich scenariuszy testowych w kodzie.
2. Dołączenie instrukcji konfiguracji lokalnego środowiska testowego.
3. Utrzymywanie przykładów, jak pisać nowe testy.

Ten plan testów jest zgodny z architekturą Vertical Slice projektu i koncentruje się na obszarach o najwyższym priorytecie, minimalizując jednocześnie wysiłek włożony w aspekty o niższym priorytecie.