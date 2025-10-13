# Plan implementacji widoku Resetowanie Hasła

## 1. Przegląd

Widok **Resetowanie Hasła** umożliwia użytkownikom, którzy zapomnieli hasła, zainicjowanie procesu odzyskiwania dostępu do konta. Widok ten jest dostępny dla niezalogowanych użytkowników i stanowi pierwszy krok w procesie resetu hasła - użytkownik podaje swój adres e-mail, a system wysyła na niego link do dalszego procesu resetowania hasła.

Głównym celem tego widoku jest zapewnienie prostego, bezpiecznego i przyjaznego dla użytkownika interfejsu do zgłoszenia zapomnienia hasła, przy jednoczesnym zachowaniu zasad bezpieczeństwa (nie ujawnianie informacji o istnieniu kont).

## 2. Routing widoku

**Ścieżka:** `/reset-password`

**Dostępność:** Widok jest dostępny dla niezauwierzytelnonych użytkowników. Nie wymaga ochrony przez AuthenticationGuard.

**Nawigacja do widoku:**
- Link na stronie logowania (`/login`) - "Zapomniałeś hasła?"
- Bezpośredni dostęp przez URL

## 3. Struktura komponentów

Widok składa się z następujących komponentów w hierarchii:

```
ResetPasswordPage.razor (/reset-password)
│
└── ResetPasswordForm.razor
    ├── <EditForm> (komponent Blazor)
    │   ├── <FluentValidationValidator>
    │   └── <ValidationSummary>
    ├── Pole wprowadzania e-mail
    ├── Przycisk "Wyślij link resetujący"
    └── Komunikaty zwrotne
        ├── Komunikat sukcesu (warunkowy)
        ├── Komunikat błędu (warunkowy)
        └── Wskaźnik ładowania (warunkowy)
```

## 4. Szczegóły komponentów

### ResetPasswordPage.razor

**Opis komponentu:**
Komponent strony głównej dla funkcjonalności resetowania hasła. Jest to kontener routing'owy, który hostuje formularz resetowania hasła. Odpowiada za ogólny layout strony, tytuł i opis funkcjonalności.

**Główne elementy:**
- Element `<PageTitle>` ustawiający tytuł strony
- Nagłówek strony (`<h1>`) z tytułem "Resetowanie hasła"
- Krótki opis instruujący użytkownika (np. "Podaj adres e-mail powiązany z Twoim kontem. Wyślemy Ci link do zresetowania hasła.")
- Komponent `<ResetPasswordForm>` zawierający logikę formularza
- Link powrotny do strony logowania

**Obsługiwane interakcje:**
- Brak - komponenty dziecko obsługują wszystkie interakcje

**Obsługiwana walidacja:**
- Brak - walidacja odbywa się w komponencie formularza

**Typy:**
- Brak dedykowanych typów

**Propsy:**
- Brak - komponent nie przyjmuje parametrów

---

### ResetPasswordForm.razor

**Opis komponentu:**
Główny komponent formularza odpowiedzialny za zbieranie adresu e-mail użytkownika i inicjowanie procesu resetowania hasła poprzez wywołanie API Supabase. Zarządza stanem formularza, walidacją, wywołaniami API oraz wyświetlaniem komunikatów zwrotnych dla użytkownika.

**Główne elementy:**
- `<EditForm>` z modelem `ResetPasswordModel` i obsługą `OnValidSubmit`
- `<FluentValidationValidator>` do obsługi walidacji FluentValidation
- `<ValidationSummary>` do wyświetlania podsumowania błędów walidacji
- Pole `<input type="email">` dla adresu e-mail z odpowiednim `@bind-Value`
- `<ValidationMessage>` dla pola e-mail
- Przycisk submit (`<button type="submit">`) z dynamicznym tekstem w zależności od stanu ładowania
- Sekcja komunikatów warunkowych:
  - Komunikat sukcesu (gdy `_submitSuccess == true`)
  - Komunikat błędu (gdy `_submitSuccess == false`)
  - Wskaźnik ładowania (gdy `_isSubmitting == true`)

**Obsługiwane interakcje:**
- **OnValidSubmit:** Wywoływane po pomyślnej walidacji formularza po kliknięciu przycisku submit
  - Ustawia `_isSubmitting = true`
  - Wywołuje `_supabaseClient.Auth.ResetPasswordForEmail(_model.Email)`
  - Obsługuje odpowiedź i aktualizuje stan (`_submitSuccess`, `_errorMessage`)
  - Ustawia `_isSubmitting = false`
  - Wywołuje `StateHasChanged()`
- **Input change:** Automatyczne wiązanie danych przez `@bind-Value` w polu e-mail
- **Blur validation:** Walidacja może być uruchamiana po opuszczeniu pola (konfiguracja EditForm)

**Obsługiwana walidacja:**
Walidacja wykonywana przez `ResetPasswordValidator` przy użyciu FluentValidation:

1. **E-mail wymagany:**
   - Warunek: Pole e-mail nie może być puste
   - Komunikat: "Adres e-mail jest wymagany"
   - Typ walidacji: NotEmpty()

2. **Format e-mail:**
   - Warunek: E-mail musi być w prawidłowym formacie (zawierać @, domenę, etc.)
   - Komunikat: "Podaj prawidłowy adres e-mail"
   - Typ walidacji: EmailAddress()

3. **Długość e-mail:**
   - Warunek: Maksymalnie 255 znaków
   - Komunikat: "Adres e-mail jest za długi"
   - Typ walidacji: MaximumLength(255)

**Typy:**
- `ResetPasswordModel` - model wiązania danych formularza
- `Supabase.Client` - klient Supabase do wywołań API (injected)
- `BaseResponse` - typ odpowiedzi z Supabase (implicitly used)

