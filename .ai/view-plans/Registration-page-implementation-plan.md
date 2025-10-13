````markdown
# Plan implementacji widoku Rejestracja

## 1. Przegląd
Widok Rejestracji (`/register`) ma na celu umożliwienie nowym użytkownikom stworzenie osobistego konta w aplikacji 10xJournal. Proces ten wymaga podania adresu e-mail oraz bezpiecznego hasła. Widok musi być prosty, intuicyjny i zgodny z minimalistyczną filozofią aplikacji, a także zapewniać jasne komunikaty zwrotne dotyczące walidacji i ewentualnych błędów.

## 2. Routing widoku
Widok będzie dostępny pod ścieżką `/register`. Należy zdefiniować odpowiednią regułę routingu na poziomie komponentu strony.

```csharp
// RegisterPage.razor
@page "/register"
```

## 3\. Struktura komponentów

Hierarchia komponentów zostanie zorganizowana w celu oddzielenia odpowiedzialności i promowania reużywalności.

  * `RegisterPage.razor` (Komponent-strona)
      * `RegisterForm.razor` (Komponent formularza)
          * `h1` ("Załóż konto")
          * Standardowy element `input` dla adresu e-mail
          * Standardowy element `input` dla hasła
          * Standardowy element `input` dla potwierdzenia hasła
          * `button` ("Załóż konto")
          * Element do wyświetlania komunikatów o błędach

## 4\. Szczegóły komponentów

### `RegisterPage.razor`

  * **Opis komponentu:** Główny komponent strony, odpowiedzialny za routing i hostowanie formularza rejestracji. Zarządza nawigacją po pomyślnej rejestracji.
  * **Główne elementy:** Zawiera komponent `<RegisterForm />`.
  * **Obsługiwane interakcje:** Odbiera zdarzenie pomyślnej rejestracji od `RegisterForm` i przekierowuje użytkownika na stronę z informacją o konieczności potwierdzenia adresu e-mail.
  * **Obsługiwana walidacja:** Brak.
  * **Typy:** Brak.
  * **Propsy:** Brak.

### `RegisterForm.razor`

  * **Opis komponentu:** Sercem widoku jest formularz, który zarządza stanem wprowadzanych danych, obsługuje walidację po stronie klienta, komunikuje się z serwisem autentykacji i wyświetla komunikaty zwrotne.
  * **Główne elementy:**
      * `<EditForm>`: Główny komponent Blazora do obsługi formularzy.
      * `<DataAnnotationsValidator />`: Włącza walidację opartą na atrybutach w modelu.
      * `<ValidationSummary />`: Opcjonalnie, do wyświetlania listy błędów.
      * `<InputText type="email">`: Pole do wprowadzania adresu e-mail.
      * `<InputText type="password">`: Pole do wprowadzania hasła.
      * `<InputText type="password">`: Pole do potwierdzenia hasła.
      * `<button type="submit">`: Przycisk do wysłania formularza.
      * Element `div` lub `p` do wyświetlania błędów z API.
  * **Obsługiwane interakcje:**
      * `OnValidSubmit`: Wywoływane po pomyślnym zwalidowaniu formularza po stronie klienta. Uruchamia logikę rejestracji użytkownika.
  * **Obsługiwana walidacja:**
      * **Email:**
          * Pole jest wymagane (`[Required]`).
          * Musi być poprawnym formatem adresu e-mail (`[EmailAddress]`).
      * **Hasło:**
          * Pole jest wymagane (`[Required]`).
          * Musi mieć co najmniej 6 znaków (`[MinLength(6)]`).
      * **Potwierdzenie hasła:**
          * Pole jest wymagane (`[Required]`).
          * Musi być identyczne z polem hasła (`[Compare("Password")]`).
  * **Typy:** `RegisterViewModel`.
  * **Propsy:** `[Parameter] public EventCallback OnRegisterSuccess { get; set; }`.

## 5\. Typy

