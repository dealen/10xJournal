# Plan implementacji widoku Ustawienia

## 1. Przegląd
Widok Ustawienia (`Settings`) umożliwia użytkownikowi kompleksowe zarządzanie swoim kontem w aplikacji 10xJournal. Głównym celem jest zapewnienie bezpiecznego i intuicyjnego dostępu do funkcji: zmiany hasła, wylogowania oraz trwałego usunięcia konta wraz z możliwością eksportu danych przed usunięciem. Widok jest dostępny wyłącznie dla zalogowanych użytkowników i implementuje wieloetapowy proces usuwania konta zgodny z najlepszymi praktykami UX i bezpieczeństwa.

## 2. Routing widoku
- **Ścieżka:** `/app/settings`
- **Wymagane uwierzytelnienie:** TAK (użytkownik musi być zalogowany)
- **Komponent główny:** `Settings.razor`
- **Lokalizacja:** `Features/Settings/Settings.razor`

## 3. Struktura komponentów

```
Settings.razor (strona główna)
│
├── ChangePassword/ (slice)
│   └── ChangePasswordForm.razor
│       ├── Model: ChangePasswordRequest.cs
│       └── Handler: ChangePasswordHandler.cs (opcjonalnie)
│
├── ExportData/ (slice)
│   └── ExportDataButton.razor
│       ├── Model: ExportDataResponse.cs
│       └── Handler: ExportDataHandler.cs
│
└── DeleteAccount/ (slice)
    ├── DeleteAccountSection.razor
    ├── DeleteAccountModal.razor
    ├── Models/
    │   ├── DeleteAccountRequest.cs
    │   └── DeleteAccountResponse.cs
    └── DeleteAccountHandler.cs
```

**Architektura Vertical Slice:** Każda funkcjonalność (zmiana hasła, eksport danych, usuwanie konta) jest organizowana we własnym slice'ie w katalogu `Features/Settings/`, zawierającym wszystkie niezbędne komponenty, modele i logikę.

## 4. Szczegóły komponentów

### 4.1. `Settings.razor` (Strona główna)

**Opis komponentu:**
Główny komponent-kontener widoku ustawień, odpowiedzialny za:
- Renderowanie struktury strony ustawień
- Orchestrację wszystkich pod-komponentów (zmiana hasła, eksport, usuwanie konta)
- Zarządzanie stanem globalnym widoku (np. komunikaty sukcesu/błędu)
- Obsługę wylogowania użytkownika

**Główne elementy:**
```html
<div class="settings-container">
    <h1>Ustawienia konta</h1>
    
    <!-- Sekcja: Zmiana hasła -->
    <section class="settings-section">
        <h2>Bezpieczeństwo</h2>
        <ChangePasswordForm OnPasswordChanged="HandlePasswordChanged" />
    </section>
    
    <!-- Sekcja: Wylogowanie -->
    <section class="settings-section">
        <h2>Sesja</h2>
        <button @onclick="HandleLogout">Wyloguj się</button>
    </section>
    
    <!-- Sekcja: Zarządzanie danymi -->
    <section class="settings-section">
        <h2>Twoje dane</h2>
        <ExportDataButton OnExportComplete="HandleExportComplete" />
    </section>
    
    <!-- Sekcja: Usuwanie konta (Danger Zone) -->
    <section class="settings-section danger-zone">
        <h2>Strefa niebezpieczna</h2>
        <DeleteAccountSection OnAccountDeleted="HandleAccountDeleted" />
    </section>
</div>
```

**Obsługiwane zdarzenia:**
- `HandleLogout()` - wylogowanie użytkownika i przekierowanie do strony głównej
- `HandlePasswordChanged()` - reakcja na zmianę hasła (wyświetlenie komunikatu sukcesu, opcjonalnie wylogowanie)
- `HandleExportComplete()` - reakcja na ukończenie eksportu danych
- `HandleAccountDeleted()` - obsługa pomyślnego usunięcia konta (natychmiastowe wylogowanie)

**Warunki walidacji:**
- Brak bezpośredniej walidacji na tym poziomie (delegowana do pod-komponentów)

**Typy:**
- Brak własnych modeli DTO (komponenty dzieci zarządzają swoimi typami)

**Propsy:**
- Brak (komponent główny nie przyjmuje propsów)

---

### 4.2. `ChangePasswordForm.razor` (Formularz zmiany hasła)

**Lokalizacja:** `Features/Settings/ChangePassword/ChangePasswordForm.razor`

**Opis komponentu:**
Formularz umożliwiający użytkownikowi zmianę hasła. Wymaga podania obecnego hasła oraz nowego hasła (dwukrotnie dla weryfikacji). Po pomyślnej zmianie hasła, wszystkie inne aktywne sesje użytkownika są unieważniane (zarządzane przez Supabase Auth).

