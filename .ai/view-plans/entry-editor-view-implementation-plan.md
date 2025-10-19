# Plan implementacji widoku Edytora Wpisu

## 1. Przegląd

Widok Edytora Wpisu to kluczowy komponent aplikacji 10xJournal, który zapewnia użytkownikom wolną od rozpraszaczy przestrzeń do tworzenia i edytowania wpisów dziennika. Widok obsługuje dwa scenariusze w ramach jednego komponentu: tworzenie nowych wpisów oraz edycję istniejących. Głównym celem jest zapewnienie prostego, intuicyjnego interfejsu z automatycznym zapisem, obsługą pracy offline oraz ochroną przed przypadkową utratą danych.

## 2. Routing widoku

Widok jest dostępny pod dwoma ścieżkami w ramach tego samego komponentu Blazor:

- **Tworzenie nowego wpisu**: `/app/entry/new`
- **Edycja istniejącego wpisu**: `/app/entry/{id}` gdzie `{id}` to GUID identyfikujący wpis

Komponent rozpoznaje tryb pracy na podstawie obecności parametru `id` w trasie.

## 3. Struktura komponentów

```
EntryEditor.razor (główny komponent strony)
├── header
│   ├── Przycisk "Wróć"
│   ├── SaveStatusIndicator (komponent potomny)
│   └── Przycisk "Usuń" (widoczny tylko w trybie edycji)
└── main
    └── textarea (pole do wprowadzania treści)
```

### Hierarchia komponentów:
- **EntryEditor.razor** - główny kontener i logika widoku
- **SaveStatusIndicator.razor** - wskaźnik statusu zapisu (komponent potomny, bezstanowy)

## 4. Szczegóły komponentów

### EntryEditor.razor

**Opis komponentu:**
Główny komponent odpowiedzialny za cały widok edytora. Obsługuje zarówno tworzenie nowych wpisów, jak i edycję istniejących. Zawiera logikę automatycznego zapisu z opóźnieniem (debouncing), obsługę pracy offline poprzez LocalStorage, oraz wszystkie interakcje użytkownika.

**Główne elementy HTML i komponenty potomne:**
```razor
<div class="entry-editor">
    <header class="editor-header">
        <button type="button" @onclick="NavigateBack" class="btn-back">← Wróć</button>
        <SaveStatusIndicator Status="@_saveStatus" LastSavedAt="@_lastSavedAt" />
        @if (_entryId.HasValue)
        {
            <button type="button" @onclick="HandleDeleteAsync" class="btn-delete">Usuń</button>
        }
    </header>
    
    @if (_isLoading)
    {
        <div class="loading-indicator">Ładowanie wpisu...</div>
    }
    else
    {
        <main class="editor-main">
            <textarea 
                @ref="_textareaRef"
                @oninput="HandleInput"
                value="@_content"
                placeholder="Zacznij pisać..."
                rows="20"
                aria-label="Treść wpisu">
            </textarea>
        </main>
    }
    
    @if (!string.IsNullOrEmpty(_errorMessage))
    {
        <div class="error-message" role="alert">@_errorMessage</div>
    }
</div>
```

**Obsługiwane zdarzenia:**
- `@oninput` na textarea - przechwytuje każdą zmianę treści, uruchamia timer dla opóźnionego zapisu
- `@onclick` na przycisku "Wróć" - nawigacja z powrotem do listy wpisów
- `@onclick` na przycisku "Usuń" - obsługa usuwania wpisu z potwierdzeniem
- `OnInitializedAsync` - lifecycle event do ładowania istniejącego wpisu w trybie edycji
- `OnAfterRenderAsync` - lifecycle event do ustawienia auto-focus na textarea w trybie tworzenia

**Obsługiwana walidacja:**
1. **Niepusta treść przed zapisem**:
   - Warunek: `!string.IsNullOrWhiteSpace(_content)`
   - Miejsce: przed wywołaniem API POST/PATCH
   - Działanie: jeśli treść jest pusta, nie wysyłaj żądania do API
   
2. **Minimalny rozmiar treści**:
   - Warunek: `_content.Trim().Length > 0`
   - Miejsce: w metodzie `AutoSaveAsync()`
   - Działanie: zapobiega tworzeniu pustych wpisów

3. **Weryfikacja formatu ID** (w trybie edycji):
   - Warunek: `Id.HasValue && Id.Value != Guid.Empty`
   - Miejsce: w `OnInitializedAsync()`
   - Działanie: jeśli ID jest nieprawidłowe, przekieruj do listy

**Typy wykorzystywane przez komponent:**
- `JournalEntry` - model danych wpisu (z Supabase)
- `SaveStatus` - enum definiujący stany zapisu
- `Guid?` - opcjonalny identyfikator wpisu
- `ElementReference` - referencja do elementu textarea
- `Timer` - dla implementacji debouncing

**Propsy (parametry) przyjmowane przez komponent:**
```csharp
[Parameter]
public Guid? Id { get; set; }
```
- `Id` - opcjonalny identyfikator wpisu; jeśli jest `null`, komponent działa w trybie tworzenia nowego wpisu, jeśli ma wartość - w trybie edycji

### SaveStatusIndicator.razor