**Propsy:**
- Brak - komponent nie przyjmuje parametrów od rodzica

**Stan komponentu (pola prywatne w @code):**
- `_model` (ResetPasswordModel) - model danych formularza
- `_isSubmitting` (bool) - flaga wskazująca, czy formularz jest w trakcie wysyłania
- `_submitSuccess` (bool?) - wynik wysyłania formularza (null/true/false)
- `_errorMessage` (string?) - szczegółowy komunikat błędu do wyświetlenia

**Dependency Injection:**
- `@inject Supabase.Client SupabaseClient` - klient Supabase do wywołań API
- `@inject NavigationManager NavigationManager` - opcjonalnie, jeśli potrzebna nawigacja

---

### ResetPasswordModel.cs

**Opis:**
Klasa modelu danych (DTO) używana do wiązania danych formularza resetowania hasła. Zawiera właściwości odpowiadające polom formularza.

**Struktura:**
```csharp
public class ResetPasswordModel
{
    public string Email { get; set; } = string.Empty;
}
```

**Pola:**
- `Email` (string) - Adres e-mail użytkownika, który chce zresetować hasło
  - Domyślna wartość: `string.Empty`
  - Walidowany przez `ResetPasswordValidator`

---

### ResetPasswordValidator.cs

**Opis:**
Klasa walidatora wykorzystująca FluentValidation do definiowania reguł walidacji dla `ResetPasswordModel`.

**Struktura:**
```csharp
public class ResetPasswordValidator : AbstractValidator<ResetPasswordModel>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Adres e-mail jest wymagany");
            
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Podaj prawidłowy adres e-mail");
            
        RuleFor(x => x.Email)
            .MaximumLength(255)
            .WithMessage("Adres e-mail jest za długi");
    }
}
```

## 5. Typy

### ResetPasswordModel (DTO)

Model danych formularza do wiązania z polami wejściowymi:

| Pole | Typ | Opis | Walidacja |
|------|-----|------|-----------|
| `Email` | `string` | Adres e-mail użytkownika | Wymagany, format e-mail, max 255 znaków |

**Lokalizacja:** `Features/Authentication/ResetPassword/ResetPasswordModel.cs`

**Użycie:** Wiązanie z `<EditForm Model="@_model">` w `ResetPasswordForm.razor`

---

### Typy z Supabase SDK

Wykorzystywane typy z biblioteki Supabase (nie wymagają definiowania):

**BaseResponse:**
- Typ zwracany przez `Auth.ResetPasswordForEmail()`
- Właściwości:
  - Wskazanie sukcesu/porażki operacji
  - Informacje o błędach (jeśli wystąpiły)

**ResetPasswordOptions:**
- Opcjonalny parametr dla `ResetPasswordForEmail()`
- Właściwości:
  - `RedirectTo` (string) - URL do przekierowania po kliknięciu linku w e-mailu

---

### Typy stanu komponentu (pola prywatne)

| Pole | Typ | Opis | Wartość początkowa |
|------|-----|------|-------------------|
| `_model` | `ResetPasswordModel` | Model danych formularza | `new ResetPasswordModel()` |
| `_isSubmitting` | `bool` | Czy formularz jest wysyłany | `false` |
| `_submitSuccess` | `bool?` | Wynik wysyłania (null/true/false) | `null` |
| `_errorMessage` | `string?` | Komunikat błędu | `null` |

## 6. Zarządzanie stanem

Stan w widoku resetowania hasła jest zarządzany lokalnie w komponencie `ResetPasswordForm.razor` poprzez prywatne pola w bloku `@code`. Nie ma potrzeby stosowania globalnego zarządzania stanem ani niestandardowych hooków dla tego prostego widoku.

### Stan formularza

**_model (ResetPasswordModel):**
- Przechowuje dane wprowadzone przez użytkownika
- Automatycznie aktualizowany przez Blazor'owe `@bind-Value` na polu input
- Używany w `<EditForm Model="@_model">`

### Stan interfejsu użytkownika

**_isSubmitting (bool):**
- Wskazuje, czy formularz jest obecnie w trakcie wysyłania do API
- Używany do:
  - Wyłączenia przycisku submit podczas wysyłania
  - Wyświetlenia wskaźnika ładowania
  - Zmiany tekstu przycisku na "Wysyłanie..."
  - Opcjonalnie: wyłączenia pola input podczas wysyłania

**_submitSuccess (bool?):**
- Trójstanowa flaga wskazująca wynik operacji:
  - `null` - formularz nie został jeszcze wysłany
  - `true` - operacja zakończona sukcesem
  - `false` - operacja zakończona błędem
- Używany do warunkowego renderowania komunikatów

**_errorMessage (string?):**
- Przechowuje szczegółowy komunikat błędu do wyświetlenia użytkownikowi
- Ustawiany w bloku `catch` lub po otrzymaniu błędnej odpowiedzi z API
- Wyświetlany w sekcji komunikatów błędów

### Cykl życia komponentu

**OnInitialized:**
```csharp
protected override void OnInitialized()
{
    _model = new ResetPasswordModel();
}
```

**Brak potrzeby:**
- OnParametersSet - brak parametrów routingu
- OnAfterRender - brak manipulacji DOM
- IDisposable - brak subskrypcji do posprzątania

### Aktualizacja stanu

Stan jest aktualizowany w metodzie `HandleValidSubmit()`:

```csharp
private async Task HandleValidSubmit()
{
    _isSubmitting = true;
    _errorMessage = null;
    StateHasChanged(); // Opcjonalnie, aby pokazać loading natychmiast
    
    try 
    {
        var response = await SupabaseClient.Auth.ResetPasswordForEmail(_model.Email);
        
        _submitSuccess = true;
    }
    catch (Exception ex)
    {
        _submitSuccess = false;
        _errorMessage = "Wystąpił błąd. Spróbuj ponownie później.";
        // Logowanie błędu dla celów debugowania
    }
    finally
    {
        _isSubmitting = false;
        StateHasChanged();
    }
}
```

