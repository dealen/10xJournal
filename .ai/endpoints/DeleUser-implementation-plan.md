# API Endpoint Implementation Plan: `POST /rpc/delete_my_account`

## 1. Przegląd punktu końcowego
Celem tego punktu końcowego jest zapewnienie bezpiecznej metody trwałego usunięcia konta użytkownika i wszystkich powiązanych z nim danych. Operacja jest realizowana poprzez wywołanie funkcji `delete_my_account` w PostgreSQL (RPC), która usuwa rekord użytkownika z tabeli `auth.users`. Dzięki kaskadowym ograniczeniom klucza obcego (`ON DELETE CASCADE`), usunięcie to automatycznie propaguje się do tabel `profiles`, `journal_entries` i `user_streaks`, zapewniając pełną spójność danych. Ze względu na swoją nieodwracalną naturę, operacja ta musi być wywoływana po stronie klienta z najwyższą ostrożnością, wymagając wyraźnego potwierdzenia od użytkownika.

## 2. Szczegóły żądania
- **Metoda HTTP**: `POST`
- **Struktura URL**: `/rpc/delete_my_account`
- **Parametry**:
  - **Wymagane**: Brak.
  - **Opcjonalne**: Brak.
- **Request Body**: Ciało żądania jest puste (`N/A`). Operacja jest wykonywana w kontekście użytkownika uwierzytelnionego za pomocą tokenu JWT.

## 3. Wykorzystywane typy
- **Model Polecenia (Command Model)**:
  - `DeleteMyAccountRequest.cs`: Zgodnie z architekturą Vertical Slice, ten model będzie używany do hermetyzacji logiki operacji, nawet jeśli nie zawiera żadnych właściwości. Znajduje się w `Features/Settings/Models/`.

## 4. Szczegóły odpowiedzi
- **Odpowiedź sukcesu**:
  - **Kod statusu**: `200 OK`
  - **Ciało odpowiedzi**:
    ```json
    {
        "status": "success",
        "message": "Account and all associated data have been permanently deleted."
    }
    ```
- **Odpowiedzi błędów**:
  - **Kod statusu**: `401 Unauthorized` - Występuje, gdy token JWT jest nieprawidłowy, wygasł lub go brakuje.
  - **Kod statusu**: `500 Internal Server Error` - Może wystąpić w przypadku nieoczekiwanego błędu po stronie bazy danych podczas wykonywania funkcji RPC.

## 5. Przepływ danych
1.  Użytkownik w interfejsie Blazor (`Settings.razor`) inicjuje akcję usunięcia konta.
2.  Interfejs użytkownika wyświetla modalne okno dialogowe z prośbą o ostateczne potwierdzenie, jasno informując o nieodwracalności operacji.
3.  Po potwierdzeniu, komponent Blazor wywołuje metodę w dedykowanej klasie obsługi (np. `DeleteMyAccountHandler.cs`) wewnątrz swojego slice'a.
4.  Handler, używając klienta Supabase (`Supabase.Client`), wykonuje wywołanie RPC do funkcji `delete_my_account`.
5.  Klient Supabase automatycznie dołącza nagłówek `Authorization` z tokenem JWT bieżącej sesji.
6.  Supabase PostgREST odbiera żądanie, weryfikuje token JWT i wywołuje funkcję `delete_my_account()` w PostgreSQL.
7.  Funkcja `delete_my_account()` pobiera `user_id` z `auth.uid()` i wykonuje `DELETE FROM auth.users WHERE id = auth.uid()`.
8.  Baza danych, dzięki regułom `ON DELETE CASCADE`, automatycznie usuwa powiązane rekordy z `public.profiles`, `public.journal_entries` i `public.user_streaks`.
9.  Funkcja RPC zwraca obiekt JSON z komunikatem o sukcesie.
10. Klient Supabase otrzymuje odpowiedź `200 OK` i przekazuje ją do handlera.
11. Handler informuje komponent UI o pomyślnym usunięciu konta.
12. Komponent UI wylogowuje użytkownika i przekierowuje go na stronę główną.

