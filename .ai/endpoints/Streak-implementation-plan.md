# API Endpoint Implementation Plan: Get User Streak

## 1. Przegląd punktu końcowego
Celem tego punktu końcowego jest pobranie danych dotyczących serii pisania (streaka) dla aktualnie uwierzytelnionego użytkownika. Dane te obejmują obecną serię, najdłuższą serię oraz datę ostatniego wpisu. Punkt końcowy jest udostępniany przez auto-generowane API PostgREST w Supabase i zabezpieczony za pomocą polityk RLS. Implementacja po stronie klienta będzie polegać na wywołaniu tego punktu końcowego i obsłudze odpowiedzi.

## 2. Szczegóły żądania
- **Metoda HTTP**: `GET`
- **Struktura URL**: `/rest/v1/user_streaks`
- **Parametry**:
  - **Wymagane (Query)**: `select=*` - Zapewnia, że wszystkie kolumny z tabeli `user_streaks` zostaną zwrócone w odpowiedzi.
  - **Wymagane (Nagłówki)**:
    - `Authorization: Bearer <jwt_token>` - Token uwierzytelniający użytkownika.
    - `ApiKey: <supabase_anon_key>` - Klucz publiczny (anon) projektu Supabase.
- **Request Body**: Brak.

## 3. Wykorzystywane typy
Do deserializacji odpowiedzi z API zostanie wykorzystany istniejący model DTO:
- **`10xJournal.Client.Features.Settings.Models.UserStreak`**
  ```csharp
  // Located at: 10xJournal.Client/Features/Settings/Models/UserStreak.cs
  public class UserStreak
  {
      [JsonPropertyName("user_id")]
      public Guid UserId { get; set; }

      [JsonPropertyName("current_streak")]
      public int CurrentStreak { get; set; }

      [JsonPropertyName("longest_streak")]
      public int LongestStreak { get; set; }

      [JsonPropertyName("last_entry_date")]
      public DateOnly? LastEntryDate { get; set; }
  }
  ```

## 4. Szczegóły odpowiedzi
- **Odpowiedź sukcesu (`200 OK`)**:
  - **Struktura**: Tablica JSON zawierająca jeden obiekt `UserStreak`.
  - **Przykład**:
    ```json
    [
        {
            "user_id": "f1g2h3i4-j5k6-l7m8-n9o0-p1q2r3s4t5u6",
            "current_streak": 5,
            "longest_streak": 12,
            "last_entry_date": "2025-10-11"
        }
    ]
    ```
- **Odpowiedzi błędów**:
  - `401 Unauthorized`: Występuje, gdy token JWT jest nieprawidłowy, wygasł lub go brakuje.
  - `5xx Server Error`: W przypadku wewnętrznego błędu po stronie Supabase.

## 5. Przepływ danych
1. Komponent Blazor (np. `NavMenu.razor`) inicjuje żądanie pobrania danych o "streaku".
2. Za pomocą wstrzykniętego klienta Supabase (`Supabase.Client`) zostaje wywołana metoda `From<UserStreak>().Get()`.
3. Biblioteka klienta Supabase konstruuje żądanie `GET` do `/rest/v1/user_streaks?select=*`, dołączając wymagane nagłówki `Authorization` i `ApiKey`.
4. Supabase (PostgREST) odbiera żądanie i weryfikuje token JWT.
5. Polityka RLS (Row Level Security) na tabeli `user_streaks` jest egzekwowana, filtrując wyniki tak, aby zwrócić tylko rekord należący do uwierzytelnionego użytkownika (`auth.uid() = user_id`).
6. Baza danych zwraca pasujący wiersz do PostgREST.
7. PostgREST formatuje wynik jako tablicę JSON i wysyła odpowiedź `200 OK` do klienta.
8. Klient Blazor deserializuje odpowiedź do obiektu `Postgrest.Responses.ModelResponse<UserStreak>`, z którego pobierany jest pierwszy element.
9. Dane są wyświetlane w interfejsie użytkownika.