### Resetowanie stanu

Po pomyślnym wysłaniu można rozważyć:
- Pozostawienie formularza w stanie sukcesu (z komunikatem)
- Czyszczenie pola e-mail: `_model.Email = string.Empty;`
- Wyłączenie możliwości ponownego wysłania przez określony czas

## 7. Integracja API

### Endpoint Supabase Auth

Widok integruje się z Supabase Authentication API poprzez metodę `ResetPasswordForEmail` dostępną w kliencie Supabase.

### Metoda API

**Metoda:** `Auth.ResetPasswordForEmail()`

**Sygnatura:**
```csharp
Task<BaseResponse> ResetPasswordForEmail(string email, ResetPasswordOptions? options = null)
```

**Parametry:**

| Parametr | Typ | Wymagany | Opis |
|----------|-----|----------|------|
| `email` | `string` | Tak | Adres e-mail użytkownika, dla którego ma być zresetowane hasło |
| `options` | `ResetPasswordOptions?` | Nie | Opcje dodatkowe, np. URL przekierowania |

**Typ żądania:**
```csharp
// Input z formularza
string email = _model.Email;

// Opcjonalne opcje przekierowania
var options = new ResetPasswordOptions 
{ 
    RedirectTo = "https://twoja-aplikacja.com/update-password" 
};
```

**Typ odpowiedzi:**
```csharp
BaseResponse response
```

Właściwości `BaseResponse` wykorzystywane do określenia sukcesu operacji.

### Implementacja w komponencie

**Dependency Injection:**
```csharp
@inject Supabase.Client SupabaseClient
```

**Wywołanie API:**
```csharp
private async Task HandleValidSubmit()
{
    _isSubmitting = true;
    _errorMessage = null;
    
    try 
    {
        // Wywołanie API Supabase
        var response = await SupabaseClient.Auth.ResetPasswordForEmail(
            _model.Email,
            new ResetPasswordOptions 
            { 
                RedirectTo = "https://twoja-aplikacja.com/update-password" 
            }
        );
        
        // Sprawdzenie sukcesu
        if (response != null)
        {
            _submitSuccess = true;
        }
        else
        {
            _submitSuccess = false;
            _errorMessage = "Nie udało się wysłać linku resetującego. Spróbuj ponownie.";
        }
    }
    catch (HttpRequestException httpEx)
    {
        // Błąd sieci
        _submitSuccess = false;
        _errorMessage = "Nie można połączyć się z serwerem. Sprawdź połączenie internetowe.";
        // Log exception
    }
    catch (Exception ex)
    {
        // Inne błędy
        _submitSuccess = false;
        _errorMessage = "Wystąpił błąd. Spróbuj ponownie później.";
        // Log exception
    }
    finally
    {
        _isSubmitting = false;
        StateHasChanged();
    }
}
```

### Konfiguracja URL przekierowania

URL `RedirectTo` powinien wskazywać na stronę w aplikacji, która obsługuje aktualizację hasła po kliknięciu linku w e-mailu. Ten widok będzie musiał:
- Odczytać token z parametrów URL
- Wyświetlić formularz do wprowadzenia nowego hasła
- Wywołać odpowiednią metodę Supabase do aktualizacji hasła

**Przykład URL:** `https://10xjournal.com/update-password`

### Obsługa odpowiedzi

**Sukces:**
- Supabase wysyła e-mail z linkiem resetującym
- Aplikacja wyświetla komunikat sukcesu
- Użytkownik nie jest przekierowywany (pozostaje na stronie z komunikatem)

**Błąd:**
- Wyświetlenie komunikatu błędu
- Możliwość ponowienia próby
- Szczegóły błędu logowane po stronie aplikacji

### Bezpieczeństwo

**Ważne:** Ze względów bezpieczeństwa, API Supabase nie ujawnia, czy podany adres e-mail istnieje w systemie. Niezależnie od tego, czy konto istnieje, API zwraca sukces. Dlatego komunikat sukcesu powinien brzmieć:

> "Jeśli podany adres e-mail jest zarejestrowany w systemie, otrzymasz link do resetowania hasła. Sprawdź swoją skrzynkę pocztową, w tym folder spam."

## 8. Interakcje użytkownika

### 1. Wejście na stronę resetowania hasła

**Akcja użytkownika:** Nawigacja do `/reset-password` poprzez link "Zapomniałeś hasła?" na stronie logowania lub bezpośredni URL

**Oczekiwany rezultat:**
- Renderowanie strony `ResetPasswordPage`
- Wyświetlenie nagłówka "Resetowanie hasła"
- Wyświetlenie krótkiego opisu instruującego
- Wyświetlenie formularza z pustym polem e-mail
- Przycisk "Wyślij link resetujący" jest aktywny
- Brak komunikatów o błędach lub sukcesach

---

### 2. Wprowadzenie pustego e-maila i próba wysłania

**Akcja użytkownika:** Pozostawienie pola e-mail pustego i kliknięcie przycisku submit

**Oczekiwany rezultat:**
- Walidacja zapobiega wysłaniu formularza
- Pod polem e-mail pojawia się komunikat: "Adres e-mail jest wymagany"
- Pole e-mail może być podświetlone (czerwona ramka)
- Formularz nie wysyła żądania API
- Przycisk submit pozostaje aktywny

---

### 3. Wprowadzenie nieprawidłowego formatu e-mail