## 6. Względy bezpieczeństwa
- **Uwierzytelnianie**: Każde żądanie musi być uwierzytelnione za pomocą ważnego tokenu JWT. Supabase obsługuje to automatycznie.
- **Autoryzacja**: Funkcja `delete_my_account` operuje wyłącznie na identyfikatorze użytkownika (`auth.uid()`) pobranym z tokenu JWT, co uniemożliwia jednemu użytkownikowi usunięcie konta innego użytkownika.
- **Ochrona CSRF**: Operacja musi być inicjowana za pomocą formularza Blazor (`EditForm`) z metodą `POST`, aby korzystać z wbudowanych mechanizmów ochrony przed atakami Cross-Site Request Forgery.
- **Potwierdzenie użytkownika**: Interfejs użytkownika musi implementować mechanizm "twardego" potwierdzenia (np. wpisanie frazy "usuń moje konto"), aby zapobiec przypadkowemu usunięciu.

## 7. Rozważania dotyczące wydajności
- Operacja usuwania jest transakcyjna i może zająć chwilę, jeśli użytkownik ma bardzo dużą liczbę wpisów w dzienniku.
- Interfejs użytkownika powinien blokować dalsze akcje i wyświetlać wskaźnik ładowania (np. spinner) na czas trwania operacji, aby zapewnić dobre wrażenia użytkownika i zapobiec podwójnemu wywołaniu.
- Nie przewiduje się znaczących wąskich gardeł wydajnościowych, ponieważ operacja jest jednorazowa i rzadko używana, a indeksy na kluczach obcych zapewniają efektywne kaskadowe usuwanie.

## 8. Etapy wdrożenia
1.  **Baza danych (SQL)**:
    -   Utworzyć plik migracji Supabase dla funkcji `delete_my_account`.
    -   Zaimplementować funkcję `delete_my_account()` w PostgreSQL. Funkcja powinna być zdefiniowana z `SECURITY DEFINER`, aby mieć uprawnienia do modyfikacji tabeli `auth.users`.
    -   Funkcja powinna pobierać ID użytkownika z `auth.uid()` i wykonywać operację `DELETE`.
    -   Wdrożyć migrację do lokalnego środowiska deweloperskiego Supabase.

2.  **Frontend (Blazor - Slice `Features/Settings/DeleteAccount/`)**:
    -   Utworzyć nowy slice `Features/Settings/DeleteAccount/`.
    -   Przenieść istniejący model `DeleteMyAccountRequest.cs` do `Features/Settings/DeleteAccount/Models/`.
    -   Stworzyć komponent `DeleteAccount.razor` wewnątrz nowego slice'a.
    -   W komponencie `DeleteAccount.razor` zaimplementować interfejs użytkownika z przyciskiem "Usuń konto", który otwiera modalne okno dialogowe.
    -   W oknie dialogowym dodać pole tekstowe wymagające od użytkownika wpisania frazy potwierdzającej oraz przycisk ostatecznego usunięcia.
    -   W bloku `@code` (lub w dedykowanym handlerze `DeleteAccountHandler.cs`) zaimplementować logikę wywołania RPC:
        -   Wstrzyknąć `Supabase.Client` i `ILogger`.
        -   Stworzyć metodę `HandleDeleteAsync`.
        -   W bloku `try-catch` wywołać `await supabase.Rpc("delete_my_account", null)`.
        -   W przypadku sukcesu, wylogować użytkownika i przekierować go.
        -   W przypadku błędu, zalogować wyjątek i wyświetlić użytkownikowi stosowny komunikat.

3.  **Integracja**:
    -   Dodać nowo utworzony komponent `DeleteAccount.razor` do strony `Settings.razor`.
    -   Dokładnie przetestować cały przepływ: wyświetlenie modala, walidację potwierdzenia, pomyślne usunięcie, obsługę błędów (np. poprzez tymczasowe wyłączenie sieci).
    -   Sprawdzić w bazie danych, czy po operacji wszystkie dane użytkownika zostały poprawnie usunięte.
