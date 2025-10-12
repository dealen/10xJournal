# Plan implementacji widoku Logowanie

## 1. Przegląd
Widok logowania (`/login`) stanowi punkt wejścia dla zarejestrowanych użytkowników aplikacji 10xJournal. Jego głównym celem jest bezpieczne uwierzytelnienie użytkownika na podstawie adresu e-mail i hasła oraz przekierowanie go do głównego interfejsu aplikacji po pomyślnej weryfikacji. Widok ten musi być prosty, responsywny i zapewniać czytelne komunikaty o błędach.

## 2. Routing widoku
Widok będzie dostępny pod ścieżką URL: `/login`. Komponentem odpowiedzialnym za ten routing będzie `LoginPage.razor`, oznaczony dyrektywą `@page "/login"`.

## 3. Struktura komponentów
Struktura będzie prosta i skoncentrowana na jednym zadaniu. Główny komponent strony będzie zawierał formularz logowania.

```

  - LoginPage.razor (@page "/login")
      - h1 ("Zaloguj się")
      - p (Tekst z linkiem do strony rejestracji `/register`)
      - LoginForm.razor (Komponent formularza)
          - EditForm (Komponent Blazor)
              - Pole na adres e-mail (InputText)
              - Pole na hasło (InputText type="password")
              - Przycisk "Zaloguj się" (Button)
              - Link "Nie pamiętam hasła" (a href="/reset-password")

```

## 4. Szczegóły komponentów
### LoginPage.razor
-   **Opis komponentu**: Jest to routowalna strona, która pełni rolę kontenera dla widoku logowania. Wyświetla nagłówek i osadza w sobie komponent `LoginForm.razor`. Może również obsługiwać zdarzenia z komponentu potomnego w celu nawigacji.
-   **Główne elementy**: `<h1>`, `<p>`, komponent `<LoginForm />`.
-   **Obsługiwane interakcje**: Brak bezpośrednich interakcji; deleguje je do `LoginForm`. Po otrzymaniu sygnału o pomyślnym zalogowaniu, przekierowuje użytkownika na stronę główną (`/`) za pomocą `NavigationManager`.
-   **Obsługiwana walidacja**: Brak; walidacja odbywa się w `LoginForm`.
-   **Typy**: Brak.
-   **Propsy**: Brak.

### LoginForm.razor
-   **Opis komponentu**: Sercem widoku jest komponent zawierający formularz logowania. Wykorzystuje wbudowany w Blazor komponent `<EditForm>` do obsługi wprowadzania danych, walidacji i przesyłania. Komponent zarządza stanem ładowania i wyświetlaniem błędów.
-   **Główne elementy**: `<EditForm>`, `<DataAnnotationsValidator>`, `<InputText>`, `<ValidationMessage>`, `<button>`, `<a>`.
-   **Obsługiwane interakcje**:
    -   **Wprowadzanie danych**: Użytkownik wpisuje e-mail i hasło.
    -   **Przesłanie formularza**: Kliknięcie przycisku "Zaloguj się" wywołuje zdarzenie `OnValidSubmit` formularza, o ile walidacja po stronie klienta zakończy się sukcesem.
    -   **Przejście do resetowania hasła**: Kliknięcie linku "Nie pamiętam hasła" przenosi na stronę `/reset-password`.
-   **Obsługiwana walidacja**: Walidacja jest realizowana za pomocą atrybutów `Data Annotations` na modelu `LoginModel`.
    -   `Email`: Wymagany (`[Required]`), musi być w poprawnym formacie (`[EmailAddress]`).
    -   `Password`: Wymagany (`[Required]`).
-   **Typy**: `LoginModel`.
-   **Propsy**: `EventCallback OnLoginSuccess` - zdarzenie wywoływane w komponencie nadrzędnym po pomyślnym zalogowaniu.

## 5. Typy
Do implementacji widoku logowania wymagany jest jeden niestandardowy typ ViewModel.

### LoginModel.cs
Będzie to klasa C# używana do powiązania danych z formularzem `<EditForm>` i do zdefiniowania reguł walidacji.

```csharp
using System.ComponentModel.DataAnnotations;

public class LoginModel
{
    [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
    [EmailAddress(ErrorMessage = "Proszę podać poprawny adres e-mail.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hasło jest wymagane.")]
    public string Password { get; set; } = string.Empty;
}
```

## 6\. Zarządzanie stanem

Zarządzanie stanem będzie realizowane lokalnie wewnątrz komponentu `LoginForm.razor`. Nie ma potrzeby stosowania globalnego zarządcy stanu dla tego widoku.
  - **`private LoginModel loginModel = new();`**: Instancja modelu widoku powiązana z polami formularza.
  - **`private bool isBusy = false;`**: Wartość logiczna (`bool`) określająca, czy żądanie logowania jest w toku. Służy do dezaktywacji przycisku przesyłania i ewentualnego wyświetlania wskaźnika ładowania, aby zapobiec wielokrotnemu przesyłaniu formularza.
  - **`private string? errorMessage = null;`**: Ciąg znaków (`string`) przechowujący komunikat o błędzie z serwera (np. "Nieprawidłowe dane logowania"), który jest wyświetlany użytkownikowi.

## 7\. Integracja API