**Akcja użytkownika:** Wprowadzenie tekstu bez znaku @ lub w nieprawidłowym formacie (np. "test", "test@", "@example.com")

**Oczekiwany rezultat:**
- Walidacja zapobiega wysłaniu formularza
- Pod polem e-mail pojawia się komunikat: "Podaj prawidłowy adres e-mail"
- Pole e-mail może być podświetlone (czerwona ramka)
- Formularz nie wysyła żądania API
- Użytkownik może poprawić wprowadzony tekst

---

### 4. Wprowadzenie prawidłowego e-maila i kliknięcie przycisku

**Akcja użytkownika:** Wprowadzenie prawidłowego adresu e-mail (np. "user@example.com") i kliknięcie "Wyślij link resetujący"

**Oczekiwany rezultat:**
- Walidacja przechodzi pomyślnie
- Przycisk submit zostaje wyłączony
- Tekst przycisku zmienia się na "Wysyłanie..."
- Pojawia się wskaźnik ładowania (spinner/loader)
- Pole e-mail może zostać wyłączone
- Formularz wywołuje API Supabase: `Auth.ResetPasswordForEmail()`

---

### 5. Sukces - e-mail został wysłany

**Rezultat API:** Supabase zwraca pozytywną odpowiedź

**Oczekiwany rezultat w UI:**
- Wskaźnik ładowania znika
- Pojawia się zielony/niebieski komunikat sukcesu:
  ```
  ✓ Link do resetowania hasła został wysłany!
  
  Jeśli podany adres e-mail jest zarejestrowany w systemie, otrzymasz 
  wiadomość z linkiem do resetowania hasła. Sprawdź swoją skrzynkę 
  pocztową, w tym folder spam.
  
  Link jest ważny przez 24 godziny.
  ```
- Formularz może pozostać widoczny lub być ukryty
- Przycisk może pozostać wyłączony lub zmienić się na "Wyślij ponownie" (po opóźnieniu)
- Wyświetlony jest link "Powrót do logowania" kierujący na `/login`

---

### 6. Błąd - problem z wysłaniem

**Rezultat API:** Supabase zwraca błąd lub następuje wyjątek (brak sieci, timeout, limit żądań)

**Oczekiwany rezultat w UI:**
- Wskaźnik ładowania znika
- Pojawia się czerwony komunikat błędu:
  ```
  ✗ Nie udało się wysłać linku resetującego
  
  [Szczegóły błędu w zależności od przyczyny:]
  - "Nie można połączyć się z serwerem. Sprawdź połączenie internetowe."
  - "Wysłano zbyt wiele próśb. Spróbuj ponownie za kilka minut."
  - "Wystąpił błąd. Spróbuj ponownie później."
  ```
- Przycisk submit zostaje ponownie aktywny
- Pole e-mail pozostaje edytowalne
- Użytkownik może poprawić dane i spróbować ponownie

---

### 7. Kliknięcie "Powrót do logowania"

**Akcja użytkownika:** Kliknięcie linku "Powrót do logowania" (dostępnego zawsze lub po komunikacie sukcesu)

**Oczekiwany rezultat:**
- Nawigacja do strony `/login`
- Opuszczenie widoku resetowania hasła
- Stan formularza jest resetowany

---

### 8. Wielokrotne kliknięcie przycisku submit

**Akcja użytkownika:** Próba wielokrotnego kliknięcia przycisku podczas wysyłania

**Oczekiwany rezultat:**
- Przycisk jest wyłączony podczas `_isSubmitting = true`
- Kolejne kliknięcia są ignorowane
- Tylko jedno żądanie API jest wysyłane
- Zapobiega to zduplikowanym żądaniom

---

### 9. Użycie na urządzeniu mobilnym

**Akcja użytkownika:** Dostęp do strony z telefonu komórkowego

**Oczekiwany rezultat:**
- Responsywny layout dzięki Pico.css
- Pole input e-mail używa `type="email"` co aktywuje odpowiednią klawiaturę mobilną (z klawiszem @)
- Wszystkie elementy są łatwo klikalne (odpowiedni rozmiar dotykowy)
- Tekst jest czytelny bez potrzeby powiększania
- Formularz jest w pełni funkcjonalny

## 9. Warunki i walidacja

### Walidacja na poziomie formularza (FluentValidation)

Wszystkie warunki walidacji są zdefiniowane w `ResetPasswordValidator` i są sprawdzane przez komponent `ResetPasswordForm` przed wysłaniem formularza.

#### Warunek 1: E-mail jest wymagany

**Komponent:** `ResetPasswordForm`

**Reguła walidacji:**
```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .WithMessage("Adres e-mail jest wymagany");
```

**Kiedy jest sprawdzany:**
- Po próbie wysłania formularza
- Opcjonalnie: po opuszczeniu pola (on blur)

**Wpływ na UI:**
- Jeśli pole jest puste: formularz nie jest wysyłany
- Pod polem e-mail wyświetla się komunikat: "Adres e-mail jest wymagany"
- Pole może być oznaczone czerwoną ramką
- `<ValidationMessage For="@(() => _model.Email)">` renderuje komunikat

---

#### Warunek 2: E-mail musi być w prawidłowym formacie

**Komponent:** `ResetPasswordForm`

**Reguła walidacji:**
```csharp
RuleFor(x => x.Email)
    .EmailAddress()
    .WithMessage("Podaj prawidłowy adres e-mail");
```

**Kiedy jest sprawdzany:**
- Po próbie wysłania formularza
- Opcjonalnie: podczas wpisywania lub po opuszczeniu pola

**Wpływ na UI:**
- Jeśli format jest nieprawidłowy (brak @, nieprawidłowa domena): formularz nie jest wysyłany
- Pod polem e-mail wyświetla się komunikat: "Podaj prawidłowy adres e-mail"
- Pole może być oznaczone czerwoną ramką
- Użytkownik musi poprawić format przed wysłaniem