**Główne elementy:**
```html
<EditForm Model="@changePasswordModel" OnValidSubmit="HandleChangePassword">
    <DataAnnotationsValidator />
    <ValidationSummary />
    
    <div class="form-group">
        <label for="currentPassword">Aktualne hasło</label>
        <InputText id="currentPassword" type="password" 
                   @bind-Value="changePasswordModel.CurrentPassword" />
        <ValidationMessage For="@(() => changePasswordModel.CurrentPassword)" />
    </div>
    
    <div class="form-group">
        <label for="newPassword">Nowe hasło</label>
        <InputText id="newPassword" type="password" 
                   @bind-Value="changePasswordModel.NewPassword" />
        <ValidationMessage For="@(() => changePasswordModel.NewPassword)" />
    </div>
    
    <div class="form-group">
        <label for="confirmPassword">Potwierdź nowe hasło</label>
        <InputText id="confirmPassword" type="password" 
                   @bind-Value="changePasswordModel.ConfirmPassword" />
        <ValidationMessage For="@(() => changePasswordModel.ConfirmPassword)" />
    </div>
    
    <button type="submit" disabled="@isSubmitting">
        @(isSubmitting ? "Zmieniam..." : "Zmień hasło")
    </button>
    
    @if (!string.IsNullOrEmpty(errorMessage))
    {
        <div class="error-message">@errorMessage</div>
    }
    
    @if (!string.IsNullOrEmpty(successMessage))
    {
        <div class="success-message">@successMessage</div>
    }
</EditForm>
```

**Obsługiwane zdarzenia:**
- `HandleChangePassword()` - wysłanie żądania zmiany hasła do Supabase Auth
- `OnPasswordChanged` (EventCallback) - callback do komponentu rodzica po pomyślnej zmianie

**Warunki walidacji:**
- `CurrentPassword`: wymagane, minimum 1 znak (niepuste)
- `NewPassword`: wymagane, minimum 8 znaków, musi zawierać przynajmniej jedną wielką literę, jedną małą literę i jedną cyfrę
- `ConfirmPassword`: wymagane, musi być identyczne z `NewPassword`
- Walidacja po stronie klienta: użycie `DataAnnotations` w modelu `ChangePasswordRequest`
- Walidacja po stronie serwera: Supabase Auth weryfikuje, czy `CurrentPassword` jest poprawne

**Typy:**
- `ChangePasswordRequest` (model formularza)

**Propsy:**
- `[Parameter] public EventCallback OnPasswordChanged { get; set; }`

---

### 4.3. `ExportDataButton.razor` (Przycisk eksportu danych)

**Lokalizacja:** `Features/Settings/ExportData/ExportDataButton.razor`

**Opis komponentu:**
Komponent odpowiedzialny za eksport wszystkich wpisów dziennika użytkownika do pliku JSON. Po kliknięciu przycisku, wywołuje endpoint `/rpc/export_journal_entries`, pobiera dane i generuje plik do pobrania w przeglądarce.

**Główne elementy:**
```html
<div class="export-section">
    <p>Pobierz kopię wszystkich swoich wpisów w formacie JSON.</p>
    
    <button @onclick="HandleExport" disabled="@isExporting">
        @(isExporting ? "Eksportuję..." : "Eksportuj dane")
    </button>
    
    @if (!string.IsNullOrEmpty(errorMessage))
    {
        <div class="error-message">@errorMessage</div>
    }
    
    @if (!string.IsNullOrEmpty(successMessage))
    {
        <div class="success-message">@successMessage</div>
    }
</div>
```

**Obsługiwane zdarzenia:**
- `HandleExport()` - wywołanie RPC `export_journal_entries`, generowanie i pobranie pliku
- `OnExportComplete` (EventCallback) - callback do rodzica po pomyślnym eksporcie

**Warunki walidacji:**
- Brak bezpośredniej walidacji formularza
- Weryfikacja: użytkownik musi być zalogowany (JWT token w nagłówku)

**Typy:**
- `ExportDataResponse` (odpowiedź z API)

**Propsy:**
- `[Parameter] public EventCallback OnExportComplete { get; set; }`

---

### 4.4. `DeleteAccountSection.razor` (Sekcja usuwania konta)

**Lokalizacja:** `Features/Settings/DeleteAccount/DeleteAccountSection.razor`

**Opis komponentu:**
Komponent inicjujący proces usuwania konta. Wyświetla ostrzeżenie o nieodwracalności operacji i przycisk otwierający modal potwierdzenia. Jest to pierwszy krok w wieloetapowym procesie usuwania konta.

**Główne elementy:**
```html
<div class="delete-account-section">
    <h3>Usuwanie konta</h3>
    <p class="warning-text">
        ⚠️ Uwaga: Usunięcie konta jest nieodwracalne. 
        Wszystkie Twoje wpisy, profil i dane zostaną trwale usunięte.
    </p>
    <p>Przed usunięciem zalecamy eksport danych.</p>
    
    <button class="danger-button" @onclick="OpenDeleteModal">
        Usuń moje konto
    </button>
</div>

@if (showDeleteModal)
{
    <DeleteAccountModal 
        OnConfirm="HandleAccountDeleted" 
        OnCancel="CloseDeleteModal" />
}
```

**Obsługiwane zdarzenia:**
- `OpenDeleteModal()` - otwiera modal potwierdzenia usunięcia
- `CloseDeleteModal()` - zamyka modal
- `HandleAccountDeleted()` - przekazuje informację o usunięciu konta do rodzica

**Warunki walidacji:**
- Brak (walidacja jest w modalu)

**Typy:**
- Brak własnych typów (używa typów z modalu)

**Propsy:**
- `[Parameter] public EventCallback OnAccountDeleted { get; set; }`

---

### 4.5. `DeleteAccountModal.razor` (Modal potwierdzenia usunięcia konta)

**Lokalizacja:** `Features/Settings/DeleteAccount/DeleteAccountModal.razor`