## 6. Względy bezpieczeństwa
- **Uwierzytelnianie**: Każde żądanie musi być uwierzytelnione za pomocą ważnego tokenu JWT. Klient Supabase zarządza tym automatycznie.
- **Autoryzacja**: Dostęp do danych jest ściśle kontrolowany przez politykę RLS w PostgreSQL, która jest fundamentalnym elementem bezpieczeństwa. Gwarantuje ona, że użytkownik ma dostęp **wyłącznie** do swojego rekordu `user_streaks`.
- **Transport**: Cała komunikacja z Supabase musi odbywać się przez HTTPS, aby chronić tokeny i dane w transporcie.

## 7. Obsługa błędów
- **Brak autoryzacji (`401 Unauthorized`)**: W bloku `catch (Gotrue.Client.Exceptions.GotrueException)` należy obsłużyć błąd autoryzacji. Aplikacja powinna potraktować to jako wylogowanie użytkownika i potencjalnie przekierować go na stronę logowania.
- **Błędy sieciowe i inne wyjątki**: Ogólny blok `catch (Exception ex)` powinien logować błąd za pomocą `ILogger` w celu ułatwienia diagnostyki. W UI można wyświetlić dyskretny komunikat o niemożności zsynchronizowania danych, bez przerywania pracy użytkownika.
- **Brak danych**: Jeśli zapytanie zwróci pustą listę (co w normalnych warunkach nie powinno się zdarzyć po zalogowaniu, dzięki triggerom tworzącym rekord), kod powinien być na to odporny i traktować "streak" jako 0.

## 8. Rozważania dotyczące wydajności
- **Wielkość zapytania**: Zapytanie jest bardzo lekkie, ponieważ pobiera tylko jeden wiersz z tabeli.
- **Indeksy**: Tabela `user_streaks` używa `user_id` jako klucza głównego, co zapewnia błyskawiczny odczyt danych dla konkretnego użytkownika. Nie są wymagane dodatkowe indeksy.
- **Częstotliwość wywołań**: Dane o "streaku" nie zmieniają się często (maksymalnie raz dziennie). Należy rozważyć cachowanie wyniku po stronie klienta na określony czas (np. 15-60 minut) lub do momentu utworzenia nowego wpisu, aby uniknąć niepotrzebnych zapytań do API przy każdej nawigacji.

## 9. Etapy wdrożenia
1. **Identyfikacja komponentu**: Zdecydować, który komponent Blazor będzie odpowiedzialny za pobieranie i wyświetlanie danych o "streaku". Dobrym kandydatem jest `NavMenu.razor`, ponieważ jest on stale widoczny dla zalogowanego użytkownika.
2. **Wstrzyknięcie zależności**: W docelowym komponencie wstrzyknąć `Supabase.Client` oraz `ILogger`.
   ```csharp
   @inject Supabase.Client SupabaseClient
   @inject ILogger<NavMenu> Logger
   ```
3. **Implementacja logiki pobierania danych**:
   - Dodać pole do przechowywania stanu "streaka", np. `private int _currentStreak;`.
   - W metodzie cyklu życia komponentu, np. `OnInitializedAsync`, zaimplementować logikę pobierania danych w bloku `try-catch`.
   - Upewnić się, że zapytanie jest wykonywane tylko dla zalogowanych użytkowników.
   ```csharp
   protected override async Task OnInitializedAsync()
   {
       try
       {
           var response = await SupabaseClient.From<UserStreak>().Get();
           var streakData = response.Models.FirstOrDefault();
           if (streakData != null)
           {
               _currentStreak = streakData.CurrentStreak;
               StateHasChanged(); // Odśwież UI
           }
       }
       catch (Exception ex)
       {
           Logger.LogError(ex, "Error fetching user streak data.");
           // Opcjonalnie: obsłuż błąd w UI
       }
   }
   ```
4. **Wyświetlanie danych**: W części HTML komponentu (`.razor`) dodać element, który będzie wyświetlał wartość `_currentStreak`, np. obok nazwy użytkownika lub jako ikona.
5. **Obsługa stanu uwierzytelnienia**: Zintegrować logikę z `AuthenticationStateProvider`, aby pobierać dane o "streaku" tylko wtedy, gdy użytkownik jest zalogowany, i resetować je po wylogowaniu. Można subskrybować zdarzenie `AuthenticationStateChanged`.