**Przykłady nieprawidłowych formatów:**
- `test` (brak @)
- `test@` (brak domeny)
- `@example.com` (brak nazwy użytkownika)
- `test @example.com` (spacja)

---

#### Warunek 3: E-mail nie może być dłuższy niż 255 znaków

**Komponent:** `ResetPasswordForm`

**Reguła walidacji:**
```csharp
RuleFor(x => x.Email)
    .MaximumLength(255)
    .WithMessage("Adres e-mail jest za długi");
```

**Kiedy jest sprawdzany:**
- Po próbie wysłania formularza
- Opcjonalnie: podczas wpisywania

**Wpływ na UI:**
- Jeśli długość przekracza 255 znaków: formularz nie jest wysyłany
- Pod polem e-mail wyświetla się komunikat: "Adres e-mail jest za długi"
- Użytkownik musi skrócić wprowadzony tekst

---

### Warunki stanu UI (nie związane z walidacją danych)

#### Warunek 4: Formularz jest w trakcie wysyłania

**Komponent:** `ResetPasswordForm`

**Sprawdzany przez:** `_isSubmitting == true`

**Wpływ na UI:**
- Przycisk submit jest wyłączony (`disabled`)
- Tekst przycisku zmienia się z "Wyślij link resetujący" na "Wysyłanie..."
- Wyświetlany jest wskaźnik ładowania (spinner)
- Opcjonalnie: pole e-mail jest wyłączone
- Użytkownik nie może ponownie wysłać formularza do czasu zakończenia operacji

**Implementacja:**
```razor
<button type="submit" disabled="@_isSubmitting">
    @(_isSubmitting ? "Wysyłanie..." : "Wyślij link resetujący")
</button>

@if (_isSubmitting)
{
    <div class="spinner">Wysyłanie...</div>
}
```

---

#### Warunek 5: Formularz został pomyślnie wysłany

**Komponent:** `ResetPasswordForm`

**Sprawdzany przez:** `_submitSuccess == true`

**Wpływ na UI:**
- Wyświetlany jest komunikat sukcesu (zielony/niebieski)
- Komunikat zawiera instrukcje dla użytkownika
- Link "Powrót do logowania" jest widoczny
- Opcjonalnie: formularz jest ukryty lub wyłączony

**Implementacja:**
```razor
@if (_submitSuccess == true)
{
    <div class="success-message">
        ✓ Link do resetowania hasła został wysłany!
        <p>Jeśli podany adres jest zarejestrowany, otrzymasz wiadomość...</p>
    </div>
}
```

---

#### Warunek 6: Wysłanie formularza zakończyło się błędem

**Komponent:** `ResetPasswordForm`

**Sprawdzany przez:** `_submitSuccess == false`

**Wpływ na UI:**
- Wyświetlany jest komunikat błędu (czerwony)
- Komunikat zawiera szczegóły z `_errorMessage`
- Formularz pozostaje edytowalny
- Przycisk submit jest ponownie aktywny
- Użytkownik może spróbować ponownie

**Implementacja:**
```razor
@if (_submitSuccess == false)
{
    <div class="error-message">
        ✗ @_errorMessage
    </div>
}
```

---

### Podsumowanie warunków według komponentów

| Komponent | Warunek | Typ | Wpływ na UI |
|-----------|---------|-----|-------------|
| `ResetPasswordForm` | E-mail wymagany | Walidacja danych | Komunikat błędu, blokada wysłania |
| `ResetPasswordForm` | E-mail format | Walidacja danych | Komunikat błędu, blokada wysłania |
| `ResetPasswordForm` | E-mail długość | Walidacja danych | Komunikat błędu, blokada wysłania |
| `ResetPasswordForm` | Wysyłanie w toku | Stan UI | Wyłączony przycisk, loader, zmiana tekstu |
| `ResetPasswordForm` | Wysłanie sukces | Stan UI | Komunikat sukcesu, opcjonalnie ukrycie formularza |
| `ResetPasswordForm` | Wysłanie błąd | Stan UI | Komunikat błędu, ponowna aktywacja formularza |

## 10. Obsługa błędów

### Kategorie błędów

Widok resetowania hasła musi obsługiwać różne scenariusze błędów, zapewniając użytkownikowi jasne komunikaty bez ujawniania szczegółów technicznych lub informacji mogących zagrozić bezpieczeństwu.

---

### 1. Błędy walidacji (po stronie klienta)

**Przyczyna:** Użytkownik wprowadził nieprawidłowe dane przed wysłaniem formularza

**Rodzaje:**
- Puste pole e-mail
- Nieprawidłowy format e-mail
- E-mail za długi (>255 znaków)

**Detekcja:** FluentValidation w `ResetPasswordValidator` przed wywołaniem API

**Obsługa:**
```csharp
// Obsługiwane automatycznie przez EditForm i FluentValidationValidator
// Walidacja zapobiega wywołaniu OnValidSubmit
```

**Komunikaty dla użytkownika:**
- "Adres e-mail jest wymagany"
- "Podaj prawidłowy adres e-mail"
- "Adres e-mail jest za długi"

**Wyświetlanie:**
- `<ValidationMessage For="@(() => _model.Email)">` pod polem input
- Opcjonalnie: `<ValidationSummary>` na górze formularza

**Odzyskiwanie:** Użytkownik poprawia dane i ponownie wysyła formularz

---

### 2. Błędy sieciowe

**Przyczyna:** Brak połączenia internetowego, timeout, problemy z DNS

**Detekcja:** Wyjątek `HttpRequestException` podczas wywołania API