**Opis komponentu:**
Kluczowy komponent implementujący wieloetapowy proces usuwania konta:
1. **Krok 1:** Informacja o eksporcie danych z przyciskiem "Eksportuj teraz" (opcjonalne)
2. **Krok 2:** Pole do wpisania hasła użytkownika
3. **Krok 3:** Pole do wpisania frazy potwierdzającej: "usuń moje dane"
4. **Krok 4:** Aktywacja przycisku ostatecznego usunięcia dopiero po spełnieniu wszystkich warunków

**Główne elementy:**
```html
<div class="modal-overlay" @onclick="HandleOverlayClick">
    <div class="modal-content" @onclick:stopPropagation="true">
        <h2>Potwierdzenie usunięcia konta</h2>
        
        <!-- Krok 1: Eksport danych -->
        @if (!hasExported)
        {
            <div class="modal-step">
                <p><strong>Krok 1:</strong> Zalecamy eksport Twoich danych</p>
                <button @onclick="HandleExport" disabled="@isExporting">
                    @(isExporting ? "Eksportuję..." : "Eksportuj teraz")
                </button>
                <button class="link-button" @onclick="SkipExport">
                    Pomiń ten krok
                </button>
            </div>
        }
        
        <!-- Krok 2: Weryfikacja hasła -->
        @if (hasExported || skipExport)
        {
            <div class="modal-step">
                <p><strong>Krok 2:</strong> Wprowadź swoje hasło</p>
                <input type="password" 
                       @bind="password" 
                       @bind:event="oninput"
                       placeholder="Twoje hasło" />
            </div>
        }
        
        <!-- Krok 3: Wpisanie frazy potwierdzającej -->
        @if (!string.IsNullOrEmpty(password))
        {
            <div class="modal-step">
                <p><strong>Krok 3:</strong> Wpisz dokładnie: <code>usuń moje dane</code></p>
                <input type="text" 
                       @bind="confirmationPhrase" 
                       @bind:event="oninput"
                       placeholder="usuń moje dane" />
            </div>
        }
        
        <!-- Przyciski akcji -->
        <div class="modal-actions">
            <button @onclick="HandleCancel">Anuluj</button>
            <button class="danger-button" 
                    @onclick="HandleDelete" 
                    disabled="@(!IsDeleteEnabled)">
                Usuń konto na zawsze
            </button>
        </div>
        
        @if (!string.IsNullOrEmpty(errorMessage))
        {
            <div class="error-message">@errorMessage</div>
        }
    </div>
</div>
```

**Obsługiwane zdarzenia:**
- `HandleExport()` - eksport danych w modalu
- `SkipExport()` - pominięcie kroku eksportu
- `HandleOverlayClick()` - zamknięcie modalu kliknięciem w tło
- `HandleCancel()` - anulowanie procesu
- `HandleDelete()` - wywołanie API usuwania konta
- Dwukierunkowe bindowanie dla `password` i `confirmationPhrase`

**Warunki walidacji:**
- `IsDeleteEnabled` - computed property, przycisk usuwania jest aktywny tylko gdy:
  - `hasExported == true` OR `skipExport == true`
  - `password.Length >= 8` (podstawowa walidacja długości)
  - `confirmationPhrase == "usuń moje dane"` (dokładne dopasowanie, case-sensitive)

**Typy:**
- `DeleteAccountRequest`
- `DeleteAccountResponse`
- `ExportDataResponse` (dla funkcji eksportu)

**Propsy:**
- `[Parameter] public EventCallback OnConfirm { get; set; }`
- `[Parameter] public EventCallback OnCancel { get; set; }`

---

## 5. Typy

### 5.1. `ChangePasswordRequest.cs`
**Lokalizacja:** `Features/Settings/ChangePassword/Models/ChangePasswordRequest.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace _10xJournal.Client.Features.Settings.ChangePassword.Models;

/// <summary>
/// Model żądania zmiany hasła użytkownika.
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Aktualne hasło użytkownika.
    /// Wymagane do weryfikacji tożsamości przed zmianą.
    /// </summary>
    [Required(ErrorMessage = "Aktualne hasło jest wymagane")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// Nowe hasło użytkownika.
    /// Musi spełniać wymogi bezpieczeństwa (min. 8 znaków, wielka litera, mała litera, cyfra).
    /// </summary>
    [Required(ErrorMessage = "Nowe hasło jest wymagane")]
    [MinLength(8, ErrorMessage = "Hasło musi mieć co najmniej 8 znaków")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", 
        ErrorMessage = "Hasło musi zawierać przynajmniej jedną wielką literę, jedną małą literę i jedną cyfrę")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Potwierdzenie nowego hasła.
    /// Musi być identyczne z NewPassword.
    /// </summary>
    [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane")]
    [Compare(nameof(NewPassword), ErrorMessage = "Hasła muszą być identyczne")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

---

### 5.2. `ExportDataResponse.cs`
**Lokalizacja:** `Features/Settings/ExportData/Models/ExportDataResponse.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace _10xJournal.Client.Features.Settings.ExportData.Models;

/// <summary>
/// Model odpowiedzi z endpointu eksportu danych.
/// Mapuje strukturę JSON zwracaną przez /rpc/export_journal_entries.
/// </summary>
public class ExportDataResponse
{
    /// <summary>
    /// Data i czas wykonania eksportu.
    /// </summary>
    [JsonPropertyName("export_date")]
    public DateTimeOffset ExportDate { get; set; }