**Opis komponentu:**
Prosty, bezstanowy komponent wyświetlający aktualny status operacji zapisu. Komunikuje użytkownikowi, czy dane są w trakcie zapisu, zostały zapisane pomyślnie, czy wystąpił błąd.

**Główne elementy HTML:**
```razor
<div class="save-status-indicator" aria-live="polite">
    @switch (Status)
    {
        case SaveStatus.Idle:
            <span class="status-idle"></span>
            break;
        case SaveStatus.Saving:
            <span class="status-saving">Zapisywanie...</span>
            break;
        case SaveStatus.Saved:
            <span class="status-saved">
                Zapisano @if (LastSavedAt.HasValue) { <text>o @LastSavedAt.Value.ToString("HH:mm")</text> }
            </span>
            break;
        case SaveStatus.Error:
            <span class="status-error">Błąd zapisu</span>
            break;
    }
</div>
```

**Obsługiwane zdarzenia:**
Brak - komponent jest czysto prezentacyjny.

**Obsługiwana walidacja:**
Brak - komponent tylko wyświetla dane.

**Typy wykorzystywane przez komponent:**
- `SaveStatus` - enum określający aktualny stan
- `DateTime?` - opcjonalny timestamp ostatniego zapisu

**Propsy przyjmowane przez komponent:**
```csharp
[Parameter]
public SaveStatus Status { get; set; }

[Parameter]
public DateTime? LastSavedAt { get; set; }
```

## 5. Typy

### SaveStatus (enum)

Enum definiujący możliwe stany operacji zapisu.

**Lokalizacja:** `Features/JournalEntries/EditEntry/SaveStatus.cs`

```csharp
namespace _10xJournal.Client.Features.JournalEntries.EditEntry;

/// <summary>
/// Represents the current state of the auto-save operation.
/// </summary>
public enum SaveStatus
{
    /// <summary>
    /// No save operation is currently in progress.
    /// </summary>
    Idle,
    
    /// <summary>
    /// A save operation is currently in progress.
    /// </summary>
    Saving,
    
    /// <summary>
    /// The last save operation completed successfully.
    /// </summary>
    Saved,
    
    /// <summary>
    /// The last save operation failed.
    /// </summary>
    Error
}
```

### Istniejące typy (wykorzystywane ponownie)

**JournalEntry** - `Features/JournalEntries/Models/JournalEntry.cs`
```csharp
public class JournalEntry : BaseModel
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**CreateJournalEntryRequest** - do utworzenia w `Features/JournalEntries/EditEntry/CreateJournalEntryRequest.cs` (jeśli nie istnieje):
```csharp
namespace _10xJournal.Client.Features.JournalEntries.EditEntry;

public class CreateJournalEntryRequest
{
    public string Content { get; set; } = string.Empty;
}
```

**UpdateJournalEntryRequest** - do utworzenia w `Features/JournalEntries/EditEntry/UpdateJournalEntryRequest.cs` (jeśli nie istnieje):
```csharp
namespace _10xJournal.Client.Features.JournalEntries.EditEntry;

public class UpdateJournalEntryRequest
{
    public string Content { get; set; } = string.Empty;
}
```

## 6. Zarządzanie stanem

Stan widoku jest zarządzany **lokalnie w komponencie** `EntryEditor.razor` poprzez prywatne pola w bloku `@code`. Nie jest wymagane żadne globalne zarządzanie stanem ani zewnętrzne store'y.

### Pola stanu komponentu:

```csharp
private string _content = string.Empty;
private Guid? _entryId = null;
private SaveStatus _saveStatus = SaveStatus.Idle;
private DateTime? _lastSavedAt = null;
private bool _isLoading = false;
private string? _errorMessage = null;
private ElementReference _textareaRef;
private Timer? _autoSaveTimer = null;
```

### Opis pól:

- **`_content`**: Aktualna treść wprowadzana przez użytkownika w textarea. Jest to źródło prawdy dla stanu edytora.
- **`_entryId`**: Identyfikator wpisu. `null` w trybie tworzenia nowego wpisu, ustawiane na wartość GUID po pierwszym pomyślnym zapisie lub załadowaniu istniejącego wpisu.
- **`_saveStatus`**: Aktualny status operacji zapisu (Idle, Saving, Saved, Error).
- **`_lastSavedAt`**: Timestamp ostatniego pomyślnego zapisu, używany do wyświetlenia informacji "Zapisano o XX:XX".
- **`_isLoading`**: Flaga wskazująca, czy komponent jest w trakcie ładowania istniejącego wpisu z API.
- **`_errorMessage`**: Tekst komunikatu błędu do wyświetlenia użytkownikowi.
- **`_textareaRef`**: Referencja do elementu DOM textarea, używana do programowego ustawienia focusa.
- **`_autoSaveTimer`**: Timer wykorzystywany do implementacji mechanizmu debouncing dla auto-zapisu.

### Stałe:

```csharp
private const int AUTOSAVE_DELAY_MS = 1000;
private const string LOCALSTORAGE_KEY_PREFIX = "unsaved_entry_";
```

### Cykl życia stanu:

1. **Inicjalizacja** (`OnInitializedAsync`):
   - Jeśli `Id.HasValue` → tryb edycji → pobierz wpis z API → ustaw `_content` i `_entryId`
   - Jeśli `Id` jest `null` → tryb tworzenia → sprawdź LocalStorage czy są niezapisane dane

2. **Zmiana treści** (`HandleInput`):
   - Aktualizacja `_content`
   - Zapisanie do LocalStorage
   - Anulowanie poprzedniego timera i uruchomienie nowego (debouncing)

3. **Auto-zapis** (`AutoSaveAsync`):
   - Jeśli `_entryId` jest `null` → POST (tworzenie) → ustaw `_entryId` z odpowiedzi
   - Jeśli `_entryId` ma wartość → PATCH (aktualizacja)
   - Aktualizacja `_saveStatus` i `_lastSavedAt`
   - Wyczyszczenie LocalStorage po pomyślnym zapisie

4. **Usuwanie** (`HandleDeleteAsync`):
   - DELETE na API
   - Wyczyszczenie LocalStorage
   - Nawigacja do listy

## 7. Integracja API

### Wykorzystywane endpointy:

#### 1. Pobranie istniejącego wpisu (GET)

**Metoda API:** Implicit GET przez Supabase Client  
**Ścieżka:** `/rest/v1/journal_entries?id=eq.<entry_id>&select=*`  
**Kiedy:** W `OnInitializedAsync()` gdy `Id.HasValue == true`

**Implementacja:**
```csharp
var response = await _supabaseClient
    .From<JournalEntry>()
    .Where(x => x.Id == Id!.Value)
    .Single();
    