**Obsługa:**
```csharp
catch (HttpRequestException httpEx)
{
    _submitSuccess = false;
    _errorMessage = "Nie można połączyć się z serwerem. Sprawdź połączenie internetowe i spróbuj ponownie.";
    
    // Logowanie dla debugowania
    _logger.LogError(httpEx, "Network error during password reset for email: {Email}", _model.Email);
}
```

**Komunikat dla użytkownika:**
```
✗ Nie można połączyć się z serwerem
Sprawdź połączenie internetowe i spróbuj ponownie.
```

**Odzyskiwanie:**
- Użytkownik sprawdza połączenie internetowe
- Próbuje ponownie klikając przycisk submit
- Formularz pozostaje wypełniony

---

### 3. Limit żądań (Rate Limiting)

**Przyczyna:** Zbyt wiele żądań resetowania hasła w krótkim czasie (ochrona przed spam/abuse)

**Detekcja:** Supabase zwraca odpowiedź z kodem błędu rate limit (429)

**Obsługa:**
```csharp
catch (Exception ex) when (ex.Message.Contains("rate limit") || ex.Message.Contains("too many requests"))
{
    _submitSuccess = false;
    _errorMessage = "Wysłano zbyt wiele próśb. Spróbuj ponownie za kilka minut.";
    
    _logger.LogWarning("Rate limit hit for password reset, email: {Email}", _model.Email);
}
```

**Komunikat dla użytkownika:**
```
✗ Wysłano zbyt wiele próśb
Spróbuj ponownie za kilka minut.
```

**Odzyskiwanie:**
- Użytkownik czeka określony czas (np. 5-15 minut)
- Próbuje ponownie później

**Opcjonalnie:** Wyłączenie przycisku submit z licznikiem czasu do następnej próby

---

### 4. Błąd serwera Supabase

**Przyczyna:** Problem po stronie Supabase (maintenance, overload, błąd wewnętrzny)

**Detekcja:** Supabase zwraca błąd 5xx lub API nie odpowiada prawidłowo

**Obsługa:**
```csharp
catch (Exception ex)
{
    _submitSuccess = false;
    _errorMessage = "Usługa tymczasowo niedostępna. Spróbuj ponownie za kilka minut.";
    
    _logger.LogError(ex, "Supabase service error during password reset");
}
```

**Komunikat dla użytkownika:**
```
✗ Usługa tymczasowo niedostępna
Spróbuj ponownie za kilka minut.
```

**Odzyskiwanie:**
- Użytkownik czeka i próbuje ponownie
- Jeśli problem się powtarza, może skontaktować się z supportem

---

### 5. Nieznany błąd (fallback)

**Przyczyna:** Dowolny nieoczekiwany błąd, który nie pasuje do kategorii powyżej

**Detekcja:** Blok `catch` dla ogólnego wyjątku

**Obsługa:**
```csharp
catch (Exception ex)
{
    _submitSuccess = false;
    _errorMessage = "Wystąpił nieoczekiwany błąd. Spróbuj ponownie później.";
    
    // Szczegółowe logowanie dla debugowania
    _logger.LogError(ex, "Unexpected error during password reset for email: {Email}. Exception: {Exception}", 
        _model.Email, ex.ToString());
}
```

**Komunikat dla użytkownika:**
```
✗ Wystąpił nieoczekiwany błąd
Spróbuj ponownie później. Jeśli problem się powtarza, skontaktuj się z nami.
```

**Odzyskiwanie:**
- Użytkownik próbuje ponownie
- Jeśli błąd się powtarza, kontaktuje się z supportem
- Deweloper analizuje logi

---

### 6. Bezpieczeństwo - nieistniejący adres e-mail

**Przyczyna:** Użytkownik wprowadził adres e-mail, który nie jest zarejestrowany w systemie

**WAŻNE:** Ze względów bezpieczeństwa, **nie ujawniamy** czy konto istnieje

**Obsługa:**
```csharp
// Supabase zwraca sukces niezależnie od tego, czy e-mail istnieje
if (response != null)
{
    _submitSuccess = true;
    // Komunikat jest taki sam dla istniejącego i nieistniejącego konta
}
```

**Komunikat dla użytkownika (identyczny dla wszystkich przypadków):**
```
✓ Link do resetowania hasła został wysłany!

Jeśli podany adres e-mail jest zarejestrowany w systemie, otrzymasz 
wiadomość z linkiem do resetowania hasła. Sprawdź swoją skrzynkę 
pocztową, w tym folder spam.
```

**Uzasadnienie:**
- Zapobiega enumeracji użytkowników (sprawdzaniu, które e-maile są zarejestrowane)
- Zwiększa bezpieczeństwo systemu
- Jest standardową praktyką w aplikacjach webowych

---

### Strategia logowania błędów

**Co logować:**
- Wszystkie wyjątki z pełnym stack trace
- Czas błędu
- Adres e-mail (ale NIE hasło czy tokeny)
- Typ błędu
- Informacje kontekstowe (user agent, IP - jeśli dostępne)

**Gdzie logować:**
- Serilog do Azure Application Insights / Seq / Sentry (zgodnie z PRD)
- Lokalne logi w trybie development

**Czego NIE logować:**
- Haseł
- Tokenów uwierzytelniających
- Danych osobowych wrażliwych

**Przykład logowania:**
```csharp
_logger.LogError(ex, 
    "Password reset failed. Email: {Email}, Error: {ErrorMessage}, Timestamp: {Timestamp}", 
    _model.Email, 
    ex.Message, 
    DateTime.UtcNow);
```

---

### Podsumowanie obsługi błędów