    /// <summary>
    /// Liczba wyeksportowanych wpisów.
    /// </summary>
    [JsonPropertyName("entry_count")]
    public int EntryCount { get; set; }

    /// <summary>
    /// Lista wszystkich wpisów dziennika użytkownika.
    /// </summary>
    [JsonPropertyName("entries")]
    public List<ExportedEntry> Entries { get; set; } = new();
}

/// <summary>
/// Model pojedynczego wpisu w eksporcie.
/// </summary>
public class ExportedEntry
{
    /// <summary>
    /// Data utworzenia wpisu.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Treść wpisu dziennika.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
```

---

### 5.3. `DeleteAccountRequest.cs`
**Lokalizacja:** `Features/Settings/DeleteAccount/Models/DeleteAccountRequest.cs`

```csharp
namespace _10xJournal.Client.Features.Settings.DeleteAccount.Models;

/// <summary>
/// Model żądania usunięcia konta użytkownika.
/// Zgodnie z dokumentacją API, endpoint /rpc/delete_my_account
/// nie wymaga ciała żądania (user_id jest pobierany z JWT).
/// Ten model służy do przechowywania danych walidacyjnych po stronie klienta.
/// </summary>
public class DeleteAccountRequest
{
    /// <summary>
    /// Hasło użytkownika do weryfikacji tożsamości.
    /// Nie jest wysyłane w żądaniu usunięcia, ale używane
    /// do ponownej autentykacji przed wywołaniem API.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Fraza potwierdzająca: "usuń moje dane".
    /// Wymagana do aktywacji przycisku usuwania.
    /// </summary>
    public string ConfirmationPhrase { get; set; } = string.Empty;

