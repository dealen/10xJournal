# Plan implementacji endpointu API: CreateJournalEntry

## 1. Przegląd endpointu
Celem tego endpointu jest umożliwienie uwierzytelnionym użytkownikom tworzenia nowych wpisów w dzienniku. Endpoint jest częścią REST API dostarczanego przez Supabase i jest wywoływany bezpośrednio z klienta Blazor WASM. Logika biznesowa, taka jak aktualizacja serii wpisów użytkownika (streaks), jest obsługiwana automatycznie przez wyzwalacze bazy danych PostgreSQL.

## 2. Szczegóły żądania
- **Metoda HTTP**: `POST`
- **Struktura URL**: `/rest/v1/journal_entries`
- **Nagłówki**:
  - `apikey`: **Wymagany**. Publiczny klucz anonimowy projektu Supabase.
  - `Authorization`: **Wymagany**. Token dostępowy JWT użytkownika w formacie `Bearer <token>`.
  - `Content-Type`: **Wymagany**. Musi mieć wartość `application/json`.
  - `Prefer`: **Wymagany**. Musi mieć wartość `return=representation`, aby otrzymać nowo utworzony obiekt w odpowiedzi.
- **Treść żądania**:
  ```json
  {
      "content": "Treść nowego wpisu w dzienniku."
  }
  ```

## 3. Wykorzystywane typy
- **`CreateJournalEntryRequest.cs`**: Model polecenia (Command Model) używany do wysłania danych z formularza Blazor.
  ```csharp
  // 10xJournal.Client/Features/JournalEntries/CreateEntry/CreateJournalEntryRequest.cs
  namespace _10xJournal.Client.Features.JournalEntries.CreateEntry;

  public class CreateJournalEntryRequest
  {
      public string Content { get; set; } = string.Empty;
  }
  ```
- **`JournalEntry.cs`**: Model DTO (Data Transfer Object) reprezentujący odpowiedź z API. Model ten już istnieje i zostanie ponownie wykorzystany.
  - Lokalizacja: `10xJournal.Client/Features/JournalEntries/Models/JournalEntry.cs`

## 4. Szczegóły odpowiedzi
- **Odpowiedź sukcesu (Kod `201 Created`)**:
  - **Treść**: Pełny obiekt nowo utworzonego wpisu w dzienniku, zgodny z modelem `JournalEntry.cs`.
    ```json
    {
        "id": "c3d4e5f6-a7b8-c9d0-e1f2-a3b4c5d6e7f8",
        "user_id": "f1g2h3i4-j5k6-l7m8-n9o0-p1q2r3s4t5u6",
        "content": "Treść nowego wpisu w dzienniku.",
        "created_at": "2025-10-12T09:30:00Z",
        "updated_at": "2025-10-12T09:30:00Z"
    }
    ```
- **Odpowiedzi błędów**:
  - **Kod `400 Bad Request`**: Treść żądania jest nieprawidłowa (np. brak pola `content`).
  - **Kod `401 Unauthorized`**: Problem z uwierzytelnianiem (brak/nieprawidłowy token JWT lub klucz API).
  - **Kod `500 Internal Server Error`**: Wewnętrzny błąd serwera Supabase.

## 5. Przepływ danych
1.  Użytkownik wprowadza treść wpisu w komponencie Blazor i klika przycisk "Zapisz".
2.  Logika komponentu Blazor (`@code`) tworzy instancję `CreateJournalEntryRequest` z danymi z formularza.
3.  Klient `supabase-csharp` jest używany do wysłania żądania `POST` na adres `/rest/v1/journal_entries` z odpowiednimi nagłówkami i treścią.
4.  Supabase odbiera żądanie, weryfikuje token JWT i autoryzuje operację na podstawie polityk RLS.
5.  Dane są wstawiane do tabeli `public.journal_entries`. `user_id` jest pobierane z `auth.uid()`.
6.  Po pomyślnym wstawieniu, wyzwalacz `update_user_streak_on_new_entry` w bazie danych jest aktywowany, aby zaktualizować tabelę `user_streaks`.
7.  Supabase zwraca odpowiedź `201 Created` z pełnym obiektem nowego wpisu.
8.  Klient Blazor odbiera odpowiedź, deserializuje ją do obiektu `JournalEntry` i aktualizuje interfejs użytkownika (np. czyści formularz, nawiguje do listy wpisów).