| Scenariusz | Detekcja | Komunikat użytkownika | Odzyskiwanie |
|------------|----------|----------------------|--------------|
| Błąd walidacji | FluentValidation | Specyficzne komunikaty walidacji | Popraw dane |
| Brak sieci | `HttpRequestException` | "Sprawdź połączenie internetowe" | Sprawdź sieć, retry |
| Rate limit | Kod błędu 429 | "Zbyt wiele próśb" | Czekaj kilka minut |
| Błąd serwera | Kod 5xx | "Usługa niedostępna" | Czekaj, retry |
| Nieznany błąd | Ogólny `catch` | "Nieoczekiwany błąd" | Retry, kontakt z supportem |
| Nieistniejący e-mail | Brak (sukces) | Standardowy komunikat sukcesu | Sprawdź pocztę |

## 11. Kroki implementacji

### Krok 1: Przygotowanie struktury katalogów

Utwórz strukturę folderów zgodną z architekturą Vertical Slice:

```bash
10xJournal.Client/
└── Features/
    └── Authentication/
        └── ResetPassword/
            ├── ResetPasswordPage.razor
            ├── ResetPasswordForm.razor
            ├── ResetPasswordModel.cs
            └── ResetPasswordValidator.cs
```

**Lokalizacja:** `10xJournal.Client/Features/Authentication/ResetPassword/`

---

### Krok 2: Utworzenie modelu danych

Utwórz plik `ResetPasswordModel.cs`:

```csharp
namespace _10xJournal.Client.Features.Authentication.ResetPassword;

/// <summary>
/// Model danych dla formularza resetowania hasła.
/// </summary>
public class ResetPasswordModel
{
    /// <summary>
    /// Adres e-mail użytkownika, który chce zresetować hasło.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
```

**Cel:** Binding danych formularza

---

### Krok 3: Utworzenie walidatora

Utwórz plik `ResetPasswordValidator.cs`:

```csharp
using FluentValidation;

namespace _10xJournal.Client.Features.Authentication.ResetPassword;

/// <summary>
/// Walidator dla formularza resetowania hasła.
/// </summary>
public class ResetPasswordValidator : AbstractValidator<ResetPasswordModel>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Adres e-mail jest wymagany");
            
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Podaj prawidłowy adres e-mail");
            
        RuleFor(x => x.Email)
            .MaximumLength(255)
            .WithMessage("Adres e-mail jest za długi");
    }
}
```

**Cel:** Walidacja danych przed wysłaniem do API

**Zależności:** Wymaga pakietu `FluentValidation` i integracji z Blazor

---

### Krok 4: Utworzenie komponentu strony głównej

Utwórz plik `ResetPasswordPage.razor`:

```razor
@page "/reset-password"
@layout PublicLayout

<PageTitle>Resetowanie hasła - 10xJournal</PageTitle>

<div class="reset-password-page">
    <header>
        <h1>Resetowanie hasła</h1>
        <p>
            Podaj adres e-mail powiązany z Twoim kontem. 
            Wyślemy Ci link do zresetowania hasła.
        </p>
    </header>

    <ResetPasswordForm />

    <footer>
        <p>
            <a href="/login">← Powrót do logowania</a>
        </p>
    </footer>
</div>

@code {
    // Brak logiki - komponent pełni rolę kontenera
}
```

**Cel:** Routing i layout dla widoku resetowania hasła

**Uwaga:** `PublicLayout` powinien być layoutem dla stron publicznych (bez nawigacji zalogowanego użytkownika)

---

### Krok 5: Utworzenie komponentu formularza

Utwórz plik `ResetPasswordForm.razor`:

```razor
@using FluentValidation
@inject Supabase.Client SupabaseClient
@inject ILogger<ResetPasswordForm> Logger

<div class="reset-password-form">
    @if (_submitSuccess == true)
    {
        <div class="success-message" role="alert">
            <strong>✓ Link do resetowania hasła został wysłany!</strong>
            <p>
                Jeśli podany adres e-mail jest zarejestrowany w systemie, 
                otrzymasz wiadomość z linkiem do resetowania hasła. 
                Sprawdź swoją skrzynkę pocztową, w tym folder spam.
            </p>
            <p>
                <small>Link jest ważny przez 24 godziny.</small>
            </p>
        </div>
    }
    else
    {
        <EditForm Model="@_model" OnValidSubmit="HandleValidSubmit">
            <FluentValidationValidator ValidatorType="@typeof(ResetPasswordValidator)" />
            <ValidationSummary />

            <div class="form-group">
                <label for="email">Adres e-mail</label>
                <InputText 
                    id="email" 
                    @bind-Value="_model.Email" 
                    type="email"
                    placeholder="twoj@email.com"
                    disabled="@_isSubmitting" />
                <ValidationMessage For="@(() => _model.Email)" />
            </div>

            @if (_submitSuccess == false && !string.IsNullOrEmpty(_errorMessage))
            {
                <div class="error-message" role="alert">
                    <strong>✗</strong> @_errorMessage
                </div>
            }

            <div class="form-actions">
                <button type="submit" disabled="@_isSubmitting">
                    @(_isSubmitting ? "Wysyłanie..." : "Wyślij link resetujący")
                </button>
            </div>

            @if (_isSubmitting)
            {
                <div class="loading-indicator">
                    <span>Wysyłanie żądania...</span>
                </div>
            }
        </EditForm>
    }
</div>

@code {
    private ResetPasswordModel _model = new();
    private bool _isSubmitting = false;
    private bool? _submitSuccess = null;
    private string? _errorMessage = null;

    private async Task HandleValidSubmit()
    {
        _isSubmitting = true;
        _errorMessage = null;
        StateHasChanged();

        try
        {
            // Opcjonalne: konfiguracja URL przekierowania po kliknięciu linku w e-mailu
            var options = new Supabase.Gotrue.ResetPasswordOptions
            {
                RedirectTo = "https://twoja-aplikacja.com/update-password"
            };

            var response = await SupabaseClient.Auth.ResetPasswordForEmail(
                _model.Email, 
                options
            );

            if (response != null)
            {
                _submitSuccess = true;
                Logger.LogInformation("Password reset email sent successfully to: {Email}", _model.Email);
            }
            else
            {
                _submitSuccess = false;
                _errorMessage = "Nie udało się wysłać linku resetującego. Spróbuj ponownie.";
                Logger.LogWarning("Password reset failed for email: {Email}", _model.Email);
            }
        }
        catch (HttpRequestException httpEx)
        {
            _submitSuccess = false;
            _errorMessage = "Nie można połączyć się z serwerem. Sprawdź połączenie internetowe.";
            Logger.LogError(httpEx, "Network error during password reset");
        }
        catch (Exception ex) when (ex.Message.Contains("rate limit") || ex.Message.Contains("too many"))
        {
            _submitSuccess = false;
            _errorMessage = "Wysłano zbyt wiele próśb. Spróbuj ponownie za kilka minut.";
            Logger.LogWarning("Rate limit hit for password reset");
        }
        catch (Exception ex)
        {
            _submitSuccess = false;
            _errorMessage = "Wystąpił błąd. Spróbuj ponownie później.";
            Logger.LogError(ex, "Unexpected error during password reset");
        }
        finally
        {
            _isSubmitting = false;
            StateHasChanged();
        }
    }
}
```