    /// <summary>
    /// Flaga określająca, czy użytkownik wyeksportował dane.
    /// </summary>
    public bool HasExportedData { get; set; }
}
```

---

### 5.4. `DeleteAccountResponse.cs`
**Lokalizacja:** `Features/Settings/DeleteAccount/Models/DeleteAccountResponse.cs`

```csharp
using System.Text.Json.Serialization;

namespace _10xJournal.Client.Features.Settings.DeleteAccount.Models;

/// <summary>
/// Model odpowiedzi z endpointu usuwania konta.
/// Mapuje strukturę JSON zwracaną przez /rpc/delete_my_account.
/// </summary>
public class DeleteAccountResponse
{
    /// <summary>
    /// Status operacji (np. "success").
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Komunikat zwrotny z API.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
```

---

## 6. Zarządzanie stanem

### 6.1. Stan lokalny komponentów

**Każdy komponent zarządza własnym stanem lokalnie:**

**`Settings.razor`:**
```csharp
private string? globalSuccessMessage;
private string? globalErrorMessage;
```

**`ChangePasswordForm.razor`:**
```csharp
private ChangePasswordRequest changePasswordModel = new();
private bool isSubmitting = false;
private string? errorMessage;
private string? successMessage;
```

**`ExportDataButton.razor`:**
```csharp
private bool isExporting = false;
private string? errorMessage;
private string? successMessage;
```

**`DeleteAccountModal.razor`:**
```csharp
private DeleteAccountRequest deleteModel = new();
private string password = string.Empty;
private string confirmationPhrase = string.Empty;
private bool hasExported = false;
private bool skipExport = false;
private bool isDeleting = false;
private string? errorMessage;

private bool IsDeleteEnabled => 
    (hasExported || skipExport) && 
    password.Length >= 8 && 
    confirmationPhrase == "usuń moje dane";
```

### 6.2. Brak globalnego store

Dla MVP nie jest wymagany globalny store (np. Fluxor). Wszystkie operacje są lokalne do widoku Settings, a komunikacja między komponentami odbywa się przez `EventCallback`.

### 6.3. Wstrzykiwane serwisy

Komponenty wymagają następujących serwisów (dependency injection):
- `Supabase.Client` - do komunikacji z API
- `NavigationManager` - do przekierowań po wylogowaniu/usunięciu konta
- `ILogger<T>` - do logowania błędów i zdarzeń
- `IJSRuntime` - do generowania i pobierania pliku eksportu w przeglądarce

---

## 7. Integracja API

### 7.1. Zmiana hasła

**Metoda Supabase:**
```csharp
await _supabaseClient.Auth.Update(new UserAttributes
{
    Password = changePasswordModel.NewPassword
});
```

**Typ żądania:** `ChangePasswordRequest` (lokalny model)  
**Typ odpowiedzi:** Supabase zwraca `Session` lub rzuca `GotrueException`

**Obsługa:**
```csharp
try
{
    isSubmitting = true;
    errorMessage = null;
    
    var response = await _supabaseClient.Auth.Update(new UserAttributes
    {
        Password = changePasswordModel.NewPassword
    });
    
    successMessage = "Hasło zostało zmienione. Wszystkie inne sesje zostały wylogowane.";
    await OnPasswordChanged.InvokeAsync();
    
    // Opcjonalnie: wylogowanie bieżącej sesji
    // await Task.Delay(2000);
    // await _supabaseClient.Auth.SignOut();
    // _navigationManager.NavigateTo("/");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Błąd podczas zmiany hasła");
    errorMessage = "Nie udało się zmienić hasła. Sprawdź, czy podałeś poprawne aktualne hasło.";
}
finally
{
    isSubmitting = false;
}
```

---

### 7.2. Eksport danych

**Endpoint:** `POST /rpc/export_journal_entries`

**Typ żądania:** Brak ciała żądania (N/A)  
**Typ odpowiedzi:** `ExportDataResponse`

**Obsługa:**
```csharp
try
{
    isExporting = true;
    errorMessage = null;
    
    var response = await _supabaseClient
        .Rpc("export_journal_entries", null)
        .Get<ExportDataResponse>();
    
    // Generowanie pliku JSON i pobranie w przeglądarce
    var json = JsonSerializer.Serialize(response, new JsonSerializerOptions 
    { 
        WriteIndented = true 
    });
    
    var fileName = $"10xJournal_export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
    
    await _jsRuntime.InvokeVoidAsync("downloadFile", fileName, json);
    
    successMessage = $"Wyeksportowano {response.EntryCount} wpisów.";
    await OnExportComplete.InvokeAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Błąd podczas eksportu danych");
    errorMessage = "Nie udało się wyeksportować danych. Spróbuj ponownie.";
}
finally
{
    isExporting = false;
}
```

**JavaScript helper (wwwroot/js/export.js):**
```javascript
window.downloadFile = (filename, content) => {
    const blob = new Blob([content], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};
```

---

### 7.3. Usuwanie konta

**Endpoint:** `POST /rpc/delete_my_account`

**Typ żądania:** Brak ciała żądania (N/A) - user_id pochodzi z JWT  
**Typ odpowiedzi:** `DeleteAccountResponse`

**Obsługa (w `DeleteAccountModal.razor`):**
```csharp
private async Task HandleDelete()
{
    try
    {
        isDeleting = true;
        errorMessage = null;
        
        // Krok 1: Ponowna weryfikacja hasła (opcjonalne, ale zalecane)
        // Supabase nie ma bezpośredniej metody verify password, 
        // więc możemy spróbować zalogować się ponownie
        
        var currentUser = _supabaseClient.Auth.CurrentUser;
        if (currentUser == null)
        {
            errorMessage = "Sesja wygasła. Zaloguj się ponownie.";
            return;
        }
        
        // Opcjonalnie: re-authentication
        // await _supabaseClient.Auth.SignIn(currentUser.Email, password);
        
        // Krok 2: Wywołanie RPC usuwania konta
        var response = await _supabaseClient
            .Rpc("delete_my_account", null)
            .Get<DeleteAccountResponse>();
        
        if (response.Status == "success")
        {
            // Krok 3: Wylogowanie i przekierowanie
            await _supabaseClient.Auth.SignOut();
            await OnConfirm.InvokeAsync();
            _navigationManager.NavigateTo("/", true);
        }
        else
        {
            errorMessage = "Nie udało się usunąć konta. Spróbuj ponownie.";
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Błąd podczas usuwania konta");
        errorMessage = "Wystąpił błąd podczas usuwania konta. Sprawdź poprawność hasła i spróbuj ponownie.";
    }
    finally
    {
        isDeleting = false;
    }
}
```

---

## 8. Interakcje użytkownika

### 8.1. Zmiana hasła
1. Użytkownik wypełnia formularz: aktualne hasło, nowe hasło, potwierdzenie nowego hasła
2. Kliknięcie "Zmień hasło" → walidacja po stronie klienta (DataAnnotations)
3. Jeśli walidacja przeszła → wywołanie `Auth.Update()` na Supabase
4. Sukces → wyświetlenie komunikatu "Hasło zostało zmienione"
5. Błąd → wyświetlenie komunikatu błędu (np. "Nieprawidłowe aktualne hasło")

### 8.2. Wylogowanie
1. Użytkownik klika przycisk "Wyloguj się"
2. Wywołanie `_supabaseClient.Auth.SignOut()`
3. Przekierowanie do strony głównej `/`

### 8.3. Eksport danych
1. Użytkownik klika przycisk "Eksportuj dane"
2. Wyświetlenie spinnera/komunikatu "Eksportuję..."
3. Wywołanie `/rpc/export_journal_entries`
4. Wygenerowanie pliku JSON i automatyczne pobranie w przeglądarce
5. Wyświetlenie komunikatu sukcesu z liczbą wyeksportowanych wpisów

### 8.4. Usuwanie konta (wieloetapowe)
1. Użytkownik klika "Usuń moje konto" → otwiera się modal
2. **Krok 1 (opcjonalny):** Modal sugeruje eksport danych
   - Użytkownik klika "Eksportuj teraz" → wywołanie funkcji eksportu w modalu
   - Lub klika "Pomiń ten krok" → przejście do kroku 2
3. **Krok 2:** Użytkownik wpisuje swoje hasło → pole staje się widoczne
4. **Krok 3:** Użytkownik wpisuje frazę "usuń moje dane" → pole staje się widoczne
5. Po spełnieniu wszystkich warunków → przycisk "Usuń konto na zawsze" staje się aktywny
6. Kliknięcie przycisku → wywołanie `/rpc/delete_my_account`
7. Sukces → wylogowanie i przekierowanie do strony głównej
8. W dowolnym momencie użytkownik może kliknąć "Anuluj" lub kliknąć w tło modalu, aby zamknąć modal bez usuwania konta

---

## 9. Warunki i walidacja

### 9.1. ChangePasswordForm
**Komponent:** `ChangePasswordForm.razor`

**Warunki:**
- `CurrentPassword`:
  - Wymagane (NOT NULL)
  - Minimum 1 znak
- `NewPassword`:
  - Wymagane (NOT NULL)
  - Minimum 8 znaków
  - Regex: musi zawierać małą literę, wielką literę i cyfrę
- `ConfirmPassword`:
  - Wymagane (NOT NULL)
  - Musi być identyczne z `NewPassword` (atrybut `[Compare]`)

**Wpływ na interfejs:**
- Jeśli walidacja nie przeszła → przycisk "Zmień hasło" jest zablokowany (EditForm)
- Błędy walidacji są wyświetlane pod każdym polem (`<ValidationMessage>`)
- Po stronie serwera: Supabase Auth weryfikuje poprawność `CurrentPassword` i zwraca błąd 400 lub 401, jeśli jest nieprawidłowe

---

### 9.2. DeleteAccountModal
**Komponent:** `DeleteAccountModal.razor`

**Warunki:**
- `hasExported` OR `skipExport` musi być `true`
- `password.Length >= 8`
- `confirmationPhrase == "usuń moje dane"` (case-sensitive, dokładne dopasowanie)

**Obliczana właściwość:**
```csharp
private bool IsDeleteEnabled => 
    (hasExported || skipExport) && 
    password.Length >= 8 && 
    confirmationPhrase == "usuń moje dane";
```

**Wpływ na interfejs:**
- Kroki są wyświetlane sekwencyjnie (warunki `@if`)
- Przycisk "Usuń konto na zawsze" jest aktywny (`disabled="@(!IsDeleteEnabled)"`) tylko po spełnieniu wszystkich warunków
- Jeśli użytkownik nie wypełni wszystkich pól poprawnie, przycisk pozostaje nieaktywny

---

### 9.3. Walidacja po stronie API
- **Zmiana hasła:** Supabase Auth weryfikuje poprawność `CurrentPassword` i rzuca wyjątek `GotrueException`, jeśli jest nieprawidłowe
- **Usuwanie konta:** Endpoint `/rpc/delete_my_account` weryfikuje JWT token i zwraca 401, jeśli token jest nieprawidłowy lub wygasł
- **Eksport danych:** Endpoint `/rpc/export_journal_entries` weryfikuje JWT token

---

## 10. Obsługa błędów

### 10.1. Błędy sieciowe
**Scenariusz:** Utrata połączenia internetowego podczas wywołania API

**Obsługa:**
- Wszystkie wywołania API są opakowane w bloki `try-catch`
- W przypadku wyjątku sieciowego:
  - Logowanie błędu do `ILogger`
  - Wyświetlenie użytkownikowi komunikatu: "Nie udało się nawiązać połączenia. Sprawdź połączenie internetowe i spróbuj ponownie."

```csharp
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "Błąd połączenia podczas operacji");
    errorMessage = "Nie udało się nawiązać połączenia. Sprawdź połączenie internetowe i spróbuj ponownie.";
}
```

---

### 10.2. Błędy autoryzacji (401 Unauthorized)
**Scenariusz:** Token JWT wygasł lub jest nieprawidłowy

**Obsługa:**
- Wylogowanie użytkownika
- Przekierowanie do strony logowania z komunikatem: "Twoja sesja wygasła. Zaloguj się ponownie."

```csharp
catch (Supabase.Postgrest.Exceptions.PostgrestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
{
    _logger.LogWarning("Token JWT wygasł lub jest nieprawidłowy");
    await _supabaseClient.Auth.SignOut();
    _navigationManager.NavigateTo("/auth/login?message=session-expired");
}
```

---

### 10.3. Błąd nieprawidłowego hasła (zmiana hasła)
**Scenariusz:** Użytkownik podał nieprawidłowe aktualne hasło

**Obsługa:**
- Supabase Auth zwróci `GotrueException`
- Wyświetlenie komunikatu: "Nie udało się zmienić hasła. Sprawdź, czy podałeś poprawne aktualne hasło."

```csharp
catch (Supabase.Gotrue.Exceptions.GotrueException ex)
{
    _logger.LogWarning(ex, "Nieprawidłowe aktualne hasło");
    errorMessage = "Nie udało się zmienić hasła. Sprawdź, czy podałeś poprawne aktualne hasło.";
}
```

---

### 10.4. Błąd podczas usuwania konta
**Scenariusz:** Błąd po stronie bazy danych lub serwera podczas usuwania konta

**Obsługa:**
- Logowanie pełnego wyjątku
- Wyświetlenie komunikatu: "Wystąpił błąd podczas usuwania konta. Spróbuj ponownie lub skontaktuj się z pomocą techniczną."
- **Ważne:** Konto NIE zostało usunięte, użytkownik może spróbować ponownie

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Błąd podczas usuwania konta użytkownika {UserId}", _supabaseClient.Auth.CurrentUser?.Id);
    errorMessage = "Wystąpił błąd podczas usuwania konta. Spróbuj ponownie lub skontaktuj się z pomocą techniczną.";
}
```

---

### 10.5. Błąd podczas eksportu danych
**Scenariusz:** Brak wpisów do eksportu lub błąd po stronie serwera

**Obsługa:**
- Jeśli `EntryCount == 0`: Komunikat informacyjny: "Nie masz jeszcze żadnych wpisów do eksportu."
- Jeśli błąd serwera: "Nie udało się wyeksportować danych. Spróbuj ponownie."

```csharp
if (response.EntryCount == 0)
{
    successMessage = "Nie masz jeszcze żadnych wpisów do eksportu.";
    return;
}
```

---

### 10.6. Błędy walidacji formularzy
**Scenariusz:** Użytkownik próbuje wysłać formularz z nieprawidłowymi danymi

**Obsługa:**
- `EditForm` z `DataAnnotationsValidator` automatycznie wyświetla komunikaty błędów walidacji
- `ValidationSummary` wyświetla podsumowanie wszystkich błędów na górze formularza
- `ValidationMessage` wyświetla komunikaty błędów pod konkretnymi polami

---

## 11. Kroki implementacji

### Krok 1: Przygotowanie struktury katalogów (Vertical Slice Architecture)
1. Utworzyć katalogi zgodnie z architekturą slice:
   ```
   Features/Settings/
   ├── ChangePassword/
   │   ├── Models/
   │   └── ChangePasswordForm.razor
   ├── ExportData/
   │   ├── Models/
   │   └── ExportDataButton.razor
   └── DeleteAccount/
       ├── Models/
       ├── DeleteAccountSection.razor
       └── DeleteAccountModal.razor
   ```

### Krok 2: Implementacja modeli DTO
1. Utworzyć `ChangePasswordRequest.cs` z atrybutami walidacji
2. Utworzyć `ExportDataResponse.cs` i `ExportedEntry.cs`
3. Utworzyć `DeleteAccountRequest.cs` i `DeleteAccountResponse.cs`
4. Upewnić się, że wszystkie modele mają odpowiednie atrybuty `[JsonPropertyName]` dla deserializacji

### Krok 3: Implementacja funkcji eksportu JavaScript
1. Utworzyć plik `wwwroot/js/export.js`
2. Dodać funkcję `downloadFile(filename, content)`
3. Dodać referencję do skryptu w `wwwroot/index.html`:
   ```html
   <script src="js/export.js"></script>
   ```

### Krok 4: Implementacja komponentu `ChangePasswordForm.razor`
1. Utworzyć plik `Features/Settings/ChangePassword/ChangePasswordForm.razor`
2. Zaimplementować `EditForm` z bindowaniem do `ChangePasswordRequest`
3. Dodać pola: `CurrentPassword`, `NewPassword`, `ConfirmPassword`
4. Zaimplementować metodę `HandleChangePassword()`:
   - Wstrzyknąć `Supabase.Client`, `ILogger`
   - Wywołać `Auth.Update()`
   - Obsłużyć sukces i błędy
5. Dodać `EventCallback OnPasswordChanged`
6. Przetestować walidację formularza

### Krok 5: Implementacja komponentu `ExportDataButton.razor`
1. Utworzyć plik `Features/Settings/ExportData/ExportDataButton.razor`
2. Dodać przycisk "Eksportuj dane" z obsługą stanu `isExporting`
3. Zaimplementować metodę `HandleExport()`:
   - Wstrzyknąć `Supabase.Client`, `IJSRuntime`, `ILogger`
   - Wywołać RPC `export_journal_entries`
   - Deserializować odpowiedź do `ExportDataResponse`
   - Wywołać `IJSRuntime.InvokeVoidAsync("downloadFile", ...)`
   - Obsłużyć przypadek braku wpisów (`EntryCount == 0`)
4. Dodać `EventCallback OnExportComplete`

### Krok 6: Implementacja komponentu `DeleteAccountSection.razor`
1. Utworzyć plik `Features/Settings/DeleteAccount/DeleteAccountSection.razor`
2. Dodać sekcję z ostrzeżeniem i przyciskiem "Usuń moje konto"
3. Zaimplementować metodę `OpenDeleteModal()` do zarządzania flagą `showDeleteModal`
4. Warunkowo renderować `DeleteAccountModal` gdy `showDeleteModal == true`
5. Dodać `EventCallback OnAccountDeleted`

### Krok 7: Implementacja komponentu `DeleteAccountModal.razor`
1. Utworzyć plik `Features/Settings/DeleteAccount/DeleteAccountModal.razor`
2. Zaimplementować strukturę HTML modalu z overlay
3. Dodać wieloetapowy przepływ:
   - Krok 1: Eksport danych (opcjonalny)
   - Krok 2: Weryfikacja hasła
   - Krok 3: Wpisanie frazy potwierdzającej
4. Zaimplementować computed property `IsDeleteEnabled`
5. Zaimplementować metodę `HandleDelete()`:
   - Wstrzyknąć `Supabase.Client`, `NavigationManager`, `ILogger`
   - Wywołać RPC `delete_my_account`
   - Po sukcesie: wylogować i przekierować
6. Zaimplementować metodę `HandleExport()` wewnątrz modalu (ponowne użycie logiki z `ExportDataButton`)
7. Dodać `EventCallback OnConfirm` i `OnCancel`
8. Zaimplementować zamykanie modalu przez kliknięcie w overlay

### Krok 8: Implementacja strony głównej `Settings.razor`
1. Utworzyć plik `Features/Settings/Settings.razor`
2. Dodać dyrektywę `@page "/app/settings"`
3. Dodać dyrektywę `@attribute [Authorize]` (wymaga zalogowania)
4. Zbudować layout strony z sekcjami:
   - Bezpieczeństwo → `<ChangePasswordForm>`
   - Sesja → przycisk "Wyloguj się"
   - Twoje dane → `<ExportDataButton>`
   - Strefa niebezpieczna → `<DeleteAccountSection>`
5. Zaimplementować metodę `HandleLogout()`:
   - Wywołać `Auth.SignOut()`
   - Przekierować do `/`
6. Zaimplementować handlery dla EventCallbacks:
   - `HandlePasswordChanged()` → wyświetlić komunikat sukcesu
   - `HandleExportComplete()` → wyświetlić komunikat sukcesu
   - `HandleAccountDeleted()` → automatyczne wylogowanie (już obsłużone w modalu)

### Krok 9: Styling (Pico.css)
1. Upewnić się, że Pico.css jest załadowany w `wwwroot/index.html`
2. Dodać customowe style w `wwwroot/css/app.css`:
   - `.settings-container` → kontener główny
   - `.settings-section` → sekcje ustawień z odstępami
   - `.danger-zone` → sekcja usuwania konta z czerwonym akcentem
   - `.danger-button` → czerwony przycisk dla akcji destrukcyjnych
   - `.modal-overlay` → tło modalu z `position: fixed` i `z-index`
   - `.modal-content` → białe tło z cieniem i zaokrąglonymi rogami
   - `.modal-step` → odstępy między krokami w modalu
   - `.error-message` / `.success-message` → komunikaty kolorowe
3. Upewnić się, że wszystko działa responsywnie (Pico.css zapewnia to domyślnie)

### Krok 10: Aktualizacja nawigacji
1. Dodać link do strony Settings w `Layout/NavMenu.razor`:
   ```html
   <NavLink href="app/settings">
       <span class="icon">⚙️</span> Ustawienia
   </NavLink>
   ```

### Krok 11: Testowanie manualne
1. **Test zmiany hasła:**
   - Wypełnić formularz z prawidłowymi danymi → sprawdzić sukces
   - Wypełnić z nieprawidłowym aktualnym hasłem → sprawdzić komunikat błędu
   - Wypełnić z niezgodnymi nowymi haseł → sprawdzić walidację
2. **Test wylogowania:**
   - Kliknąć "Wyloguj się" → sprawdzić przekierowanie i brak sesji
3. **Test eksportu:**
   - Kliknąć "Eksportuj dane" → sprawdzić pobranie pliku JSON
   - Otworzyć plik i zweryfikować strukturę danych
4. **Test usuwania konta:**
   - Otworzyć modal → sprawdzić wszystkie kroki
   - Wypróbować "Pomiń eksport" → sprawdzić, czy kolejne kroki się pokazują
   - Wpisać hasło i frazę → sprawdzić, czy przycisk się aktywuje
   - Anulować → sprawdzić zamknięcie modalu
   - Usunąć konto → sprawdzić wylogowanie i przekierowanie
   - Zweryfikować w bazie danych, że wszystkie dane użytkownika zostały usunięte

### Krok 12: Testowanie błędów i przypadków brzegowych
1. Symulować utratę połączenia (DevTools → Offline) → sprawdzić komunikaty błędów
2. Symulować wygaśnięcie tokenu → sprawdzić automatyczne wylogowanie
3. Próbować wyeksportować dane gdy nie ma żadnych wpisów → sprawdzić komunikat
4. Próbować usunąć konto bez wypełnienia wszystkich kroków → sprawdzić, że przycisk jest nieaktywny

### Krok 13: Code review i refaktoryzacja
1. Upewnić się, że wszystkie komponenty używają dependency injection
2. Sprawdzić, czy wszystkie błędy są logowane z odpowiednim kontekstem
3. Upewnić się, że komunikaty dla użytkownika są przyjazne i w języku polskim
4. Zweryfikować, że kod jest zgodny z Vertical Slice Architecture
5. Sprawdzić, czy wszystkie nazwy plików i klas są zgodne z konwencją

### Krok 14: Dokumentacja
1. Dodać komentarze XML do wszystkich publicznych metod i właściwości
2. Zaktualizować dokumentację projektu o informacje o nowym widoku
3. Utworzyć screenshot'y widoku do dokumentacji użytkownika (opcjonalne)

---

## Podsumowanie

Ten plan implementacji zapewnia szczegółowy przewodnik krok po kroku dla wdrożenia widoku Ustawienia w aplikacji 10xJournal. Widok został zaprojektowany zgodnie z zasadami Vertical Slice Architecture, co oznacza, że każda funkcjonalność (zmiana hasła, eksport, usuwanie konta) jest samowystarczalnym slice'em ze swoją logiką, modelami i komponentami.

Kluczowe aspekty implementacji:
- **Bezpieczeństwo:** Wieloetapowy proces usuwania konta z wymuszonym potwierdzeniem
- **UX:** Progresywne ujawnianie kroków w modalu usuwania, jasne komunikaty błędów
- **Architektura:** Zgodność z Vertical Slice Architecture, brak centralnych warstw serwisów
- **Responsywność:** Wykorzystanie Pico.css do zapewnienia mobile-first design
- **Obsługa błędów:** Kompleksowe try-catch bloki i przyjazne komunikaty dla użytkownika