_content = response.Content;
_entryId = response.Id;
```

**Typ odpowiedzi:** `JournalEntry`

**Obsługa błędów:**
- 401 Unauthorized → przekierowanie do logowania
- 404 Not Found → komunikat "Wpis nie został znaleziony", nawigacja do listy
- Błąd sieciowy → komunikat "Brak połączenia", możliwość ponowienia

---

#### 2. Utworzenie nowego wpisu (POST)

**Metoda API:** POST  
**Ścieżka:** `/rest/v1/journal_entries`  
**Kiedy:** W `AutoSaveAsync()` gdy `_entryId == null` i treść nie jest pusta

**Typ żądania:** `CreateJournalEntryRequest`
```csharp
var request = new CreateJournalEntryRequest 
{ 
    Content = _content 
};
```

**Implementacja:**
```csharp
var response = await _supabaseClient
    .From<JournalEntry>()
    .Insert(request);
    
_entryId = response.Models.First().Id;
```

**Typ odpowiedzi:** `ModeledResponse<JournalEntry>` z kodem 201 Created

**Obsługa błędów:**
- 400 Bad Request → komunikat "Nieprawidłowe dane wpisu"
- 401 Unauthorized → przekierowanie do logowania
- Błąd sieciowy → zapis w LocalStorage, komunikat "Offline - zapisano lokalnie"

---

#### 3. Aktualizacja wpisu (PATCH)

**Metoda API:** PATCH  
**Ścieżka:** `/rest/v1/journal_entries?id=eq.<entry_id>`  
**Kiedy:** W `AutoSaveAsync()` gdy `_entryId.HasValue == true` i treść się zmieniła

**Typ żądania:** Bezpośrednia aktualizacja przez Supabase Client (nie wymaga osobnego DTO)

**Implementacja:**
```csharp
await _supabaseClient
    .From<JournalEntry>()
    .Where(x => x.Id == _entryId!.Value)
    .Set(x => x.Content, _content)
    .Update();
```

**Typ odpowiedzi:** `ModeledResponse<JournalEntry>` z kodem 200 OK

**Obsługa błędów:**
- 401 Unauthorized → przekierowanie do logowania
- 404 Not Found → komunikat "Wpis nie istnieje", nawigacja do listy
- Błąd sieciowy → zapis w LocalStorage, komunikat "Offline - zapisano lokalnie"

---

#### 4. Usunięcie wpisu (DELETE)

**Metoda API:** DELETE  
**Ścieżka:** `/rest/v1/journal_entries?id=eq.<entry_id>`  
**Kiedy:** W `HandleDeleteAsync()` po potwierdzeniu przez użytkownika

**Implementacja:**
```csharp
await _supabaseClient
    .From<JournalEntry>()
    .Where(x => x.Id == _entryId!.Value)
    .Delete();