Do obsługi formularza zostanie stworzona dedykowana klasa ViewModel.

  * **`RegisterViewModel`**
      * **Cel:** Model danych dla formularza rejestracji, zawierający logikę walidacji.
      * **Pola:**
          * `public string Email { get; set; }`
              * **Atrybuty:** `[Required(ErrorMessage = "Adres e-mail jest wymagany.")]`, `[EmailAddress(ErrorMessage = "Proszę podać poprawny adres e-mail.")]`
          * `public string Password { get; set; }`
              * **Atrybuty:** `[Required(ErrorMessage = "Hasło jest wymagane.")]`, `[MinLength(6, ErrorMessage = "Hasło musi mieć co najmniej 6 znaków.")]`
          * `public string ConfirmPassword { get; set; }`
              * **Atrybuty:** `[Required(ErrorMessage = "Potwierdzenie hasła jest wymagane.")]`, `[Compare(nameof(Password), ErrorMessage = "Hasła nie są identyczne.")]`

## 6\. Zarządzanie stanem

Zarządzanie stanem będzie realizowane wewnątrz komponentu `RegisterForm.razor`. Nie ma potrzeby stosowania zewnętrznej biblioteki do zarządzania stanem.

  * **Zmienne stanu:**
      * `private RegisterViewModel model = new();`: Instancja modelu widoku, powiązana z polami formularza za pomocą dyrektywy `@bind`.
      * `private bool isLoading = false;`: Flaga logiczna wskazująca, czy trwa operacja komunikacji z API. Służy do wyłączenia formularza i wyświetlenia wskaźnika ładowania na przycisku.
      * `private string? apiErrorMessage = null;`: Przechowuje komunikaty o błędach zwrócone przez API (np. "Użytkownik już istnieje"), które są wyświetlane w interfejsie.

## 7\. Integracja API

Integracja z Supabase będzie realizowana poprzez wstrzykiwany serwis `IAuthService`, który będzie abstrakcją nad klientem Supabase C\#.

  * **Endpoint:** Rejestracja użytkownika w Supabase.
  * **Metoda w serwisie:** `Task RegisterAsync(string email, string password)`
  * **Wywołanie SDK Supabase:** `await supabaseClient.Auth.SignUpAsync(email, password);`
  * **Typy żądania:** Metoda `SignUpAsync` przyjmuje `email` (string) i `password` (string).
  * **Typy odpowiedzi:**
      * **Sukces:** `Supabase.Gotrue.Session` - obiekt sesji, który potwierdza pomyślną rejestrację.
      * **Błąd:** Wyjątek typu `Supabase.Gotrue.Exceptions.GotrueException`, zawierający szczegóły błędu.

## 8\. Interakcje użytkownika

  * **Wprowadzanie danych:** Użytkownik wypełnia pola formularza. Dzięki dwukierunkowemu wiązaniu danych (`@bind`) stan `RegisterViewModel` jest na bieżąco aktualizowany. Walidacja jest uruchamiana w czasie rzeczywistym po opuszczeniu pola.
  * **Wysłanie formularza:**
      * Użytkownik klika przycisk "Załóż konto".
      * Jeśli walidacja klienta nie powiedzie się, wyświetlane są komunikaty pod odpowiednimi polami, a wysłanie jest blokowane.
      * Jeśli walidacja klienta powiedzie się, stan `isLoading` zmienia się na `true`, przycisk jest blokowany, a do `IAuthService` wysyłane jest żądanie rejestracji.
  * **Rezultat operacji:**
      * **Sukces:** `isLoading` wraca na `false`, a komponent `RegisterPage` przekierowuje użytkownika na stronę z podziękowaniem i informacją o konieczności weryfikacji e-mail.
      * **Błąd:** `isLoading` wraca na `false`, a zmienna `apiErrorMessage` jest ustawiana na podstawie błędu z API, co powoduje wyświetlenie komunikatu w UI.

## 9\. Warunki i walidacja

