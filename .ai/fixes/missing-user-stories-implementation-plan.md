# Plan Implementacji Brakujących User Stories

**Data utworzenia:** 13 października 2025  
**Wersja:** 1.0  
**Status:** Do implementacji

---

## Spis Treści

1. [Przegląd i Priorytety](#1-przegląd-i-priorytety)
2. [US 11: Wpis Powitalny dla Nowego Użytkownika (KRYTYCZNY)](#2-us-11-wpis-powitalny-dla-nowego-użytkownika-krytyczny)
3. [US 8: Funkcjonalność Wylogowania (KRYTYCZNY)](#3-us-8-funkcjonalność-wylogowania-krytyczny)
4. [US 12: Dynamiczna Lista Wpisów (WYSOKI PRIORYTET)](#4-us-12-dynamiczna-lista-wpisów-wysoki-priorytet)
5. [US 10: Zmiana Hasła w Ustawieniach (WYSOKI PRIORYTET)](#5-us-10-zmiana-hasła-w-ustawieniach-wysoki-priorytet)
6. [Kolejność Implementacji](#6-kolejność-implementacji)
7. [Testowanie](#7-testowanie)

---

## 1. Przegląd i Priorytety

### Status Obecnej Implementacji

| User Story | Status | Priorytet | Wymagane Działania |
|-----------|--------|-----------|-------------------|
| **US 11** | ❌ Usunięte przez migrację | **KRYTYCZNY** | Przywrócić wpis powitalny |
| **US 8** | ❌ Nie zaimplementowane | **KRYTYCZNY** | Dodać logout w NavMenu/Settings |
| **US 12** | ⚠️ Tylko placeholder | **WYSOKI** | Dynamiczne pobieranie wpisów |
| **US 10** | ⚠️ Tylko placeholder | **WYSOKI** | Połączyć z UpdatePassword |

### Kryteria Sukcesu MVP

Aby projekt spełniał minimalne wymagania MVP, **wszystkie cztery** User Stories muszą być w pełni zaimplementowane i przetestowane.

---

## 2. US 11: Wpis Powitalny dla Nowego Użytkownika (KRYTYCZNY)

### 2.1. Analiza Problemu

**Obecny stan:**
- Migracja `20251012123000_remove_automation_triggers.sql` usunęła trigger `on_profile_created`
- Nowi użytkownicy widzą pustą listę wpisów po rejestracji
- Brak onboardingu wyjaśniającego cel aplikacji

**Wymagania PRD:**
> "Nowy użytkownik po zalogowaniu widzi pierwszy, automatycznie wygenerowany wpis powitalny, który wyjaśnia cel aplikacji i zachęca do działania."

### 2.2. Strategia Implementacji

**Rekomendacja:** Implementacja po stronie klienta (Blazor), wykonywana podczas pierwszego logowania użytkownika.

**Uzasadnienie:**
- ✅ Prostsze w implementacji i debugowaniu
- ✅ Nie wymaga modyfikacji bazy danych ani nowych migracji
- ✅ Łatwiejsza aktualizacja treści powitalnej w przyszłości
- ✅ Zgodne z filozofią projektu (logika w kliencie, Supabase jako BaaS)
- ❌ Wymaga dodatkowego zapytania podczas pierwszego logowania

### 2.3. Architektura Rozwiązania

#### Lokalizacja w Vertical Slice Architecture

```
/Features
  /JournalEntries
    /CreateEntry
      - CreateEntry.razor (istniejący)
      - CreateEntry.cs (istniejący)
    /WelcomeEntry
      - WelcomeEntryService.cs (NOWY)
```

#### Punkt Integracji

Wpis powitalny zostanie utworzony automatycznie w komponencie `ListEntries.razor` podczas pierwszego załadowania listy, jeśli użytkownik nie ma jeszcze żadnych wpisów.

### 2.4. Szczegółowa Implementacja

#### Krok 1: Utworzenie `WelcomeEntryService.cs`

**Plik:** `Features/JournalEntries/WelcomeEntry/WelcomeEntryService.cs`

```csharp
using _10xJournal.Client.Features.JournalEntries.Models;
using Supabase;
using Microsoft.Extensions.Logging;

namespace _10xJournal.Client.Features.JournalEntries.WelcomeEntry;

/// <summary>
/// Service responsible for creating the welcome entry for new users.
/// </summary>
public class WelcomeEntryService
{
    private readonly Client _supabaseClient;
    private readonly ILogger<WelcomeEntryService> _logger;
    
    // Welcome entry content in Polish
    private const string WELCOME_CONTENT = @"Witaj w 10xJournal!

To jest Twoja prywatna przestrzeń do myślenia i pisania, wolna od rozpraszaczy. Celem tej aplikacji jest pomóc Ci w budowaniu nawyku regularnego prowadzenia dziennika.

Możesz edytować lub usunąć ten wpis. Kliknij przycisk 'Nowy wpis', aby rozpocząć swoją historię.";

    public WelcomeEntryService(Client supabaseClient, ILogger<WelcomeEntryService> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    /// <summary>
    /// Creates a welcome entry for the authenticated user if they have no entries yet.
    /// </summary>
    /// <returns>True if welcome entry was created, false if user already has entries or creation failed.</returns>
    public async Task<bool> CreateWelcomeEntryIfNeededAsync()
    {
        try
        {
            // Check if user already has any entries
            var existingEntries = await _supabaseClient
                .From<JournalEntry>()
                .Select("id")
                .Limit(1)
                .Get();

            if (existingEntries?.Models?.Count > 0)
            {
                _logger.LogDebug("User already has entries, skipping welcome entry creation");
                return false;
            }

            // Get current user ID
            var user = _supabaseClient.Auth.CurrentUser;
            if (user == null)
            {
                _logger.LogWarning("Cannot create welcome entry: user not authenticated");
                return false;
            }

            // Create welcome entry
            var welcomeEntry = new JournalEntry
            {
                UserId = Guid.Parse(user.Id),
                Content = WELCOME_CONTENT,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var response = await _supabaseClient
                .From<JournalEntry>()
                .Insert(welcomeEntry);

            if (response?.Models?.Count > 0)
            {
                _logger.LogInformation("Welcome entry created successfully for user {UserId}", user.Id);
                return true;
            }

            _logger.LogWarning("Welcome entry creation returned empty response");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating welcome entry");
            return false;
        }
    }
}
```

#### Krok 2: Rejestracja Serwisu w DI

**Plik:** `Program.cs`

Dodaj rejestrację serwisu w kontenerze DI:

```csharp
// W sekcji builder.Services

// Existing registrations...
builder.Services.AddScoped<IAuthService, AuthService>();

// ADD THIS LINE:
builder.Services.AddScoped<WelcomeEntryService>();
```

#### Krok 3: Integracja w `ListEntries.razor`

**Plik:** `Features/JournalEntries/ListEntries/ListEntries.razor`

Zmodyfikuj komponent, aby automatycznie tworzyć wpis powitalny przy pierwszym załadowaniu:

```razor
@page "/journal"
@using _10xJournal.Client.Features.JournalEntries.Models
@using _10xJournal.Client.Features.JournalEntries.WelcomeEntry
@inject Supabase.Client SupabaseClient
@inject WelcomeEntryService WelcomeEntryService
@inject ILogger<ListEntries> Logger

<PageTitle>10xJournal - Twoje Wpisy</PageTitle>

<h1>Twoje wpisy</h1>

@if (_isLoading)
{
    <p aria-live="polite">Ładowanie wpisów...</p>
}
else if (_entries == null || !_entries.Any())
{
    <p>Nie masz jeszcze żadnych wpisów. Kliknij "Nowy wpis", aby rozpocząć!</p>
}
else
{
    <ul>
        @foreach (var entry in _entries)
        {
            <li>
                <a href="/journal/entry/@entry.Id">
                    <strong>@GetEntryTitle(entry)</strong>
                    <br />
                    <small>@entry.CreatedAt.ToString("d MMMM yyyy, HH:mm")</small>
                </a>
            </li>
        }
    </ul>
}

<a href="/journal/new" role="button">Nowy wpis</a>

@code {
    private List<JournalEntry>? _entries;
    private bool _isLoading = true;
    private bool _welcomeEntryCreated = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadEntriesAsync();
    }

    private async Task LoadEntriesAsync()
    {
        try
        {
            _isLoading = true;

            // Attempt to load entries
            var response = await SupabaseClient
                .From<JournalEntry>()
                .Select("*")
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            _entries = response?.Models ?? new List<JournalEntry>();

            // If no entries exist and welcome entry not yet created, create it
            if (_entries.Count == 0 && !_welcomeEntryCreated)
            {
                _welcomeEntryCreated = await WelcomeEntryService.CreateWelcomeEntryIfNeededAsync();
                
                if (_welcomeEntryCreated)
                {
                    // Reload entries to show the welcome entry
                    await LoadEntriesAsync();
                    return; // Exit to prevent setting _isLoading to false twice
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading journal entries");
            _entries = new List<JournalEntry>();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private string GetEntryTitle(JournalEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Content))
        {
            return "(Pusty wpis)";
        }

        // Get first line or first 100 characters as title
        var firstLine = entry.Content.Split('\n')[0];
        return firstLine.Length > 100 
            ? firstLine.Substring(0, 100) + "..." 
            : firstLine;
    }
}
```

### 2.5. Testowanie US 11

**Scenariusze testowe:**

1. **Nowy użytkownik:**
   - Zarejestruj nowe konto
   - Zaloguj się
   - Przejdź do `/journal`
   - ✅ Sprawdź, czy wpis powitalny jest widoczny
   - ✅ Sprawdź, czy treść jest zgodna z `WELCOME_CONTENT`

2. **Istniejący użytkownik:**
   - Zaloguj się jako użytkownik z wpisami
   - Przejdź do `/journal`
   - ✅ Sprawdź, że DRUGI wpis powitalny NIE został utworzony

3. **Obsługa błędów:**
   - Symuluj błąd sieciowy podczas tworzenia wpisu
   - ✅ Sprawdź, że aplikacja nie zawiesza się
   - ✅ Sprawdź, że błąd jest logowany

---

## 3. US 8: Funkcjonalność Wylogowania (KRYTYCZNY)

### 3.1. Analiza Problemu

**Obecny stan:**
- Brak jakiejkolwiek funkcji wylogowania w aplikacji
- Użytkownik nie może bezpiecznie zakończyć sesji
- Brak wywołań `Auth.SignOut()` w kodzie

**Wymagania PRD:**
> "Jako zalogowany użytkownik, chcę mieć możliwość wylogowania się z aplikacji, aby zabezpieczyć dostęp do mojego konta na współdzielonym urządzeniu."

### 3.2. Strategia Implementacji

**Podejście:** Dodanie przycisku wylogowania w dwóch miejscach:
1. **NavMenu.razor** - dla łatwego dostępu na wszystkich stronach
2. **Settings.razor** - jako część zarządzania kontem

### 3.3. Architektura Rozwiązania

#### Lokalizacja w Vertical Slice Architecture

```
/Features
  /Authentication
    /Logout
      - LogoutHandler.cs (NOWY)
/Layout
  - NavMenu.razor (MODYFIKACJA)
/Features
  /Settings
    - Settings.razor (MODYFIKACJA)
```

### 3.4. Szczegółowa Implementacja

#### Krok 1: Utworzenie `LogoutHandler.cs`

**Plik:** `Features/Authentication/Logout/LogoutHandler.cs`

```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Supabase;

namespace _10xJournal.Client.Features.Authentication.Logout;

/// <summary>
/// Handles user logout operations.
/// Encapsulates logout logic following Vertical Slice Architecture.
/// </summary>
public class LogoutHandler
{
    private readonly Client _supabaseClient;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(
        Client supabaseClient, 
        NavigationManager navigationManager,
        ILogger<LogoutHandler> logger)
    {
        _supabaseClient = supabaseClient;
        _navigationManager = navigationManager;
        _logger = logger;
    }

    /// <summary>
    /// Logs out the current user and redirects to login page.
    /// </summary>
    /// <returns>Task representing the asynchronous operation.</returns>
    public async Task LogoutAsync()
    {
        try
        {
            _logger.LogInformation("User logout initiated");

            // Sign out from Supabase Auth
            await _supabaseClient.Auth.SignOut();

            _logger.LogInformation("User logged out successfully");

            // Redirect to login page
            _navigationManager.NavigateTo("/login", forceLoad: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            
            // Even if logout fails on server, clear client state and redirect
            // This ensures user can't remain in a half-logged-out state
            _navigationManager.NavigateTo("/login", forceLoad: true);
        }
    }
}
```

#### Krok 2: Rejestracja w DI

**Plik:** `Program.cs`

```csharp
// ADD THIS LINE:
builder.Services.AddScoped<LogoutHandler>();
```

#### Krok 3: Modyfikacja `NavMenu.razor`

**Plik:** `Layout/NavMenu.razor`

```razor
@using _10xJournal.Client.Features.Authentication.Logout
@inject LogoutHandler LogoutHandler

<nav>
    <ul>
        <li><a href="/journal">Dziennik</a></li>
        <li><a href="/journal/new">Nowy wpis</a></li>
        <li><a href="/settings">Ustawienia</a></li>
        <li>
            <button @onclick="HandleLogoutAsync" type="button" class="secondary">
                Wyloguj
            </button>
        </li>
    </ul>
</nav>

@code {
    private async Task HandleLogoutAsync()
    {
        await LogoutHandler.LogoutAsync();
    }
}
```

#### Krok 4: Modyfikacja `Settings.razor`

**Plik:** `Features/Settings/Settings.razor`

```razor
@page "/settings"
@using _10xJournal.Client.Features.Authentication.Logout
@inject LogoutHandler LogoutHandler
@inject NavigationManager NavigationManager

<PageTitle>Ustawienia - 10xJournal</PageTitle>

<h1>Ustawienia</h1>

<section>
    <h2>Zmiana hasła</h2>
    <p>Zaktualizuj hasło do swojego konta.</p>
    <button @onclick="NavigateToChangePassword">Zmień hasło</button>
</section>

<section>
    <h2>Bezpieczeństwo konta</h2>
    <button @onclick="HandleLogoutAsync" class="secondary">Wyloguj się</button>
</section>

<section>
    <h2>Usuwanie konta</h2>
    <p>Trwale usuń swoje konto i wszystkie dane.</p>
    <button class="contrast" disabled>Usuń konto (wkrótce)</button>
</section>

@code {
    private async Task HandleLogoutAsync()
    {
        await LogoutHandler.LogoutAsync();
    }

    private void NavigateToChangePassword()
    {
        NavigationManager.NavigateTo("/update-password");
    }
}
```

### 3.5. Testowanie US 8

**Scenariusze testowe:**

1. **Wylogowanie z NavMenu:**
   - Zaloguj się
   - Kliknij przycisk "Wyloguj" w NavMenu
   - ✅ Sprawdź przekierowanie na `/login`
   - ✅ Spróbuj dostać się do `/journal` - powinno przekierować na login

2. **Wylogowanie z Settings:**
   - Zaloguj się
   - Przejdź do `/settings`
   - Kliknij "Wyloguj się"
   - ✅ Sprawdź przekierowanie na `/login`

3. **Sprawdzenie czyszczenia sesji:**
   - Wyloguj się
   - Otwórz DevTools → Application → Local Storage
   - ✅ Sprawdź, że tokeny Supabase zostały usunięte

---

## 4. US 12: Dynamiczna Lista Wpisów (WYSOKI PRIORYTET)

### 4.1. Analiza Problemu

**Obecny stan:**
- `ListEntries.razor` ma tylko hardcoded placeholder
- Brak pobierania danych z Supabase
- Nie spełnia wymagania sortowania (najnowsze → najstarsze)

**Wymagania PRD:**
> "Wpisy są wyświetlane na liście w porządku odwrotnie chronologicznym. Każdy element listy pokazuje datę utworzenia oraz pierwszą linijkę tekstu (traktowaną jako tytuł)."

### 4.2. Strategia Implementacji

Większość implementacji została już wykonana w [Kroku 3 US 11](#krok-3-integracja-w-listentriesrazor). Komponenta `ListEntries.razor` zawiera:

- ✅ Dynamiczne pobieranie wpisów z Supabase
- ✅ Sortowanie od najnowszych (`ORDER BY created_at DESC`)
- ✅ Wyświetlanie pierwszej linii jako tytułu (max 100 znaków)
- ✅ Formatowanie daty
- ✅ Stan ładowania
- ✅ Obsługa pustej listy

### 4.3. Dodatkowe Ulepszenia

#### Dodanie CSS dla Lepszej Prezentacji

**Plik:** `Features/JournalEntries/ListEntries/ListEntries.razor.css`

```css
/* Isolated CSS for ListEntries component */

h1 {
    margin-bottom: 1rem;
}

ul {
    list-style: none;
    padding: 0;
    margin: 2rem 0;
}

li {
    margin-bottom: 1rem;
    padding: 1rem;
    border: 1px solid var(--muted-border-color);
    border-radius: var(--border-radius);
    transition: background-color 0.2s ease;
}

li:hover {
    background-color: var(--card-background-color);
}

li a {
    display: block;
    text-decoration: none;
    color: inherit;
}

li strong {
    font-size: 1.1rem;
    color: var(--primary);
}

li small {
    color: var(--muted-color);
}

/* Loading state */
[aria-live="polite"] {
    text-align: center;
    padding: 2rem;
    color: var(--muted-color);
}

/* Empty state */
p:not([aria-live]) {
    text-align: center;
    padding: 2rem;
    color: var(--muted-color);
}
```

### 4.4. Testowanie US 12

**Scenariusze testowe:**

1. **Lista z wpisami:**
   - Zaloguj się jako użytkownik z wpisami
   - ✅ Sprawdź, czy wpisy są wyświetlane
   - ✅ Sprawdź sortowanie (najnowsze na górze)
   - ✅ Sprawdź format daty
   - ✅ Sprawdź, że tytuł to pierwsza linia (max 100 znaków)

2. **Pusty stan:**
   - Zaloguj się jako nowy użytkownik (po utworzeniu wpisu powitalnego)
   - Usuń wpis powitalny
   - ✅ Sprawdź komunikat "Nie masz jeszcze żadnych wpisów"

3. **Stan ładowania:**
   - Symuluj wolne połączenie (DevTools → Network → Slow 3G)
   - Odśwież stronę `/journal`
   - ✅ Sprawdź, czy wyświetla się "Ładowanie wpisów..."

4. **Długie wpisy:**
   - Utwórz wpis z pierwszą linią dłuższą niż 100 znaków
   - ✅ Sprawdź, czy tytuł jest obcięty do 100 znaków z "..."

---

## 5. US 10: Zmiana Hasła w Ustawieniach (WYSOKI PRIORYTET)

### 5.1. Analiza Problemu

**Obecny stan:**
- `Settings.razor` ma przycisk "Change Password" bez funkcjonalności
- Komponent `UpdatePasswordPage.razor` już istnieje i obsługuje reset hasła z emaila
- Brak połączenia między Settings a funkcją zmiany hasła

**Wymagania PRD:**
> "Jako zalogowany użytkownik, chcę mieć dostęp do strony 'Ustawienia', gdzie mogę zmienić swoje hasło, aby dbać o bezpieczeństwo konta."

### 5.2. Strategia Implementacji

**Podejście:** Utworzenie dedykowanego komponentu `ChangePasswordForm.razor` w Settings, który pozwala zalogowanemu użytkownikowi zmienić hasło bez potrzeby resetu przez email.

### 5.3. Architektura Rozwiązania

```
/Features
  /Settings
    - Settings.razor (MODYFIKACJA)
    /ChangePassword
      - ChangePasswordForm.razor (NOWY)
      - ChangePasswordModel.cs (NOWY)
```

### 5.4. Szczegółowa Implementacja

#### Krok 1: Utworzenie `ChangePasswordModel.cs`

**Plik:** `Features/Settings/ChangePassword/ChangePasswordModel.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace _10xJournal.Client.Features.Settings.ChangePassword;

/// <summary>
/// Model for changing password in settings.
/// </summary>
public class ChangePasswordModel
{
    [Required(ErrorMessage = "Obecne hasło jest wymagane.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nowe hasło jest wymagane.")]
    [MinLength(6, ErrorMessage = "Hasło musi mieć co najmniej 6 znaków.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Hasła muszą być identyczne.")]
    [DataType(DataType.Password)]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
```

#### Krok 2: Utworzenie `ChangePasswordForm.razor`

**Plik:** `Features/Settings/ChangePassword/ChangePasswordForm.razor`

```razor
@using Microsoft.AspNetCore.Components.Forms
@using _10xJournal.Client.Features.Settings.ChangePassword
@inject Supabase.Client SupabaseClient
@inject ILogger<ChangePasswordForm> Logger

<EditForm Model="_model" OnValidSubmit="HandleValidSubmit">
    <DataAnnotationsValidator />
    
    @if (!string.IsNullOrWhiteSpace(_errorMessage))
    {
        <p role="alert" style="color: var(--del-color)">@_errorMessage</p>
    }
    
    @if (_submitSuccess)
    {
        <p role="status" style="color: var(--ins-color)">
            Hasło zostało zmienione pomyślnie! Zostaniesz wylogowany za @_redirectCountdown sekund...
        </p>
    }

    <fieldset>
        <label for="current-password">Obecne hasło</label>
        <InputText 
            id="current-password" 
            @bind-Value="_model.CurrentPassword" 
            type="password" 
            autocomplete="current-password"
            disabled="@_isSubmitting" />
        <ValidationMessage For="() => _model.CurrentPassword" />

        <label for="new-password">Nowe hasło</label>
        <InputText 
            id="new-password" 
            @bind-Value="_model.NewPassword" 
            type="password" 
            autocomplete="new-password"
            disabled="@_isSubmitting" />
        <ValidationMessage For="() => _model.NewPassword" />

        <label for="confirm-new-password">Potwierdź nowe hasło</label>
        <InputText 
            id="confirm-new-password" 
            @bind-Value="_model.ConfirmNewPassword" 
            type="password" 
            autocomplete="new-password"
            disabled="@_isSubmitting" />
        <ValidationMessage For="() => _model.ConfirmNewPassword" />
    </fieldset>

    <button type="submit" disabled="@(_isSubmitting || _submitSuccess)">
        @(_isSubmitting ? "Zmieniam hasło..." : "Zmień hasło")
    </button>
</EditForm>

@code {
    private ChangePasswordModel _model = new();
    private bool _isSubmitting;
    private bool _submitSuccess;
    private string? _errorMessage;
    private int _redirectCountdown = 5;
    private System.Timers.Timer? _redirectTimer;

    [Parameter]
    public EventCallback OnPasswordChanged { get; set; }

    private async Task HandleValidSubmit()
    {
        if (_isSubmitting)
            return;

        _isSubmitting = true;
        _errorMessage = null;
        _submitSuccess = false;

        try
        {
            // Verify current password by attempting to re-authenticate
            var currentUser = SupabaseClient.Auth.CurrentUser;
            if (currentUser == null || string.IsNullOrEmpty(currentUser.Email))
            {
                _errorMessage = "Nie można zweryfikować użytkownika. Zaloguj się ponownie.";
                return;
            }

            // Attempt to sign in with current password to verify it's correct
            try
            {
                await SupabaseClient.Auth.SignIn(currentUser.Email, _model.CurrentPassword);
            }
            catch
            {
                _errorMessage = "Obecne hasło jest nieprawidłowe.";
                return;
            }

            // Update password
            var updateResult = await SupabaseClient.Auth.Update(new Supabase.Gotrue.UserAttributes
            {
                Password = _model.NewPassword
            });

            if (updateResult?.User != null)
            {
                Logger.LogInformation("Password changed successfully for user {UserId}", currentUser.Id);
                _submitSuccess = true;
                
                // Start countdown and redirect to login
                StartRedirectCountdown();
                
                if (OnPasswordChanged.HasDelegate)
                {
                    await OnPasswordChanged.InvokeAsync();
                }
            }
            else
            {
                _errorMessage = "Nie udało się zmienić hasła. Spróbuj ponownie.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error changing password");
            _errorMessage = "Wystąpił błąd podczas zmiany hasła. Spróbuj ponownie później.";
        }
        finally
        {
            _isSubmitting = false;
            StateHasChanged();
        }
    }

    private void StartRedirectCountdown()
    {
        _redirectTimer = new System.Timers.Timer(1000);
        _redirectTimer.Elapsed += async (sender, e) =>
        {
            _redirectCountdown--;
            await InvokeAsync(StateHasChanged);

            if (_redirectCountdown <= 0)
            {
                _redirectTimer?.Stop();
                _redirectTimer?.Dispose();
                
                // Sign out and redirect to login
                await SupabaseClient.Auth.SignOut();
                await InvokeAsync(() =>
                {
                    var navManager = (Microsoft.AspNetCore.Components.NavigationManager)
                        SupabaseClient.GetType().GetProperty("NavigationManager")?.GetValue(SupabaseClient)!;
                    navManager?.NavigateTo("/login", forceLoad: true);
                });
            }
        };
        _redirectTimer.Start();
    }

    public void Dispose()
    {
        _redirectTimer?.Stop();
        _redirectTimer?.Dispose();
    }
}
```

#### Krok 3: Aktualizacja `Settings.razor`

**Plik:** `Features/Settings/Settings.razor`

Zaktualizuj komponent, aby zawierał formularz zmiany hasła (kod już przedstawiony w [Kroku 4 US 8](#krok-4-modyfikacja-settingsrazor), ale dodaj sekcję z formularzem):

```razor
@page "/settings"
@using _10xJournal.Client.Features.Authentication.Logout
@using _10xJournal.Client.Features.Settings.ChangePassword
@inject LogoutHandler LogoutHandler
@inject NavigationManager NavigationManager

<PageTitle>Ustawienia - 10xJournal</PageTitle>

<h1>Ustawienia</h1>

<section>
    <h2>Zmiana hasła</h2>
    <p>Zaktualizuj hasło do swojego konta.</p>
    
    @if (_showChangePasswordForm)
    {
        <ChangePasswordForm OnPasswordChanged="HandlePasswordChanged" />
        <button @onclick="CancelChangePassword" class="secondary">Anuluj</button>
    }
    else
    {
        <button @onclick="ShowChangePasswordForm">Zmień hasło</button>
    }
</section>

<section>
    <h2>Bezpieczeństwo konta</h2>
    <button @onclick="HandleLogoutAsync" class="secondary">Wyloguj się</button>
</section>

<section>
    <h2>Usuwanie konta</h2>
    <p>Trwale usuń swoje konto i wszystkie dane.</p>
    <button class="contrast" disabled>Usuń konto (wkrótce)</button>
</section>

@code {
    private bool _showChangePasswordForm = false;

    private async Task HandleLogoutAsync()
    {
        await LogoutHandler.LogoutAsync();
    }

    private void ShowChangePasswordForm()
    {
        _showChangePasswordForm = true;
    }

    private void CancelChangePassword()
    {
        _showChangePasswordForm = false;
    }

    private async Task HandlePasswordChanged()
    {
        // Password was changed, user will be logged out automatically
        // by the ChangePasswordForm component
    }
}
```

### 5.5. Alternatywne Podejście (Prostsze)

Jeśli powyższe podejście jest zbyt skomplikowane, można po prostu dodać link do istniejącego `/update-password`:

```razor
<section>
    <h2>Zmiana hasła</h2>
    <p>Zaktualizuj hasło do swojego konta.</p>
    <a href="/update-password" role="button">Zmień hasło</a>
</section>
```

**Uwaga:** To wymaga modyfikacji `UpdatePasswordPage.razor`, aby działało też dla zalogowanych użytkowników (obecnie obsługuje tylko reset z emaila).

### 5.6. Testowanie US 10

**Scenariusze testowe:**

1. **Poprawna zmiana hasła:**
   - Zaloguj się
   - Przejdź do `/settings`
   - Kliknij "Zmień hasło"
   - Wpisz obecne hasło i nowe hasło (2x)
   - ✅ Sprawdź komunikat sukcesu
   - ✅ Sprawdź automatyczne wylogowanie po 5 sekundach
   - ✅ Zaloguj się ponownie z nowym hasłem

2. **Nieprawidłowe obecne hasło:**
   - Wpisz błędne obecne hasło
   - ✅ Sprawdź komunikat "Obecne hasło jest nieprawidłowe"

3. **Niezgodne nowe hasła:**
   - Wpisz różne wartości w "Nowe hasło" i "Potwierdź"
   - ✅ Sprawdź walidację "Hasła muszą być identyczne"

4. **Zbyt krótkie hasło:**
   - Wpisz hasło krótsze niż 6 znaków
   - ✅ Sprawdź komunikat walidacji

---

## 6. Kolejność Implementacji

### Rekomendowana Kolejność (Priorytet)

1. **US 8: Wylogowanie** (1-2 godziny)
   - Najszybsze do zaimplementowania
   - Krytyczne dla bezpieczeństwa
   - Nie ma zależności

2. **US 11: Wpis Powitalny** (2-3 godziny)
   - Krytyczne dla UX nowych użytkowników
   - Integruje się z US 12

3. **US 12: Dynamiczna Lista** (1 godzina)
   - Większość już zaimplementowana w US 11
   - Tylko dodanie CSS

4. **US 10: Zmiana Hasła** (3-4 godziny)
   - Najbardziej złożone
   - Najmniej krytyczne (użytkownik może użyć "Zapomniałem hasła")

### Szacowany Czas Całkowity: **7-10 godzin**

---

## 7. Testowanie

### 7.1. Checklist Funkcjonalny

Po zaimplementowaniu wszystkich User Stories, wykonaj pełny test przepływu:

- [ ] **Nowy użytkownik:**
  1. [ ] Zarejestruj konto na `/register`
  2. [ ] Zaloguj się na `/login`
  3. [ ] Przejdź do `/journal`
  4. [ ] **US 11:** Sprawdź wpis powitalny
  5. [ ] **US 12:** Sprawdź, że lista jest pusta po usunięciu wpisu powitalnego
  6. [ ] Utwórz nowy wpis
  7. [ ] **US 12:** Sprawdź, że nowy wpis jest na górze listy
  8. [ ] Przejdź do `/settings`
  9. [ ] **US 10:** Zmień hasło
  10. [ ] **US 8:** Wyloguj się z NavMenu
  11. [ ] Zaloguj się ponownie z nowym hasłem

- [ ] **Istniejący użytkownik:**
  1. [ ] Zaloguj się
  2. [ ] **US 11:** Sprawdź, że drugi wpis powitalny NIE został utworzony
  3. [ ] **US 12:** Sprawdź listę wpisów (sortowanie, tytuły, daty)
  4. [ ] **US 8:** Wyloguj się z Settings
  5. [ ] Zaloguj się ponownie

### 7.2. Testy Bezpieczeństwa

- [ ] **US 8:** Token Supabase jest usuwany po wylogowaniu
- [ ] **US 11:** Użytkownik widzi tylko swoje wpisy (RLS)
- [ ] **US 10:** Stare hasło jest wymagane przed zmianą
- [ ] XSS: Treść wpisów jest escapowana w HTML

---

## Podsumowanie

Ten plan implementacji pokrywa wszystkie brakujące User Stories wymagane dla MVP:

| US | Tytuł | Status po implementacji | Czas |
|----|-------|-------------------------|------|
| **11** | Wpis powitalny | ✅ Pełna implementacja | 2-3h |
| **8** | Wylogowanie | ✅ Pełna implementacja | 1-2h |
| **12** | Lista wpisów | ✅ Pełna implementacja | 1h |
| **10** | Zmiana hasła | ✅ Pełna implementacja | 3-4h |

**Łącznie:** 7-10 godzin pracy developera.

Po zaimplementowaniu wszystkich punktów, aplikacja 10xJournal będzie spełniać **100% wymagań MVP** określonych w PRD.

---

**Następne kroki:**
1. Przegląd planu z zespołem
2. Utworzenie tasków w systemie zarządzania projektem
3. Rozpoczęcie implementacji zgodnie z kolejnością priorytetów
4. Code review po każdym US
5. Testy integracyjne po wszystkich implementacjach