```

**Typ odpowiedzi:** Kod 204 No Content (bez treści)

**Obsługa błędów:**
- 401 Unauthorized → przekierowanie do logowania
- 404 Not Found → komunikat "Wpis nie istnieje", nawigacja do listy
- Błąd sieciowy → komunikat "Nie można usunąć wpisu - brak połączenia"

## 8. Interakcje użytkownika

### 1. Tworzenie nowego wpisu

**Sekwencja:**
1. Użytkownik klika przycisk "Nowy wpis" na liście wpisów
2. Aplikacja nawiguje do `/app/entry/new`
3. Komponent `EntryEditor` renderuje się w trybie tworzenia (`Id == null`)
4. Textarea jest puste i automatycznie otrzymuje focus
5. Użytkownik zaczyna pisać
6. Po każdym naciśnięciu klawisza:
   - Treść jest zapisywana w LocalStorage (natychmiastowo)
   - Timer debouncing jest resetowany i uruchamiany ponownie (1000ms)
7. Po upływie 1 sekundy od ostatniego naciśnięcia klawisza:
   - Status zmienia się na "Zapisywanie..."
   - Wysyłane jest żądanie POST do API
   - Po pomyślnym zapisie, status zmienia się na "Zapisano o XX:XX"
   - `_entryId` jest ustawiane na ID zwrócone z API
   - Komponent przechodzi w tryb edycji (bez zmiany URL)
   - LocalStorage jest czyszczony

**Oczekiwany wynik:**
- Wpis jest tworzony w bazie danych
- Użytkownik widzi potwierdzenie zapisu
- Kolejne zmiany będą aktualizować istniejący wpis (PATCH zamiast POST)

---

### 2. Edycja istniejącego wpisu

**Sekwencja:**
1. Użytkownik klika na wpis z listy
2. Aplikacja nawiguje do `/app/entry/{id}`
3. Komponent `EntryEditor` renderuje się z parametrem `Id`
4. Wyświetlany jest wskaźnik ładowania
5. Komponent wysyła żądanie GET do API
6. Po otrzymaniu danych:
   - Textarea wypełnia się treścią wpisu
   - Wskaźnik ładowania znika
   - Przycisk "Usuń" jest widoczny
7. Użytkownik modyfikuje treść
8. Mechanizm auto-zapisu działa tak samo jak przy tworzeniu, ale używa PATCH

**Oczekiwany wynik:**
- Wpis jest aktualizowany w bazie danych
- Użytkownik widzi potwierdzenie zapisu
- Zmiany są trwałe

---

### 3. Usuwanie wpisu

**Sekwencja:**
1. Użytkownik jest w trybie edycji wpisu
2. Użytkownik klika przycisk "Usuń"
3. Wyświetla się natywny dialog potwierdzenia: "Czy na pewno chcesz usunąć ten wpis?"
4. Jeśli użytkownik kliknie "Anuluj":
   - Dialog się zamyka, nic się nie dzieje
5. Jeśli użytkownik kliknie "OK":
   - Wysyłane jest żądanie DELETE do API
   - Po pomyślnym usunięciu, użytkownik jest przekierowywany do `/app/entries`
   - LocalStorage jest czyszczony

**Oczekiwany wynik:**
- Wpis jest trwale usunięty z bazy danych
- Użytkownik wraca do listy wpisów
- Usunięty wpis nie jest już widoczny na liście

---

### 4. Nawigacja "Wróć"

**Sekwencja:**
1. Użytkownik klika przycisk "Wróć"
2. Aplikacja nawiguje do `/app/entries`

**Uwaga:** W MVP nie ma ostrzeżenia o niezapisanych zmianach, ponieważ auto-zapis działa w tle.

**Oczekiwany wynik:**
- Użytkownik wraca do listy wpisów
- Jeśli były niezapisane zmiany w LocalStorage, zostaną tam zachowane do następnego otwarcia edytora

---

### 5. Scenariusz offline

**Sekwencja:**
1. Użytkownik pisze wpis
2. Połączenie internetowe zostaje przerwane
3. Mechanizm auto-zapisu próbuje wysłać żądanie do API
4. Żądanie kończy się błędem sieciowym
5. Status zmienia się na "Offline - zapisano lokalnie"
6. Treść pozostaje w LocalStorage
7. Gdy połączenie zostanie przywrócone:
   - Przy następnej zmianie treści, auto-zapis ponownie się uruchomi
   - Jeśli się powiedzie, LocalStorage zostanie wyczyszczony

**Oczekiwany wynik:**
- Użytkownik nie traci danych mimo braku połączenia
- Po przywróceniu połączenia, dane są automatycznie synchronizowane

## 9. Warunki i walidacja

### Walidacja treści wpisu

**Warunek:** Treść nie może być pusta lub składać się tylko z białych znaków  
**Weryfikacja:** `!string.IsNullOrWhiteSpace(_content)`  
**Komponent:** `EntryEditor.razor`, metoda `AutoSaveAsync()`  
**Wpływ na UI:**
- Jeśli treść jest pusta, auto-zapis nie jest uruchamiany
- Nie wyświetla się błąd, po prostu nie ma próby zapisu
- Użytkownik może zostawić puste pole bez konsekwencji

---

### Walidacja ID wpisu (tryb edycji)

**Warunek:** ID musi być prawidłowym GUID i wpis musi istnieć w bazie  
**Weryfikacja:** `Id.HasValue && Id.Value != Guid.Empty`  
**Komponent:** `EntryEditor.razor`, metoda `OnInitializedAsync()`  
**Wpływ na UI:**
- Jeśli ID jest nieprawidłowe, użytkownik jest przekierowywany do `/app/entries`
- Jeśli wpis nie istnieje w bazie (404), wyświetlany jest komunikat błędu

---

### Walidacja autoryzacji

**Warunek:** Użytkownik musi być zalogowany  
**Weryfikacja:** Automatyczna przez Supabase - sprawdzenie ważności JWT  
**Komponent:** Wszystkie metody API w `EntryEditor.razor`  
**Wpływ na UI:**
- Jeśli token wygasł (401), użytkownik jest przekierowywany do `/login`
- Przed przekierowaniem, aktualny content jest zapisywany w LocalStorage

---

### Walidacja sieci (offline)

**Warunek:** Dostępność połączenia z internetem  
**Weryfikacja:** Przechwytywanie `HttpRequestException` w bloku try-catch  
**Komponent:** `EntryEditor.razor`, metoda `AutoSaveAsync()`  
**Wpływ na UI:**
- Status zmienia się na "Offline - zapisano lokalnie"
- Dane są przechowywane w LocalStorage
- Nie blokuje dalszej pracy użytkownika

## 10. Obsługa błędów

### 1. Błędy sieciowe (utrata połączenia)

**Scenariusz:** Użytkownik traci dostęp do internetu podczas edycji  
**Wykrywanie:** `catch (HttpRequestException ex)`  
**Obsługa:**
```csharp
catch (HttpRequestException ex)
{
    _logger.LogWarning(ex, "Network error during auto-save for entry {EntryId}", _entryId);
    _saveStatus = SaveStatus.Error;
    _errorMessage = "Brak połączenia - zmiany zapisano lokalnie";
    await SaveToLocalStorageAsync();
}
```
**Komunikat dla użytkownika:** "Offline - zapisano lokalnie"  
**Akcje:**
- Treść pozostaje w LocalStorage
- Użytkownik może kontynuować edycję
- Auto-zapis będzie próbował ponownie przy następnej zmianie

---

### 2. Błąd autoryzacji (401 Unauthorized)

**Scenariusz:** Token JWT użytkownika wygasł lub jest nieprawidłowy  
**Wykrywanie:** `catch (Postgrest.Exceptions.PostgrestException ex) when (ex.StatusCode == 401)`  
**Obsługa:**
```csharp
catch (PostgrestException ex) when (ex.StatusCode == 401)
{
    _logger.LogWarning("User unauthorized, redirecting to login");
    await SaveToLocalStorageAsync();
    _navigationManager.NavigateTo("/login");
}
```
**Komunikat dla użytkownika:** Przekierowanie do strony logowania  
**Akcje:**
- Zapisanie treści w LocalStorage przed przekierowaniem
- Po ponownym zalogowaniu, użytkownik może wznowić edycję

---

### 3. Wpis nie istnieje (404 Not Found)

**Scenariusz:** Użytkownik próbuje edytować wpis, który został usunięty lub nie należy do niego  
**Wykrywanie:** `catch (PostgrestException ex) when (ex.StatusCode == 404)`  
**Obsługa:**
```csharp
catch (PostgrestException ex) when (ex.StatusCode == 404)
{
    _logger.LogWarning("Entry {EntryId} not found", _entryId);
    _errorMessage = "Wpis nie został znaleziony lub został usunięty";
    await Task.Delay(2000);
    _navigationManager.NavigateTo("/app/entries");
}
```
**Komunikat dla użytkownika:** "Wpis nie został znaleziony"  
**Akcje:**
- Wyświetlenie komunikatu przez 2 sekundy
- Przekierowanie do listy wpisów

---

### 4. Nieprawidłowe dane (400 Bad Request)

**Scenariusz:** API odrzuca żądanie z powodu nieprawidłowych danych (teoretycznie nie powinno się zdarzyć dzięki walidacji po stronie klienta)  
**Wykrywanie:** `catch (PostgrestException ex) when (ex.StatusCode == 400)`  
**Obsługa:**
```csharp
catch (PostgrestException ex) when (ex.StatusCode == 400)
{
    _logger.LogError(ex, "Bad request when saving entry {EntryId}", _entryId);
    _saveStatus = SaveStatus.Error;
    _errorMessage = "Nieprawidłowe dane wpisu. Spróbuj ponownie";
}
```
**Komunikat dla użytkownika:** "Nieprawidłowe dane wpisu"  
**Akcje:**
- Dane pozostają w edytorze
- Użytkownik może poprawić i spróbować ponownie

---

### 5. Błędy serwera (5xx)

**Scenariusz:** Wewnętrzny błąd serwera Supabase  
**Wykrywanie:** `catch (PostgrestException ex) when (ex.StatusCode >= 500)`  
**Obsługa:**
```csharp
catch (PostgrestException ex) when (ex.StatusCode >= 500)
{
    _logger.LogError(ex, "Server error during save operation for entry {EntryId}", _entryId);
    _saveStatus = SaveStatus.Error;
    _errorMessage = "Wystąpił błąd serwera. Spróbuj ponownie później";
    await SaveToLocalStorageAsync();
}
```
**Komunikat dla użytkownika:** "Błąd serwera - spróbuj ponownie"  
**Akcje:**
- Dane zapisane w LocalStorage
- Użytkownik może spróbować ponownie później
- Możliwość manualnego ponowienia przez przycisk "Spróbuj ponownie"

---

### 6. Błąd usuwania z potwierdzeniem anulowania

**Scenariusz:** Użytkownik anuluje operację usuwania w oknie dialogowym  
**Wykrywanie:** Wynik `window.confirm()` jest `false`  
**Obsługa:**
```csharp
var confirmed = await _jsRuntime.InvokeAsync<bool>("confirm", "Czy na pewno chcesz usunąć ten wpis?");
if (!confirmed)
{
    _logger.LogInformation("User cancelled deletion of entry {EntryId}", _entryId);
    return; // Po prostu wyjdź z metody
}
```
**Komunikat dla użytkownika:** Brak (zachowanie oczekiwane)  
**Akcje:** Nic się nie dzieje, użytkownik kontynuuje edycję

## 11. Kroki implementacji

### Krok 1: Utworzenie struktury folderów i plików

```bash
# Utwórz folder dla funkcji EditEntry w ramach JournalEntries
mkdir -p 10xJournal.Client/Features/JournalEntries/EditEntry
```

**Pliki do utworzenia:**
- `SaveStatus.cs` - enum z możliwymi stanami zapisu
- `SaveStatusIndicator.razor` - komponent wskaźnika statusu
- `EntryEditor.razor` - główny komponent edytora
- `CreateJournalEntryRequest.cs` - model żądania tworzenia (jeśli nie istnieje)
- `UpdateJournalEntryRequest.cs` - model żądania aktualizacji (jeśli nie istnieje)

---

### Krok 2: Implementacja enum SaveStatus

**Plik:** `Features/JournalEntries/EditEntry/SaveStatus.cs`

```csharp
namespace _10xJournal.Client.Features.JournalEntries.EditEntry;