Walidacja jest kluczowym elementem zapewniającym poprawność danych i dobre doświadczenie użytkownika.

  * **Walidacja po stronie klienta (Client-Side):**
      * Realizowana przez `DataAnnotationsValidator` i atrybuty w `RegisterViewModel`.
      * Sprawdza format e-mail, minimalną długość hasła, zgodność haseł i wymagane pola.
      * Zapobiega wysyłaniu niepoprawnych żądań do serwera, oszczędzając zasoby i dając natychmiastową informację zwrotną.
  * **Walidacja po stronie serwera (Server-Side):**
      * Realizowana przez Supabase.
      * Weryfikuje unikalność adresu e-mail.
      * Weryfikuje siłę hasła zgodnie z regułami projektu Supabase.
      * Wynik tej walidacji jest obsługiwany w bloku `catch` po wywołaniu API.

## 10\. Obsługa błędów

Komponent `RegisterForm` musi być przygotowany na obsługę różnych scenariuszy błędów.

  * **Błędy walidacji:** Obsługiwane przez standardowe mechanizmy Blazor (`ValidationMessage`).
  * **Błędy API:**
      * **Użytkownik już istnieje:** W bloku `catch` należy zidentyfikować ten konkretny błąd i ustawić `apiErrorMessage` na "Użytkownik o tym adresie e-mail już istnieje.".
      * **Zbyt słabe hasło:** Jeśli reguły serwera są bardziej rygorystyczne niż klienta, API zwróci błąd. Należy go obsłużyć, wyświetlając komunikat "Podane hasło jest zbyt słabe.".
      * **Błędy sieciowe/nieoczekiwane:** W przypadku utraty połączenia lub błędu 500 serwera, należy wyświetlić generyczny komunikat, np. "Wystąpił nieoczekiwany błąd. Spróbuj ponownie później." i zalogować szczegóły błędu w systemie monitoringu.

## 11\. Kroki implementacji

1.  **Stworzenie `RegisterViewModel`:** Zdefiniuj klasę `RegisterViewModel.cs` z polami `Email`, `Password`, `ConfirmPassword` i odpowiednimi atrybutami `DataAnnotations`.
2.  **Stworzenie `RegisterPage.razor`:** Utwórz komponent strony z dyrektywą `@page "/register"` i umieść w nim placeholder dla formularza.
3.  **Implementacja `IAuthService`:** Dodaj metodę `RegisterAsync` w interfejsie i jej implementację w klasie `AuthService`, która wywołuje `supabaseClient.Auth.SignUpAsync`.
4.  **Budowa `RegisterForm.razor`:**
      * Stwórz strukturę formularza przy użyciu `<EditForm>` i powiąż go z instancją `RegisterViewModel`.
      * Dodaj komponenty `<InputText>` dla każdego pola i powiąż je z właściwościami modelu za pomocą `@bind-Value`.
      * Dodaj `<DataAnnotationsValidator />`.
      * Zaimplementuj logikę w metodzie `HandleRegistration` wywoływanej przez `OnValidSubmit`.
5.  **Obsługa stanu ładowania i błędów:**
      * Wprowadź zmienne `isLoading` i `apiErrorMessage`.
      * W metodzie `HandleRegistration` opakuj wywołanie API w blok `try...catch`.
      * Ustawiaj `isLoading` na `true` przed wywołaniem i na `false` w blokach `finally` lub `catch` i po sukcesie.
      * W bloku `catch` analizuj wyjątek i ustawiaj odpowiedni `apiErrorMessage`.
      * Użyj dyrektywy `@if` w UI, aby warunkowo wyświetlać wskaźnik ładowania i komunikaty o błędach.
6.  **Nawigacja po sukcesie:**
      * Zdefiniuj `EventCallback OnRegisterSuccess` w `RegisterForm`.
      * Wywołaj go w bloku `try` po udanej rejestracji.
      * W `RegisterPage`, obsłuż to zdarzenie i użyj `NavigationManager` do przekierowania użytkownika (np. na stronę `/registration-success`).
7.  **Stylowanie:** Upewnij się, że formularz używa standardowych, semantycznych tagów HTML, aby Pico.css mógł je poprawnie ostylować bez potrzeby dodawania własnych klas CSS.