# API Endpoint Implementation Plan: Update Journal Entry

## 1. Przegląd punktu końcowego
Ten punkt końcowy umożliwia uwierzytelnionym użytkownikom aktualizację treści istniejącego wpisu w dzienniku. Dostęp jest ściśle kontrolowany przez polityki bezpieczeństwa na poziomie wiersza (RLS) w bazie danych, co gwarantuje, że użytkownicy mogą modyfikować wyłącznie własne dane.

## 2. Szczegóły żądania
- **Metoda HTTP:** `PATCH`
- **Struktura URL:** `/rest/v1/journal_entries?id=eq.{entry_id}`
- **Parametry:**
  - **Wymagane:**
    - `id`: (w URL query) Identyfikator UUID wpisu w dzienniku, który ma zostać zaktualizowany.
    - `Authorization`: (w nagłówku) `Bearer <jwt_token>` - token uwierzytelniający użytkownika.
  - **Opcjonalne:** Brak.
- **Request Body:**
  ```json
  {
      "content": "Zaktualizowana treść mojego wpisu w dzienniku."
  }
  ```

## 3. Wykorzystywane typy
- **Command Model:** `UpdateJournalEntryRequest.cs`
  ```csharp
  // Plik: 10xJournal.Client/Features/JournalEntries/Models/UpdateJournalEntryRequest.cs
  namespace _10xJournal.Client.Features.JournalEntries.Models;

  public class UpdateJournalEntryRequest
  {
      public string Content { get; set; }
  }
  ```
- **Data Model (Response):** `JournalEntry.cs` (istniejący model)

## 4. Przepływ danych
1.  Użytkownik wprowadza zmiany w interfejsie użytkownika (np. w komponencie `EditEntry.razor`).
2.  Po zatwierdzeniu zmian, logika komponentu tworzy instancję `UpdateJournalEntryRequest` z nową treścią.
3.  Wykonywane jest wywołanie klienta Supabase, np. `_supabaseClient.From<JournalEntry>().Where(x => x.Id == entryId).Set(x => x.Content, newContent).Update()`.
4.  Klient Supabase konstruuje żądanie `PATCH` do `/rest/v1/journal_entries`, dołączając token JWT.
5.  PostgREST API w Supabase odbiera żądanie.
6.  Baza danych PostgreSQL weryfikuje token JWT i stosuje politykę RLS, aby sprawdzić, czy `auth.uid()` jest równe `user_id` w docelowym wierszu.
7.  Jeśli autoryzacja się powiedzie, pole `content` oraz `updated_at` (automatycznie przez trigger) w tabeli `journal_entries` zostają zaktualizowane.
8.  Supabase zwraca odpowiedź `200 OK` z pełnym, zaktualizowanym obiektem `JournalEntry`.
9.  Aplikacja kliencka otrzymuje odpowiedź i aktualizuje stan interfejsu użytkownika.

## 5. Względy bezpieczeństwa
- **Uwierzytelnianie:** Każde żądanie musi zawierać prawidłowy token JWT w nagłówku `Authorization`. Brak lub nieważność tokenu spowoduje odpowiedź `401 Unauthorized`.
- **Autoryzacja:** Głównym mechanizmem jest polityka RLS w PostgreSQL: `CREATE POLICY "Users can CRUD their own journal entries." ON public.journal_entries FOR ALL USING (auth.uid() = user_id);`. Zapewnia ona, że operacja `UPDATE` powiedzie się tylko wtedy, gdy ID uwierzytelnionego użytkownika pasuje do `user_id` wpisu. Próba modyfikacji cudzego wpisu zakończy się błędem `404 Not Found`.
- **Walidacja danych wejściowych:** Walidacja `content` (np. sprawdzanie, czy nie jest pusty) zostanie zaimplementowana po stronie klienta przy użyciu `FluentValidation` dla `UpdateJournalEntryRequest`, aby zapobiec wysyłaniu nieprawidłowych danych.

## 6. Obsługa błędów
- **`200 OK`:** Operacja zakończona sukcesem. Odpowiedź zawiera zaktualizowany obiekt wpisu.
- **`400 Bad Request`:** Żądanie jest nieprawidłowe, np. `content` ma wartość `null`. Baza danych odrzuci takie żądanie.
- **`401 Unauthorized`:** Użytkownik jest nieuwierzytelniony (brak/nieprawidłowy token JWT).
- **`404 Not Found`:** Wpis o podanym `id` nie istnieje lub nie należy do uwierzytelnionego użytkownika.
- **`500 Internal Server Error`:** Wystąpił wewnętrzny błąd po stronie serwera Supabase.
Wszystkie błędy powinny być przechwytywane w bloku `try-catch` w kodzie klienta, logowane za pomocą `ILogger` i komunikowane użytkownikowi w przyjazny sposób.

## 7. Rozważania dotyczące wydajności
- Operacja `UPDATE` na pojedynczym wierszu przy użyciu klucza głównego jest wysoce wydajna i nie powinna stanowić wąskiego gardła.
- Indeks na `(user_id, created_at)` nie ma bezpośredniego wpływu na wydajność tej konkretnej operacji `UPDATE`, ale jest kluczowy dla operacji odczytu listy wpisów.
- Obciążenie jest minimalne, ponieważ operacja dotyczy tylko jednego rekordu.

## 8. Etapy wdrożenia
1.  **Utworzenie struktury plików:** Zgodnie z architekturą Vertical Slice, utwórz folder `Features/JournalEntries/UpdateEntry`.
2.  **Stworzenie komponentu UI:** Zaprojektuj i zaimplementuj komponent `UpdateEntry.razor`, który będzie zawierał formularz do edycji treści wpisu.
3.  **Implementacja logiki:** W bloku `@code` komponentu `UpdateEntry.razor` (lub w dedykowanym handlerze):
    a. Wstrzyknij klienta Supabase (`Supabase.Client`) oraz `ILogger`.
    b. Zaimplementuj metodę `HandleUpdateAsync`, która przyjmuje ID wpisu i nową treść.
    c. Wewnątrz metody, skonstruuj i wykonaj wywołanie `_supabaseClient.From<JournalEntry>()...Update()`.
    d. Zaimplementuj obsługę błędów w bloku `try-catch`, logując wyjątki i informując użytkownika o wyniku operacji.
4.  **Walidacja:** (Zalecane) Dodaj walidator `FluentValidation` dla `UpdateJournalEntryRequest`, aby upewnić się, że `content` nie jest pusty.
5.  **Integracja:** Zintegruj komponent `UpdateEntry.razor` z listą wpisów (`ListEntries.razor`), aby umożliwić użytkownikowi nawigację do edycji wybranego wpisu.