/// <summary>
/// Represents the current state of the auto-save operation.
/// </summary>
public enum SaveStatus
{
    Idle,
    Saving,
    Saved,
    Error
}
```

---

### Krok 3: Implementacja komponentu SaveStatusIndicator

**Plik:** `Features/JournalEntries/EditEntry/SaveStatusIndicator.razor`

```razor
@namespace _10xJournal.Client.Features.JournalEntries.EditEntry

<div class="save-status-indicator" aria-live="polite">
    @switch (Status)
    {
        case SaveStatus.Idle:
            <span class="status-idle"></span>
            break;
        case SaveStatus.Saving:
            <span class="status-saving">⏳ Zapisywanie...</span>
            break;
        case SaveStatus.Saved:
            <span class="status-saved">
                ✓ Zapisano @if (LastSavedAt.HasValue) { <text>o @LastSavedAt.Value.ToString("HH:mm")</text> }
            </span>
            break;
        case SaveStatus.Error:
            <span class="status-error">⚠ Błąd zapisu</span>
            break;
    }
</div>

@code {
    [Parameter]
    public SaveStatus Status { get; set; }

    [Parameter]
    public DateTime? LastSavedAt { get; set; }
}
```

---

### Krok 4: Implementacja głównego komponentu EntryEditor - część HTML

**Plik:** `Features/JournalEntries/EditEntry/EntryEditor.razor`

```razor
@page "/app/entry/new"
@page "/app/entry/{id:guid}"
@namespace _10xJournal.Client.Features.JournalEntries.EditEntry
@using _10xJournal.Client.Features.JournalEntries.Models
@inject NavigationManager NavigationManager
@inject Supabase.Client SupabaseClient
@inject ILogger<EntryEditor> Logger
@inject IJSRuntime JSRuntime