## 6. Względy bezpieczeństwa
- **Uwierzytelnianie**: Każde żądanie musi być uwierzytelnione za pomocą tokenu JWT. Klient `supabase-csharp` automatycznie zarządza dołączaniem tokenu.
- **Autoryzacja**: Polityki RLS (Row Level Security) w PostgreSQL zapewniają, że:
  - Użytkownik może wstawiać wpisy tylko w swoim imieniu (`WITH CHECK (auth.uid() = user_id)`).
  - `user_id` jest pobierany z zaufanego kontekstu sesji (`auth.uid()`), a nie z danych wejściowych, co zapobiega podszywaniu się.
- **Walidacja danych wejściowych**: Ograniczenie `NOT NULL` na kolumnie `content` w bazie danych zapewnia integralność danych na poziomie serwera. Dodatkowa walidacja po stronie klienta poprawia doświadczenie użytkownika.

## 7. Obsługa błędów
- Wszystkie wywołania API z klienta Blazor będą opakowane w blok `try-catch`.
- W przypadku wyjątku (np. `PostgrestException` z `supabase-csharp`), szczegóły błędu zostaną zarejestrowane za pomocą `ILogger`.
- Użytkownikowi zostanie wyświetlony ogólny, przyjazny komunikat o błędzie wraz z unikalnym identyfikatorem korelacji (Correlation ID), np. "Wystąpił błąd. Prosimy o kontakt z pomocą techniczną, podając ID błędu: [GUID]".

## 8. Rozważania dotyczące wydajności
- Operacja `INSERT` na pojedynczy wiersz jest z natury bardzo szybka.
- Wyzwalacz aktualizujący `user_streaks` działa na tej samej bazie danych i jest zoptymalizowany, więc nie powinien wprowadzać zauważalnego opóźnienia.
- Największym czynnikiem wpływającym na postrzeganą wydajność będzie opóźnienie sieciowe (latency) między klientem a serwerem Supabase. Aplikacja powinna zapewniać wizualną informację zwrotną (np. wskaźnik ładowania) podczas trwania operacji.

## 9. Etapy wdrożenia
1.  **Utworzenie modelu żądania**: Stworzyć plik `CreateJournalEntryRequest.cs` w katalogu `10xJournal.Client/Features/JournalEntries/CreateEntry/`.
2.  **Stworzenie komponentu UI**: Stworzyć plik `CreateEntry.razor` w tym samym katalogu. Komponent będzie zawierał `EditForm` z polem `textarea` powiązanym z modelem `CreateJournalEntryRequest`.
3.  **Implementacja logiki komponentu**: W bloku `@code` komponentu `CreateEntry.razor`:
    a. Wstrzyknąć klienta `Supabase.Client` oraz `NavigationManager`.
    b. Zaimplementować metodę `HandleValidSubmitAsync`, która będzie wywoływana po pomyślnej walidacji formularza.
    c. Wewnątrz tej metody, wywołać `_supabaseClient.From<JournalEntry>().Insert(requestModel)`.
    d. Dodać obsługę wskaźnika ładowania (`isLoading`).
    e. Zaimplementować blok `try-catch` do obsługi błędów i logowania.
    f. Po pomyślnym utworzeniu wpisu, zresetować model i/lub przekierować użytkownika do listy wpisów (`/entries`).
4.  **Dodanie walidacji**: Dodać atrybuty `DataAnnotations` (np. `[Required]`) do modelu `CreateJournalEntryRequest` i komponenty `DataAnnotationsValidator` oraz `ValidationSummary` do formularza w `CreateEntry.razor`.
5.  **Dodanie nawigacji**: Dodać link lub przycisk w aplikacji (np. w `NavMenu.razor` lub na stronie głównej), który będzie prowadził do nowej strony tworzenia wpisu.
