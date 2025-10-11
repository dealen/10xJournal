# API Endpoint Implementation Plan: DeleteJournalEntry

## 1. Przegląd punktu końcowego
Celem tego punktu końcowego jest umożliwienie uwierzytelnionemu użytkownikowi trwałego usunięcia jednego ze swoich wpisów w dzienniku. Operacja jest nieodwracalna. Dostęp jest ściśle kontrolowany przez polityki bezpieczeństwa na poziomie wiersza (RLS) w bazie danych, co gwarantuje, że użytkownicy mogą usuwać wyłącznie własne dane.

## 2. Szczegóły żądania
- **Metoda HTTP**: `DELETE`
- **Struktura URL**: `/rest/v1/journal_entries?id=eq.{entry_id}`
- **Parametry**:
  - **Wymagane**:
    - `id` (w query string): `UUID` wpisu do usunięcia.
    - `Authorization` (nagłówek HTTP): Token `Bearer <JWT>` uwierzytelniający użytkownika.
  - **Opcjonalne**: Brak.
- **Request Body**: Brak.

## 3. Wykorzystywane typy
Implementacja nie wymaga tworzenia nowych modeli DTO ani Command. Operacja będzie wykorzystywać istniejący model `JournalEntry` z biblioteki klienckiej Supabase oraz operować na prostym typie `Guid` (`UUID`) dla identyfikatora wpisu.

## 4. Szczegóły odpowiedzi
- **Odpowiedź sukcesu**:
  - **Kod**: `204 No Content`
  - **Treść**: Brak.
- **Odpowiedzi błędów**:
  - **Kod**: `400 Bad Request` - Jeśli parametr `id` ma nieprawidłowy format.
  - **Kod**: `401 Unauthorized` - Jeśli token JWT jest nieprawidłowy, brakujący lub wygasł.
  - **Kod**: `404 Not Found` - Jeśli wpis o podanym `id` nie istnieje lub nie należy do uwierzytelnionego użytkownika.
  - **Kod**: `5xx Server Error` - W przypadku wewnętrznego błędu serwera Supabase.

## 5. Przepływ danych
1.  Użytkownik inicjuje akcję usunięcia w interfejsie użytkownika (np. klika przycisk "Usuń" przy wpisie).
2.  Aplikacja Blazor WASM wywołuje metodę w dedykowanej klasie obsługi (np. `DeleteEntryHandler.cs`) w ramach odpowiedniego "slice" funkcji (`Features/JournalEntries/DeleteEntry/`).
3.  Metoda ta, używając klienta Supabase C#, konstruuje i wysyła żądanie `DELETE` do punktu końcowego `/rest/v1/journal_entries`, przekazując `id` wpisu jako filtr.
4.  PostgREST (warstwa API Supabase) odbiera żądanie.
5.  Baza danych PostgreSQL przetwarza żądanie `DELETE` na tabeli `journal_entries`.
6.  Polityka RLS jest automatycznie stosowana, dodając warunek `WHERE user_id = auth.uid()`.
7.  Jeśli wiersz pasujący do `id` i `user_id` zostanie znaleziony, zostaje usunięty.
8.  Baza danych zwraca potwierdzenie do PostgREST.
9.  PostgREST wysyła odpowiedź `204 No Content` do aplikacji klienckiej.
10. Aplikacja kliencka odbiera odpowiedź i aktualizuje interfejs użytkownika, usuwając wpis z listy.

## 6. Względy bezpieczeństwa
- **Uwierzytelnianie**: Każde żądanie musi zawierać prawidłowy token JWT w nagłówku `Authorization`. Klient Supabase C# automatycznie zarządza dołączaniem tego nagłówka.
- **Autoryzacja**: Kluczowym mechanizmem bezpieczeństwa jest polityka RLS zdefiniowana w bazie danych:
  ```sql
  CREATE POLICY "Users can CRUD their own journal entries."
  ON public.journal_entries FOR ALL
  USING (auth.uid() = user_id);
  ```
  Ta polityka gwarantuje, że operacja `DELETE` powiedzie się tylko wtedy, gdy `user_id` w usuwanym wierszu jest identyczny z `uid` użytkownika pochodzącym z jego tokenu JWT. Skutecznie uniemożliwia to jednemu użytkownikowi usunięcie danych innego.

## 7. Obsługa błędów
Logika po stronie klienta musi być przygotowana na obsługę następujących scenariuszy:
- **Blok `try-catch`**: Całe wywołanie API musi być opakowane w blok `try-catch`.
- **Obsługa wyjątków Supabase**: Należy przechwytywać specyficzne wyjątki z biblioteki Supabase (np. `BadRequestException`, `NotFoundException`).
- **Logowanie**: W bloku `catch` należy zalogować szczegóły błędu przy użyciu `ILogger` w celu ułatwienia diagnostyki.
- **Informacja dla użytkownika**: W przypadku błędu (np. utraty połączenia z internetem lub błędu serwera), interfejs użytkownika musi wyświetlić zrozumiały komunikat, np. "Nie udało się usunąć wpisu. Spróbuj ponownie później."

## 8. Rozważania dotyczące wydajności
- Operacja `DELETE` na pojedynczym wierszu przy użyciu klucza podstawowego (`id`) jest wysoce wydajna w PostgreSQL i nie przewiduje się żadnych wąskich gardeł wydajnościowych.
- Indeks na `(user_id, created_at)` nie ma bezpośredniego wpływu na wydajność operacji `DELETE` przez `id`, ale jest kluczowy dla ogólnej wydajności operacji na tabeli `journal_entries`.

## 9. Etapy wdrożenia
1.  **Struktura plików**: Zgodnie z Vertical Slice Architecture, utwórz nowy folder: `10xJournal.Client/Features/JournalEntries/DeleteEntry/`.
2.  **Logika usuwania**: Wewnątrz nowego folderu utwórz klasę `DeleteEntryHandler.cs`.
    - Wstrzyknij klienta Supabase (`Supabase.Client`) oraz `ILogger` przez konstruktor.
    - Utwórz publiczną, asynchroniczną metodę `ExecuteAsync(Guid entryId)`.
3.  **Implementacja metody**: W metodzie `ExecuteAsync`:
    - Zaimplementuj blok `try-catch` do obsługi błędów.
    - Użyj metody `_supabaseClient.From<JournalEntry>().Where(e => e.Id == entryId).Delete()` do wykonania operacji.
    - W bloku `catch` zaloguj wyjątek i zwróć `false` lub rzuć wyjątek dalej, aby obsłużyć go w UI.
4.  **Rejestracja w DI**: Zarejestruj `DeleteEntryHandler` w kontenerze Dependency Injection w pliku `Program.cs` jako `AddScoped`.
5.  **Integracja z UI**:
    - W komponencie Blazor, który wyświetla listę wpisów (`ListEntries.razor`), dodaj przycisk "Usuń" dla każdego wpisu.
    - Dodaj okno dialogowe z potwierdzeniem "Czy na pewno chcesz usunąć ten wpis?", aby zapobiec przypadkowemu usunięciu.
    - Po potwierdzeniu przez użytkownika, wstrzyknij `DeleteEntryHandler` do komponentu i wywołaj metodę `ExecuteAsync`, przekazując `id` wpisu.
6.  **Aktualizacja stanu UI**: Po pomyślnym wykonaniu `ExecuteAsync`, usuń wpis z lokalnej kolekcji w komponencie Blazor, aby interfejs użytkownika natychmiast odzwierciedlił zmianę bez konieczności ponownego pobierania całej listy z serwera.