<div class="entry-editor">
    <header class="editor-header">
        <button type="button" @onclick="NavigateBack" class="btn-secondary" aria-label="Wróć do listy wpisów">
            ← Wróć
        </button>
        
        <SaveStatusIndicator Status="@_saveStatus" LastSavedAt="@_lastSavedAt" />
        
        @if (_entryId.HasValue)
        {
            <button type="button" @onclick="HandleDeleteAsync" class="btn-delete" aria-label="Usuń wpis">
                Usuń
            </button>
        }
    </header>
    
    @if (_isLoading)
    {
        <div class="loading-indicator" role="status">
            <p>Ładowanie wpisu...</p>
        </div>
    }
    else
    {
        <main class="editor-main">
            <textarea 
                @ref="_textareaRef"
                @oninput="HandleInput"
                value="@_content"
                placeholder="Zacznij pisać..."
                rows="20"
                aria-label="Treść wpisu"
                class="entry-textarea">
            </textarea>
        </main>
    }
    
    @if (!string.IsNullOrEmpty(_errorMessage))
    {
        <div class="error-message" role="alert">
            @_errorMessage
        </div>
    }
</div>

@code {
    // Kod komponentu w następnym kroku
}
```

---

### Krok 5: Implementacja logiki komponentu EntryEditor - blok @code

```csharp
@code {
    [Parameter]
    public Guid? Id { get; set; }

    private string _content = string.Empty;
    private Guid? _entryId = null;
    private SaveStatus _saveStatus = SaveStatus.Idle;
    private DateTime? _lastSavedAt = null;
    private bool _isLoading = false;
    private string? _errorMessage = null;
    private ElementReference _textareaRef;
    private Timer? _autoSaveTimer = null;

    private const int AUTOSAVE_DELAY_MS = 1000;
    private const string LOCALSTORAGE_KEY_PREFIX = "unsaved_entry_";

    protected override async Task OnInitializedAsync()
    {
        _entryId = Id;

        if (_entryId.HasValue)
        {
            await LoadEntryAsync();
        }
        else
        {
            await LoadFromLocalStorageAsync();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_entryId.HasValue)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("eval", $"document.querySelector('.entry-textarea').focus()");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to set focus on textarea");
            }
        }
    }

    private async Task LoadEntryAsync()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            var response = await SupabaseClient
                .From<JournalEntry>()
                .Where(x => x.Id == _entryId!.Value)
                .Single();

            _content = response.Content;
            
            // Sprawdź czy LocalStorage ma nowszą wersję
            var localContent = await LoadFromLocalStorageAsync();
            if (!string.IsNullOrEmpty(localContent))
            {
                _content = localContent;
                _errorMessage = "Przywrócono niezapisane zmiany";
            }
        }
        catch (PostgrestException ex) when (ex.StatusCode == 404)
        {
            Logger.LogWarning("Entry {EntryId} not found", _entryId);
            _errorMessage = "Wpis nie został znaleziony";
            await Task.Delay(2000);
            NavigationManager.NavigateTo("/app/entries");
        }
        catch (PostgrestException ex) when (ex.StatusCode == 401)
        {
            Logger.LogWarning("User unauthorized");
            await SaveToLocalStorageAsync();
            NavigationManager.NavigateTo("/login");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading entry {EntryId}", _entryId);
            _errorMessage = "Nie udało się załadować wpisu";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void HandleInput(ChangeEventArgs e)
    {
        _content = e.Value?.ToString() ?? string.Empty;
        
        // Zapisz do LocalStorage natychmiast
        _ = SaveToLocalStorageAsync();
        
        // Resetuj timer debouncing
        _autoSaveTimer?.Dispose();
        _autoSaveTimer = new Timer(async _ => await AutoSaveAsync(), null, AUTOSAVE_DELAY_MS, Timeout.Infinite);
    }

    private async Task AutoSaveAsync()
    {
        // Nie zapisuj jeśli treść jest pusta
        if (string.IsNullOrWhiteSpace(_content))
        {
            return;
        }

        _saveStatus = SaveStatus.Saving;
        await InvokeAsync(StateHasChanged);

        try
        {
            if (!_entryId.HasValue)
            {
                // Tryb tworzenia - POST
                var request = new CreateJournalEntryRequest { Content = _content };
                var response = await SupabaseClient
                    .From<JournalEntry>()
                    .Insert(request);

                _entryId = response.Models.First().Id;
                Logger.LogInformation("Created new entry with ID {EntryId}", _entryId);
            }
            else
            {
                // Tryb edycji - PATCH
                await SupabaseClient
                    .From<JournalEntry>()
                    .Where(x => x.Id == _entryId!.Value)
                    .Set(x => x.Content, _content)
                    .Update();

                Logger.LogInformation("Updated entry {EntryId}", _entryId);
            }

            _saveStatus = SaveStatus.Saved;
            _lastSavedAt = DateTime.Now;
            await ClearLocalStorageAsync();
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "Network error during auto-save");
            _saveStatus = SaveStatus.Error;
            _errorMessage = "Brak połączenia - zapisano lokalnie";
            await SaveToLocalStorageAsync();
        }
        catch (PostgrestException ex) when (ex.StatusCode == 401)
        {
            Logger.LogWarning("User unauthorized during save");
            await SaveToLocalStorageAsync();
            NavigationManager.NavigateTo("/login");
        }
        catch (PostgrestException ex) when (ex.StatusCode == 404)
        {
            Logger.LogWarning("Entry {EntryId} not found during update", _entryId);
            _saveStatus = SaveStatus.Error;
            _errorMessage = "Wpis nie istnieje";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during auto-save for entry {EntryId}", _entryId);
            _saveStatus = SaveStatus.Error;
            _errorMessage = "Błąd zapisu - spróbuj ponownie";
            await SaveToLocalStorageAsync();
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task HandleDeleteAsync()
    {
        if (!_entryId.HasValue)
        {
            return;
        }

        try
        {
            var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", "Czy na pewno chcesz usunąć ten wpis?");
            
            if (!confirmed)
            {
                Logger.LogInformation("User cancelled deletion of entry {EntryId}", _entryId);
                return;
            }

            await SupabaseClient
                .From<JournalEntry>()
                .Where(x => x.Id == _entryId!.Value)
                .Delete();

            Logger.LogInformation("Deleted entry {EntryId}", _entryId);
            await ClearLocalStorageAsync();
            NavigationManager.NavigateTo("/app/entries");
        }
        catch (PostgrestException ex) when (ex.StatusCode == 401)
        {
            Logger.LogWarning("User unauthorized during delete");
            NavigationManager.NavigateTo("/login");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting entry {EntryId}", _entryId);
            _errorMessage = "Nie udało się usunąć wpisu";
        }
    }

    private void NavigateBack()
    {
        NavigationManager.NavigateTo("/app/entries");
    }

    private async Task SaveToLocalStorageAsync()
    {
        try
        {
            var key = $"{LOCALSTORAGE_KEY_PREFIX}{_entryId?.ToString() ?? "new"}";
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", key, _content);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to save to localStorage");
        }
    }

    private async Task<string?> LoadFromLocalStorageAsync()
    {
        try
        {
            var key = $"{LOCALSTORAGE_KEY_PREFIX}{_entryId?.ToString() ?? "new"}";
            return await JSRuntime.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load from localStorage");
            return null;
        }
    }

    private async Task ClearLocalStorageAsync()
    {
        try
        {
            var key = $"{LOCALSTORAGE_KEY_PREFIX}{_entryId?.ToString() ?? "new"}";
            await JSRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to clear localStorage");
        }
    }

    public void Dispose()
    {
        _autoSaveTimer?.Dispose();
    }
}
```

---

### Krok 6: Dodanie styli CSS (opcjonalnie)

**Plik:** `Features/JournalEntries/EditEntry/EntryEditor.razor.css` (isolated CSS)

```css
.entry-editor {
    max-width: 800px;
    margin: 0 auto;
    padding: 1rem;
}