**Cel:** Logika formularza, wywołanie API, obsługa błędów

---

### Krok 6: Dodanie routingu

Sprawdź, czy routing `/reset-password` jest poprawnie skonfigurowany:

- Komponent `ResetPasswordPage.razor` zawiera dyrektywę `@page "/reset-password"`
- Router Blazor automatycznie wykrywa tę ścieżkę

**Testowanie:** Uruchom aplikację i przejdź do `http://localhost:5000/reset-password`

---

### Krok 7: Dodanie linku na stronie logowania

W komponencie `Login.razor` (lub odpowiadającym mu komponencie) dodaj link do resetowania hasła:

```razor
<div class="login-form">
    <!-- ... existing login form ... -->
    
    <div class="login-footer">
        <a href="/reset-password">Zapomniałeś hasła?</a>
    </div>
</div>
```

**Cel:** Umożliwienie użytkownikom dotarcia do funkcji resetowania hasła

---

### Krok 8: Konfiguracja Supabase

Skonfiguruj Supabase do wysyłania e-maili resetujących hasło:

1. **W panelu Supabase:**
   - Przejdź do `Authentication` → `Email Templates`
   - Dostosuj szablon "Reset Password" (opcjonalnie)
   - Upewnij się, że SMTP jest skonfigurowane (lub używaj domyślnego Supabase)

2. **Konfiguracja URL przekierowania:**
   - W `ResetPasswordOptions` ustaw `RedirectTo` na URL strony aktualizacji hasła
   - URL powinien prowadzić do strony obsługującej token resetowania

**Uwaga:** Strona `/update-password` to osobny widok (poza zakresem tego planu)

---


### Krok 10: Konfiguracja FluentValidation

Upewnij się, że FluentValidation jest poprawnie skonfigurowane w projekcie:

1. **Zainstaluj pakiet NuGet:**
   ```bash
   dotnet add package Blazored.FluentValidation
   ```

2. **Zarejestruj w `Program.cs`:**
   ```csharp
   builder.Services.AddScoped<IValidator<ResetPasswordModel>, ResetPasswordValidator>();
   ```

3. **Dodaj namespace w `_Imports.razor`:**
   ```razor
   @using Blazored.FluentValidation
   ```

---


### Krok 12: Logowanie i monitoring

Skonfiguruj Serilog do logowania błędów (zgodnie z PRD):

1. **W `Program.cs` skonfiguruj Serilog:**
   ```csharp
   builder.Services.AddLogging(logging =>
   {
       logging.AddSerilog();
   });
   ```
---

### Krok 13: Integracja z istniejącym przepływem uwierzytelniania

Upewnij się, że widok resetowania hasła jest zintegrowany z:
- Stroną logowania (link "Zapomniałeś hasła?")
- Stroną rejestracji (opcjonalnie: link do resetu)
- Stroną aktualizacji hasła (docelowy URL po kliknięciu linku w e-mailu)

---

### Krok 14: Dokumentacja

Dodaj dokumentację:
- Komentarze XML do publicznych metod
- README w folderze `ResetPassword/` z opisem flow
- Aktualizacja dokumentacji projektowej z nowym widokiem

---

### Kroki opcjonalne (po MVP)

- **Krok 16:** Dodanie analytics do śledzenia użycia funkcji resetowania
- **Krok 17:** A/B testing komunikatów sukcesu
- **Krok 18:** Dodanie możliwości resetowania hasła przez SMS (jeśli obsługiwane przez Supabase)
- **Krok 19:** Implementacja mechanizmu pamięci "ostatnio używanego e-maila" w Local Storage

---

## Podsumowanie implementacji

Ten plan obejmuje pełną implementację widoku resetowania hasła zgodnie z:
- ✅ PRD (minimalistyczny, bezpieczny, responsywny)
- ✅ User Stories (umożliwienie odzyskania dostępu do konta)
- ✅ Architekturą Vertical Slice (wszystkie pliki w jednym folderze)
- ✅ Tech Stack (Blazor WASM + Supabase + Pico.css)
- ✅ Zasadami bezpieczeństwa (nie ujawnianie istnienia kont)
- ✅ Obsługą błędów i walidacją

Implementacja powinna zająć **1-2 dni** dla doświadczonego programisty frontendowego Blazor.