Integracja z backendem (Supabase) będzie realizowana za pomocą oficjalnej biblioteki klienckiej Supabase dla .NET. Klient Supabase powinien być wstrzykiwany do komponentu `LoginForm.razor` za pomocą mechanizmu DI (`@inject Supabase.Client SupabaseClient`).

  - **Endpoint**: Logowanie użytkownika.
  - **Metoda SDK**: `SupabaseClient.Auth.SignIn(email, password)`
  - **Typ żądania (parametry)**:
      - `email` (string): Adres e-mail użytkownika z `loginModel.Email`.
      - `password` (string): Hasło użytkownika z `loginModel.Password`.
  - **Typ odpowiedzi (sukces)**: `Supabase.Gotrue.Session`. Biblioteka kliencka automatycznie zarządza sesją (przechowuje tokeny). Aplikacja musi jedynie zareagować na pomyślne zakończenie wywołania.
  - **Odpowiedź (błąd)**: Metoda rzuca wyjątek, najczęściej typu `GotrueException`, który zawiera komunikat o błędzie.

## 8\. Interakcje użytkownika

  - **Użytkownik wypełnia formularz i klika "Zaloguj się"**:
    1.  Wywoływane jest zdarzenie `OnValidSubmit` formularza.
    2.  Zmienna `isBusy` ustawiana jest na `true`, co dezaktywuje przycisk.
    3.  Wykonywane jest wywołanie asynchroniczne `SupabaseClient.Auth.SignIn()`.
    4.  W przypadku sukcesu, wywoływany jest `EventCallback OnLoginSuccess`, a komponent nadrzędny (`LoginPage`) przekierowuje użytkownika na stronę główną.
    5.  W przypadku błędu, wyjątek jest przechwytywany, a jego komunikat jest przypisywany do zmiennej `errorMessage` i wyświetlany w UI.
    6.  Niezależnie od wyniku, w bloku `finally` zmienna `isBusy` jest ustawiana z powrotem na `false`, odblokowując przycisk.
  - **Użytkownik klika link "Nie pamiętam hasła"**:
    1.  Następuje nawigacja do ścieżki `/reset-password` za pomocą standardowego tagu `<a>`.

## 9\. Warunki i walidacja

  - **Puste pola**: Walidacja `[Required]` zapobiega przesłaniu formularza, jeśli pole e-mail lub hasła jest puste. Komponent `<ValidationMessage>` wyświetli odpowiedni komunikat z `ErrorMessage`.
  - **Niepoprawny format e-mail**: Walidacja `[EmailAddress]` sprawdza format adresu e-mail po stronie klienta.
  - **Stan przycisku**: Przycisk "Zaloguj się" powinien mieć atrybut `disabled` powiązany ze zmienną `isBusy`, aby zapobiec ponownemu przesłaniu formularza w trakcie przetwarzania żądania logowania.

## 10\. Obsługa błędów

Obsługa błędów powinna być zrealizowana w bloku `try-catch` wokół wywołania API.

  - **Nieprawidłowe dane logowania**: Przechwycenie `GotrueException` i wyświetlenie użytkownikowi ogólnego komunikatu, np. "Nieprawidłowy e-mail lub hasło.".
  - **Błąd sieci lub serwera**: Przechwycenie generycznego wyjątku (`Exception`) i wyświetlenie komunikatu, np. "Wystąpił błąd połączenia. Spróbuj ponownie później.". Pełna treść wyjątku powinna zostać zarejestrowana za pomocą Seriloga w celu późniejszej analizy.
  - **Wyświetlanie błędu**: Komunikat o błędzie przechowywany w zmiennej `errorMessage` powinien być wyświetlany w widocznym miejscu formularza, np. nad przyciskiem logowania. Powinien on być renderowany warunkowo, tylko jeśli nie jest `null` lub pusty.

## 11\. Kroki implementacji

1.  **Stworzenie modelu widoku**: Utwórz plik `LoginModel.cs` z właściwościami `Email` i `Password` oraz odpowiednimi atrybutami `DataAnnotations`.
2.  **Konfiguracja DI dla Supabase**: Upewnij się, że w pliku `Program.cs` klient Supabase jest zarejestrowany jako usługa (singleton), aby można go było wstrzykiwać do komponentów.
3.  **Implementacja komponentu `LoginForm.razor`**:
      - Dodaj wstrzykiwanie zależności: `@inject Supabase.Client SupabaseClient` oraz `@inject NavigationManager NavigationManager`.
      - Zdefiniuj w bloku `@code` zmienne stanu: `loginModel`, `isBusy`, `errorMessage`.
      - Zbuduj strukturę HTML formularza przy użyciu komponentu `<EditForm>` powiązanego z `loginModel`.
      - Użyj komponentów `<InputText>`, `<DataAnnotationsValidator>` i `<ValidationMessage>` dla pól i walidacji.
      - Dodaj przycisk `<button>` z atrybutem `disabled="@isBusy"` i link `<a>` do resetowania hasła.
      - Zaimplementuj metodę `HandleValidSubmit` wywoływaną przez `OnValidSubmit` formularza.
      - Wewnątrz `HandleValidSubmit`, zaimplementuj logikę wywołania `SupabaseClient.Auth.SignIn()` w bloku `try-catch-finally`.
4.  **Implementacja strony `LoginPage.razor`**:
      - Dodaj dyrektywę `@page "/login"`.
      - Umieść komponent `<LoginForm />` oraz dodaj nagłówek `<h1>` i inne elementy statyczne.
      - Zaimplementuj obsługę zdarzenia `OnLoginSuccess` z `LoginForm` (jeśli jest potrzebna separacja logiki), aby wykonać `NavigationManager.NavigateTo("/")`.
5.  **Stylowanie**: Upewnij się, że używane są semantyczne tagi HTML, które będą poprawnie stylowane przez Pico.css, zachowując minimalistyczny wygląd.
6.  **Testowanie**: Przetestuj ręcznie wszystkie scenariusze: pomyślne logowanie, błędne dane, puste pola oraz działanie linku do resetowania hasła. Sprawdź responsywność widoku na różnych szerokościach ekranu.