.editor-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
    flex-wrap: wrap;
    gap: 0.5rem;
}

.entry-textarea {
    width: 100%;
    min-height: 400px;
    font-family: inherit;
    font-size: 1rem;
    line-height: 1.6;
    resize: vertical;
}

.save-status-indicator {
    flex-grow: 1;
    text-align: center;
    font-size: 0.875rem;
}

.status-saving {
    color: var(--color-secondary, #666);
}

.status-saved {
    color: var(--color-success, #28a745);
}

.status-error {
    color: var(--color-danger, #dc3545);
}

.loading-indicator {
    text-align: center;
    padding: 2rem;
}

.error-message {
    background-color: #f8d7da;
    color: #721c24;
    padding: 0.75rem;
    border-radius: 4px;
    margin-top: 1rem;
}

.btn-delete {
    background-color: var(--color-danger, #dc3545);
    color: white;
}

@media (max-width: 768px) {
    .editor-header {
        flex-direction: column;
        align-items: stretch;
    }
    
    .save-status-indicator {
        order: -1;
        text-align: center;
    }
}
```

---

### Krok 7: Utworzenie modeli żądań (jeśli nie istnieją)

**Plik:** `Features/JournalEntries/EditEntry/CreateJournalEntryRequest.cs`

```csharp
namespace _10xJournal.Client.Features.JournalEntries.EditEntry;

public class CreateJournalEntryRequest
{
    public string Content { get; set; } = string.Empty;
}
```

**Plik:** `Features/JournalEntries/EditEntry/UpdateJournalEntryRequest.cs`

```csharp
namespace _10xJournal.Client.Features.JournalEntries.EditEntry;

public class UpdateJournalEntryRequest
{
    public string Content { get; set; } = string.Empty;
}
```

**Uwaga:** Te modele mogą już istnieć w innej lokalizacji zgodnie z planami implementacji API. Upewnij się, że używasz istniejących modeli lub dostosuj namespace w komponencie.

---

### Krok 8: Testowanie funkcjonalności

**Testy manualne do przeprowadzenia:**

1. **Tworzenie nowego wpisu:**
   - Przejdź do `/app/entry/new`
   - Sprawdź czy textarea ma auto-focus
   - Wpisz treść i poczekaj 1 sekundę
   - Sprawdź czy pojawia się "Zapisywanie...", a potem "Zapisano"
   - Odśwież stronę i sprawdź czy wpis istnieje

2. **Edycja istniejącego wpisu:**
   - Kliknij na wpis z listy
   - Sprawdź czy treść się załadowała
   - Zmodyfikuj treść
   - Sprawdź auto-zapis
   - Odśwież i sprawdź czy zmiany zostały zapisane

3. **Usuwanie wpisu:**
   - Wejdź w edycję wpisu
   - Kliknij "Usuń"
   - Sprawdź czy pojawia się dialog
   - Potwierdź i sprawdź przekierowanie do listy
   - Sprawdź czy wpis zniknął z listy

4. **Praca offline:**
   - Wyłącz połączenie internetowe (DevTools → Network → Offline)
   - Edytuj wpis
   - Sprawdź czy pojawia się komunikat "Offline"
   - Włącz połączenie
   - Zmodyfikuj wpis i sprawdź czy zapisuje się poprawnie

5. **LocalStorage recovery:**
   - Zacznij pisać wpis
   - Zamknij kartę przed zapisem (w czasie < 1s)
   - Otwórz ponownie ten sam URL
   - Sprawdź czy treść została przywrócona

6. **Responsywność:**
   - Przetestuj widok na różnych rozmiarach ekranu
   - Sprawdź czy przyciski i textarea są dobrze widoczne na mobile

---

### Krok 9: Integracja z nawigacją aplikacji

**Upewnij się, że:**
- Lista wpisów (`ListEntries.razor`) ma linki/przyciski nawigujące do `/app/entry/{id}`
- Istnieje przycisk "Nowy wpis" kierujący do `/app/entry/new`
- Nawigacja "Wróć" w edytorze prowadzi do `/app/entries`

---

### Krok 10: Dokumentacja i code review

- Dodaj komentarze XML do wszystkich publicznych metod
- Sprawdź zgodność z coding guidelines (async/await, naming, logging)
- Upewnij się, że wszystkie błędy są odpowiednio logowane
- Przeprowadź review kodu z fokusem na bezpieczeństwo (XSS, autoryzacja)

---

## Podsumowanie

Ten plan implementacji zapewnia kompletny, krok po kroku przewodnik do stworzenia widoku Edytora Wpisu zgodnie z wymaganiami PRD, user stories i architekturą Vertical Slice. Komponent jest zaprojektowany jako samodzielny, z pełną obsługą auto-zapisu, pracy offline i właściwą obsługą błędów, przy zachowaniu prostoty i minimalizmu interfejsu zgodnie z filozofią 10xJournal.
