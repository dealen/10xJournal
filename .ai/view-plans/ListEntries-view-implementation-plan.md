# Plan implementacji widoku Lista Wpisów

## 1. Przegląd
Widok **Lista Wpisów** jest głównym widokiem aplikacji po zalogowaniu użytkownika. Jego celem jest wyświetlenie wszystkich wpisów dziennika użytkownika w porządku odwrotnie chronologicznym (od najnowszego do najstarszego) oraz umożliwienie nawigacji do tworzenia nowego wpisu lub edycji istniejącego. Widok wspiera budowanie nawyku pisania poprzez wyświetlanie wskaźnika "streak" (seria kolejnych dni z wpisami) oraz zapewnia pozytywne pierwsze wrażenie dla nowych użytkowników poprzez automatyczny wpis powitalny.

Widok został zaprojektowany zgodnie z filozofią minimalizmu - koncentruje się na treści, eliminuje rozpraszacze i zapewnia intuicyjną obsługę zarówno na urządzeniach mobilnych, jak i desktopowych.

## 2. Routing widoku
- **Ścieżka**: `/app/journal`
- **Typ**: Strona chroniona, wymaga uwierzytelnienia
- **Domyślny widok**: Tak, użytkownik po zalogowaniu jest przekierowywany na tę ścieżkę
- **Autoryzacja**: Zastosowanie atrybutu `@attribute [Authorize]` lub ręczna weryfikacja stanu uwierzytelnienia w `OnInitializedAsync`

**Powiązane ścieżki:**
- `/app/journal/new` - tworzenie nowego wpisu (cel nawigacji z przycisku "Nowy wpis")
- `/app/journal/{id:guid}` - edycja istniejącego wpisu (cel nawigacji po kliknięciu elementu listy)

## 3. Struktura komponentów

Widok składa się z następującej hierarchii komponentów:

```
ListEntries.razor (komponent strony, ścieżka: /app/journal)
├── StreakIndicator (warunkowy, wyświetlany gdy current_streak > 0)
│   └── Ikona 🔥 + liczba dni
│
├── Header
│   ├── Tytuł strony (h1)
│   └── Przycisk "Nowy wpis"
│
└── Main Content (warunkowe renderowanie w zależności od stanu)
    ├── SkeletonLoader (gdy _isLoading === true)
    ├── ErrorMessage (gdy _hasError === true)
    ├── EmptyState (gdy _entries.Count === 0 && !_isLoading && !_hasError)
    └── EntriesList (gdy _entries.Count > 0 && !_isLoading)
        └── EntryListItem (dla każdego wpisu w liście, z użyciem @key)
            ├── Data utworzenia (<time>)
            └── Tytuł (pierwsza linia, <h3>)
```

## 4. Szczegóły komponentów

### 4.1. ListEntries.razor
**Lokalizacja**: `Features/JournalEntries/ListEntries/ListEntries.razor`

#### Opis komponentu
Główny komponent strony odpowiedzialny za zarządzanie stanem widoku, pobieranie danych z API Supabase oraz koordynację renderowania komponentów potomnych w zależności od aktualnego stanu (ładowanie, błąd, pusta lista, lista z danymi).

#### Główne elementy HTML i komponenty dzieci
```razor
@page "/app/journal"
@attribute [Authorize]

<main class="container">
    @if (_currentStreak > 0)
    {
        <div class="streak-indicator">
            🔥 <span>@_currentStreak dni z rzędu</span>
        </div>
    }
    
    <header>
        <h1>Mój Dziennik</h1>
        <button @onclick="NavigateToNewEntry" class="new-entry-button">
            Nowy wpis
        </button>
    </header>
    
    @if (_isLoading)
    {
        <SkeletonLoader />
    }
    else if (_hasError)
    {
        <div class="error-message" role="alert">
            <p>@_errorMessage</p>
            <button @onclick="RetryLoadAsync">Spróbuj ponownie</button>
        </div>
    }
    else if (_entries.Count == 0)
    {
        <EmptyState />
    }
    else
    {
        <div class="entries-list">
            @foreach (var entry in _entries)
            {
                <EntryListItem @key="entry.Id" Entry="entry" />
            }
        </div>
    }
</main>
```

#### Obsługiwane zdarzenia
- **NavigateToNewEntry**: Obsługa kliknięcia przycisku "Nowy wpis", nawigacja do `/app/journal/new`
- **RetryLoadAsync**: Obsługa kliknięcia przycisku "Spróbuj ponownie" w przypadku błędu, ponowne wywołanie metody pobierającej dane

#### Warunki walidacji
- **Stan uwierzytelnienia**: Weryfikacja, czy użytkownik jest zalogowany (token JWT obecny w sesji Supabase). Jeśli nie, przekierowanie do `/login`.
- **Warunki renderowania**:
  - Streak indicator: `_currentStreak > 0`
  - Skeleton loader: `_isLoading == true && !_hasError`
  - Error message: `_hasError == true`
  - Empty state: `!_isLoading && _entries.Count == 0 && !_hasError`
  - Entries list: `!_isLoading && _entries.Count > 0 && !_hasError`

#### Typy
- **JournalEntry** (DTO) - model wpisu dziennika
- **UserStreak** (DTO) - model danych o serii wpisów użytkownika

#### Propsy
Komponent nie przyjmuje żadnych parametrów od rodzica (jest komponentem strony).

---

### 4.2. EntryListItem.razor
**Lokalizacja**: `Features/JournalEntries/ListEntries/EntryListItem.razor`

#### Opis komponentu
Komponent odpowiedzialny za wyświetlenie pojedynczego wpisu na liście. Prezentuje datę utworzenia wpisu oraz jego "tytuł" - pierwszą linię treści, skróconą do maksymalnie 100 znaków. Cały element jest klikalnym linkiem prowadzącym do edytora wpisu.

#### Główne elementy HTML
```razor
<article class="entry-item">
    <a href="@($"/app/journal/{Entry.Id}")">
        <time datetime="@Entry.CreatedAt.ToString("yyyy-MM-dd")">
            @Entry.CreatedAt.ToString("d MMMM yyyy")
        </time>
        <h3>@GetDisplayTitle()</h3>
    </a>
</article>
```

#### Obsługiwane zdarzenia
- **Kliknięcie elementu**: Nawigacja realizowana natywnie przez element `<a href>`, prowadzi do `/app/journal/{Entry.Id}`

#### Warunki walidacji
- **Treść wpisu**: Jeśli `Entry.Content` jest null lub pusty, wyświetl domyślny tekst "Pusty wpis"
- **Długość tytułu**: Jeśli pierwsza linia przekracza 100 znaków, obetnij i dodaj wielokropek "..."

#### Typy
- **JournalEntry** (Parameter) - obiekt wpisu przekazywany z komponentu rodzica

#### Propsy
```csharp
[Parameter, EditorRequired]
public JournalEntry Entry { get; set; } = default!;
```

---

### 4.3. SkeletonLoader.razor
**Lokalizacja**: `Shared/Components/SkeletonLoader.razor` (komponent współdzielony)

#### Opis komponentu
Prosty komponent wyświetlający animowany placeholder podczas ładowania danych. Zapewnia wizualną informację zwrotną użytkownikowi, że aplikacja przetwarza jego żądanie. Renderuje 3-5 elementów szkieletowych imitujących wygląd rzeczywistych elementów listy.

#### Główne elementy HTML
```razor
<div class="skeleton-loader" aria-busy="true" aria-live="polite">
    <div class="skeleton-item" aria-label="Ładowanie wpisu 1">
        <div class="skeleton-date"></div>
        <div class="skeleton-title"></div>
    </div>
    <div class="skeleton-item" aria-label="Ładowanie wpisu 2">
        <div class="skeleton-date"></div>
        <div class="skeleton-title"></div>
    </div>
    <div class="skeleton-item" aria-label="Ładowanie wpisu 3">
        <div class="skeleton-date"></div>
        <div class="skeleton-title"></div>
    </div>
</div>
```

#### Obsługiwane zdarzenia
Brak (komponent tylko do wyświetlania).

#### Warunki walidacji
Brak (komponent zawsze renderuje tę samą strukturę).

#### Typy
Brak typów wejściowych.

#### Propsy
Brak parametrów (komponent bezstanowy).

---

### 4.4. EmptyState.razor
**Lokalizacja**: `Shared/Components/EmptyState.razor` (komponent współdzielony)

#### Opis komponentu
Komponent wyświetlany, gdy użytkownik nie ma jeszcze żadnych wpisów. Zawiera zachęcający komunikat oraz przycisk CTA prowadzący do tworzenia pierwszego wpisu. Zapewnia pozytywne doświadczenie użytkownika w stanie pustym i jasno komunikuje następny krok.

#### Główne elementy HTML
```razor
<div class="empty-state">
    <div class="empty-state-icon">📝</div>
    <h2>Nie masz jeszcze żadnych wpisów</h2>
    <p>Zacznij swoją przygodę z dziennikarstwem już dziś!</p>
    <button @onclick="NavigateToNewEntry" class="primary">
        Dodaj pierwszy wpis
    </button>
</div>
```

#### Obsługiwane zdarzenia
- **NavigateToNewEntry**: Nawigacja do `/app/journal/new` (przekazane jako callback lub obsłużone wewnętrznie przez NavigationManager)

#### Warunki walidacji
Brak (komponent zawsze wyświetla ten sam komunikat).

#### Typy
Brak typów wejściowych.

#### Propsy
```csharp
[Parameter]
public EventCallback OnCreateClick { get; set; }
```
Lub alternatywnie, komponent może wewnętrznie wstrzykiwać `NavigationManager` i obsługiwać nawigację samodzielnie.

---

## 5. Typy

### 5.1. JournalEntry (istniejący)
**Lokalizacja**: `Features/JournalEntries/Models/JournalEntry.cs`

Model DTO reprezentujący wpis dziennika, mapowany bezpośrednio do tabeli `journal_entries` w bazie danych Supabase.

```csharp
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace _10xJournal.Client.Features.JournalEntries.Models;

/// <summary>
/// Reprezentuje wpis dziennika z bazy danych.
/// Mapowany do tabeli 'journal_entries' w Supabase.
/// </summary>
[Table("journal_entries")]
public class JournalEntry : BaseModel
{
    /// <summary>
    /// Unikalny identyfikator wpisu.
    /// </summary>
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    /// <summary>
    /// Identyfikator użytkownika, który utworzył wpis.
    /// </summary>
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Treść/zawartość wpisu dziennika.
    /// </summary>
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Data i czas utworzenia wpisu.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Data i czas ostatniej aktualizacji wpisu.
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
```

---

### 5.2. UserStreak (nowy)
**Lokalizacja**: `Features/JournalEntries/Models/UserStreak.cs`

Model DTO reprezentujący dane o serii wpisów użytkownika (ile dni z rzędu użytkownik pisał w dzienniku). Mapowany do tabeli `user_streaks`.

```csharp
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace _10xJournal.Client.Features.JournalEntries.Models;

/// <summary>
/// Reprezentuje dane o serii wpisów użytkownika.
/// Mapowany do tabeli 'user_streaks' w Supabase.
/// </summary>
[Table("user_streaks")]
public class UserStreak : BaseModel
{
    /// <summary>
    /// Identyfikator użytkownika (klucz główny).
    /// </summary>
    [PrimaryKey("user_id", false)]
    public Guid UserId { get; set; }

    /// <summary>
    /// Aktualna seria - liczba kolejnych dni z wpisami.
    /// </summary>
    [Column("current_streak")]
    public int CurrentStreak { get; set; }

    /// <summary>
    /// Najdłuższa seria w historii użytkownika.
    /// </summary>
    [Column("longest_streak")]
    public int LongestStreak { get; set; }

    /// <summary>
    /// Data ostatniego wpisu uwzględnionego w serii.
    /// </summary>
    [Column("last_entry_date")]
    public DateTime LastEntryDate { get; set; }
}
```

**Uwaga**: Dane o serii są automatycznie aktualizowane przez trigger bazodanowy `update_user_streak_trigger` po każdym dodaniu nowego wpisu. Frontend jedynie odczytuje te dane.

---

## 6. Zarządzanie stanem

Widok **Lista Wpisów** wykorzystuje **zarządzanie stanem na poziomie komponentu** (component-level state). Nie jest wymagane wprowadzanie globalnego zarządzania stanem ani customowych hooków. Cały stan jest przechowywany w prywatnych polach komponentu `ListEntries.razor` i aktualizowany w odpowiedzi na zdarzenia cyklu życia komponentu oraz interakcje użytkownika.

### Zmienne stanu w komponencie ListEntries.razor:

```csharp
@code {
    [Inject] private Supabase.Client SupabaseClient { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    
    private List<JournalEntry> _entries = new();
    private bool _isLoading = true;
    private bool _hasError = false;
    private string? _errorMessage = null;
    private int _currentStreak = 0;
}
```

### Opis zmiennych stanu:

1. **`_entries`** (`List<JournalEntry>`)
   - **Cel**: Przechowuje listę wszystkich wpisów dziennika użytkownika pobranych z API.
   - **Inicjalizacja**: Pusta lista.
   - **Aktualizacja**: Wypełniana po udanym wywołaniu API w metodzie `LoadEntriesAsync()`.
   - **Użycie**: Iterowana w pętli `@foreach` do renderowania komponentów `EntryListItem`.

2. **`_isLoading`** (`bool`)
   - **Cel**: Wskazuje, czy trwa ładowanie danych z API.
   - **Inicjalizacja**: `true` (zakładamy, że dane są ładowane od razu po inicjalizacji).
   - **Aktualizacja**: Ustawiana na `false` po zakończeniu wywołania API (niezależnie od sukcesu czy błędu).
   - **Użycie**: Kontroluje wyświetlanie komponentu `SkeletonLoader`.

3. **`_hasError`** (`bool`)
   - **Cel**: Wskazuje, czy wystąpił błąd podczas pobierania danych.
   - **Inicjalizacja**: `false`.
   - **Aktualizacja**: Ustawiana na `true` w bloku `catch` metody `LoadEntriesAsync()`.
   - **Użycie**: Kontroluje wyświetlanie komunikatu o błędzie.

4. **`_errorMessage`** (`string?`)
   - **Cel**: Przechowuje przyjazny dla użytkownika komunikat o błędzie.
   - **Inicjalizacja**: `null`.
   - **Aktualizacja**: Ustawiana w bloku `catch` na podstawie typu wyjątku (np. "Nie można połączyć się z serwerem").
   - **Użycie**: Wyświetlana w interfejsie użytkownika, gdy `_hasError == true`.

5. **`_currentStreak`** (`int`)
   - **Cel**: Przechowuje aktualną serię wpisów użytkownika (liczba kolejnych dni z wpisami).
   - **Inicjalizacja**: `0`.
   - **Aktualizacja**: Wypełniana po udanym wywołaniu API w metodzie `LoadStreakAsync()`.
   - **Użycie**: Wyświetlana jako wskaźnik "streak" (🔥 + liczba), tylko jeśli wartość > 0.

### Przepływ aktualizacji stanu:

1. **Inicjalizacja komponentu** (`OnInitializedAsync`):
   - `_isLoading` = `true`
   - Wywołanie `LoadEntriesAsync()` i `LoadStreakAsync()`
   - Po zakończeniu: `_isLoading` = `false`

2. **Sukces pobierania danych**:
   - `_entries` = dane z API
   - `_currentStreak` = dane z API
   - `_hasError` = `false`

3. **Błąd pobierania danych**:
   - `_hasError` = `true`
   - `_errorMessage` = komunikat dla użytkownika
   - `_entries` pozostaje pusta lista

4. **Ponowne próbowanie** (kliknięcie "Spróbuj ponownie"):
   - Reset stanu błędu
   - Ponowne wywołanie `LoadEntriesAsync()`

---

## 7. Integracja API

Widok komunikuje się bezpośrednio z Supabase poprzez klienta `Supabase.Client`. Nie stosuje się dodatkowych warstw abstrakcji (np. repozytoria czy serwisy) zgodnie z architekturą Vertical Slice i zasadą KISS.

### 7.1. Pobieranie listy wpisów

**Endpoint**: Auto-generowane API PostgREST Supabase  
**Ścieżka**: `/rest/v1/journal_entries`  
**Metoda HTTP**: `GET`  
**Parametry query string**:
- `select=*` - pobiera wszystkie kolumny
- `order=created_at.desc` - sortuje według daty utworzenia, od najnowszego

**Kod implementacji**:
```csharp
private async Task LoadEntriesAsync()
{
    try
    {
        _isLoading = true;
        _hasError = false;
        
        var response = await SupabaseClient
            .From<JournalEntry>()
            .Select("*")
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        
        _entries = response.Models;
    }
    catch (Supabase.Exceptions.SupabaseException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
    {
        // Token wygasł lub jest nieprawidłowy
        _hasError = true;
        _errorMessage = "Sesja wygasła. Proszę zalogować się ponownie.";
        // TODO: Wywołać LogoutHandler i przekierować do /login
        Navigation.NavigateTo("/login", forceLoad: true);
    }
    catch (HttpRequestException ex)
    {
        // Brak połączenia z siecią
        _hasError = true;
        _errorMessage = "Nie można połączyć się z serwerem. Sprawdź połączenie internetowe.";
        Console.Error.WriteLine($"Network error: {ex.Message}");
    }
    catch (Exception ex)
    {
        // Ogólny błąd
        _hasError = true;
        _errorMessage = "Wystąpił nieoczekiwany błąd. Spróbuj ponownie za chwilę.";
        Console.Error.WriteLine($"Error loading entries: {ex}");
    }
    finally
    {
        _isLoading = false;
    }
}
```

**Typy żądania**:
- Brak body (GET request)
- Automatycznie dołączany nagłówek `Authorization: Bearer <JWT>` przez klienta Supabase

**Typy odpowiedzi**:
- **Sukces (200 OK)**: `ModeledResponse<JournalEntry>` z właściwością `Models` zawierającą `List<JournalEntry>`
- **Błąd uwierzytelnienia (401)**: `SupabaseException` z `StatusCode = Unauthorized`
- **Błędy serwera (5xx)**: `SupabaseException` z odpowiednim kodem statusu
- **Błędy sieciowe**: `HttpRequestException`

---

### 7.2. Pobieranie danych o serii (streak)

**Endpoint**: Auto-generowane API PostgREST Supabase  
**Ścieżka**: `/rest/v1/user_streaks`  
**Metoda HTTP**: `GET`  
**Parametry query string**:
- `select=*` - pobiera wszystkie kolumny
- Filtracja po `user_id` odbywa się automatycznie przez RLS

**Kod implementacji**:
```csharp
private async Task LoadStreakAsync()
{
    try
    {
        var response = await SupabaseClient
            .From<UserStreak>()
            .Select("*")
            .Single();
        
        _currentStreak = response?.CurrentStreak ?? 0;
    }
    catch (Exception ex)
    {
        // Błąd pobierania streak nie jest krytyczny, logujemy i kontynuujemy
        Console.Error.WriteLine($"Error loading streak: {ex.Message}");
        _currentStreak = 0;
    }
}
```

**Typy żądania**:
- Brak body (GET request)
- Automatycznie dołączany nagłówek `Authorization: Bearer <JWT>`

**Typy odpowiedzi**:
- **Sukces (200 OK)**: `UserStreak` object
- **Brak danych**: `null` (jeśli użytkownik nie ma jeszcze rekordu w tabeli streak)
- **Błędy**: Logowane, ale nie blokują wyświetlenia listy wpisów

---

### 7.3. Bezpieczeństwo i autoryzacja

- **Uwierzytelnianie**: Każde żądanie jest automatycznie uwierzytelniane przez klienta Supabase za pomocą tokena JWT przechowywanego w sesji użytkownika.
- **Autoryzacja**: Realizowana w całości przez polityki Row Level Security (RLS) w bazie danych PostgreSQL. Polityka `Users can CRUD their own journal entries` gwarantuje, że użytkownik ma dostęp tylko do swoich wpisów.
- **Filtrowanie danych**: Odbywa się automatycznie w bazie danych na podstawie `auth.uid()` z tokena JWT. Frontend nie musi implementować dodatkowej logiki filtrowania.

---

## 8. Interakcje użytkownika

### 8.1. Załadowanie strony (user stories #6, #11, #12, #19)

**Akcja**: Użytkownik nawiguje do `/app/journal` (po zalogowaniu lub bezpośrednio).

**Przepływ**:
1. Blazor inicjalizuje komponent `ListEntries`
2. Wywołanie `OnInitializedAsync`:
   - Ustawienie `_isLoading = true`
   - Wyświetlenie komponentu `SkeletonLoader`
3. Równoległe wywołanie `LoadEntriesAsync()` i `LoadStreakAsync()`
4. Po otrzymaniu odpowiedzi z API:
   - Ustawienie `_isLoading = false`
   - Ukrycie `SkeletonLoader`
   - Jeśli dane pobrane pomyślnie: renderowanie listy wpisów
   - Jeśli błąd: wyświetlenie komunikatu o błędzie
   - Jeśli brak wpisów: wyświetlenie komponentu `EmptyState`
5. Jeśli `_currentStreak > 0`: wyświetlenie wskaźnika streak (🔥 + liczba)

**Oczekiwany wynik**:
- Lista wpisów wyświetlona w porządku od najnowszego do najstarszego
- Nowy użytkownik widzi wpis powitalny (utworzony automatycznie przez trigger bazodanowy)
- Interfejs jest czytelny i responsywny na urządzeniach mobilnych

---

### 8.2. Kliknięcie przycisku "Nowy wpis" (user story #13)

**Akcja**: Użytkownik klika przycisk "Nowy wpis" w nagłówku strony.

**Przepływ**:
1. Wywołanie metody `NavigateToNewEntry()`
2. `NavigationManager.NavigateTo("/app/journal/new")`
3. Blazor renderuje widok tworzenia nowego wpisu

**Oczekiwany wynik**:
- Użytkownik zostaje przeniesiony do czystego edytora, gotowego do pisania nowego wpisu

---

### 8.3. Kliknięcie elementu listy (user story #15)

**Akcja**: Użytkownik klika na dowolny wpis na liście.

**Przepływ**:
1. Element `<a href="/app/journal/{entry.Id}">` obsługuje kliknięcie natywnie
2. Blazor Router przechwytuje nawigację i renderuje widok edycji wpisu z odpowiednim ID
3. Widok edycji pobiera dane wpisu i wyświetla je w edytorze

**Oczekiwany wynik**:
- Użytkownik widzi wybrany wpis w trybie edycji, może go odczytać lub zmodyfikować

---

### 8.4. Powrót do listy po dodaniu wpisu (user story #14)

**Akcja**: Użytkownik zapisuje nowy wpis w edytorze i wraca do listy (np. za pomocą przycisku "Wróć" lub nawigacji przeglądarki).

**Przepływ**:
1. Widok edycji zapisuje nowy wpis w bazie danych
2. Użytkownik klika przycisk nawigacyjny powracający do `/app/journal`
3. Komponent `ListEntries` jest ponownie inicjowany
4. Wywołanie `OnInitializedAsync` ponownie pobiera dane z API
5. Nowy wpis pojawia się na górze listy (dzięki sortowaniu `created_at.desc`)

**Oczekiwany wynik**:
- Nowy wpis jest widoczny na pierwszej pozycji listy
- Użytkownik ma potwierdzenie, że wpis został dodany

**Uwaga**: Dla lepszej wydajności, w przyszłości można rozważyć optymalizację poprzez przekazywanie informacji o nowo utworzonym wpisie lub użycie globalnego stanu, aby uniknąć ponownego pobierania wszystkich danych.

---

### 8.5. Stan pusty - brak wpisów

**Akcja**: Użytkownik wchodzi na stronę listy, ale nie ma jeszcze żadnych wpisów.

**Przepływ**:
1. Po załadowaniu danych `_entries.Count == 0`
2. Renderowanie komponentu `EmptyState`
3. Wyświetlenie zachęcającego komunikatu i przycisku "Dodaj pierwszy wpis"

**Oczekiwany wynik**:
- Użytkownik widzi przyjazny komunikat informujący o braku wpisów
- Jasne wskazanie następnego kroku (dodanie pierwszego wpisu)
- Kliknięcie przycisku przenosi do widoku tworzenia wpisu

---

### 8.6. Obsługa błędu

**Akcja**: Podczas ładowania danych wystąpił błąd (brak sieci, problem z serwerem, wygasła sesja).

**Przepływ**:
1. Wyjątek przechwycony w bloku `catch` metody `LoadEntriesAsync()`
2. Ustawienie `_hasError = true` i `_errorMessage` z przyjaznym komunikatem
3. Renderowanie komunikatu o błędzie i przycisku "Spróbuj ponownie"

**Oczekiwany wynik**:
- Użytkownik widzi zrozumiały komunikat o błędzie (nie techniczny stack trace)
- Możliwość ponowienia próby bez przeładowania strony
- W przypadku błędu 401: automatyczne wylogowanie i przekierowanie do strony logowania

---

### 8.7. Interakcja z wskaźnikiem streak

**Akcja**: Użytkownik widzi wskaźnik 🔥 z liczbą dni.

**Przepływ**:
- Wskaźnik jest wyświetlany wyłącznie gdy `_currentStreak > 0`
- Jest to pasywny element informacyjny, nie wymaga interakcji
- W przyszłości może być rozszerzony o tooltip z dodatkowymi informacjami (najdłuższa seria, motywacyjny komunikat)

**Oczekiwany wynik**:
- Użytkownik otrzymuje pozytywne wzmocnienie nawykowe
- Motywacja do kontynuowania regularnego pisania

---

## 9. Warunki i walidacja

### 9.1. Walidacja stanu uwierzytelnienia

**Komponent**: `ListEntries.razor`

**Warunek**: Użytkownik musi być uwierzytelniony, aby uzyskać dostęp do widoku.

**Implementacja**:
```csharp
@attribute [Authorize]
```
lub alternatywnie, ręczna weryfikacja w `OnInitializedAsync`:
```csharp
protected override async Task OnInitializedAsync()
{
    var session = await SupabaseClient.Auth.GetSession();
    if (session?.User == null)
    {
        Navigation.NavigateTo("/login");
        return;
    }
    
    await LoadEntriesAsync();
    await LoadStreakAsync();
}
```

**Wpływ na UI**: Jeśli użytkownik nie jest zalogowany, zostaje przekierowany do strony logowania i nie widzi treści widoku.

---

### 9.2. Walidacja stanu ładowania

**Komponent**: `ListEntries.razor`

**Warunek**: Podczas pobierania danych z API, wyświetl wskaźnik ładowania.

**Implementacja**:
```razor
@if (_isLoading)
{
    <SkeletonLoader />
}
```

**Wpływ na UI**: Użytkownik widzi animowany placeholder zamiast pustej strony, co poprawia postrzeganie wydajności aplikacji.

---

### 9.3. Walidacja stanu błędu

**Komponent**: `ListEntries.razor`

**Warunek**: Jeśli wystąpił błąd podczas pobierania danych, wyświetl przyjazny komunikat.

**Implementacja**:
```razor
else if (_hasError)
{
    <div class="error-message" role="alert">
        <p>@_errorMessage</p>
        <button @onclick="RetryLoadAsync">Spróbuj ponownie</button>
    </div>
}
```

**Wpływ na UI**: Użytkownik widzi zrozumiały komunikat o błędzie zamiast surowego wyjątku. Może podjąć akcję (ponowić próbę) bez konieczności przeładowania strony.

---

### 9.4. Walidacja stanu pustego

**Komponent**: `ListEntries.razor`

**Warunek**: Jeśli użytkownik nie ma jeszcze żadnych wpisów, wyświetl zachęcający komunikat.

**Implementacja**:
```razor
else if (_entries.Count == 0)
{
    <EmptyState />
}
```

**Wpływ na UI**: Zapobiega wyświetleniu pustej, zdezorientowanej strony. Użytkownik otrzymuje jasne wskazówki, co zrobić dalej.

---

### 9.5. Walidacja wyświetlania wskaźnika streak

**Komponent**: `ListEntries.razor`

**Warunek**: Wskaźnik streak jest wyświetlany tylko wtedy, gdy seria jest większa od zera.

**Implementacja**:
```razor
@if (_currentStreak > 0)
{
    <div class="streak-indicator">
        🔥 <span>@_currentStreak dni z rzędu</span>
    </div>
}
```

**Wpływ na UI**: Unika wyświetlania "0 dni z rzędu", co mogłoby być demotywujące dla nowych użytkowników.

---

### 9.6. Walidacja tytułu wpisu (ekstrakcja pierwszej linii)

**Komponent**: `EntryListItem.razor`

**Warunek**: Jeśli treść wpisu jest pusta lub null, wyświetl domyślny tekst. Jeśli pierwsza linia przekracza 100 znaków, obetnij i dodaj wielokropek.

**Implementacja**:
```csharp
private string GetDisplayTitle()
{
    if (string.IsNullOrWhiteSpace(Entry.Content))
        return "Pusty wpis";
    
    // Ekstrakcja pierwszej linii (obsługa różnych formatów nowej linii)
    var firstLine = Entry.Content
        .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)[0]
        .Trim();
    
    if (string.IsNullOrWhiteSpace(firstLine))
        return "Pusty wpis";
    
    // Obcięcie do 100 znaków
    return firstLine.Length > 100 
        ? firstLine.Substring(0, 100) + "..." 
        : firstLine;
}
```

**Wpływ na UI**: 
- Graceful handling pustych wpisów
- Konsystentna prezentacja tytułów
- Unikanie zbyt długich tytułów, które mogłyby zepsuć layout

---

### 9.7. Walidacja formatowania daty

**Komponent**: `EntryListItem.razor`

**Warunek**: Data utworzenia wpisu powinna być wyświetlona w czytelnym, lokalnym formacie.

**Implementacja**:
```razor
<time datetime="@Entry.CreatedAt.ToString("yyyy-MM-dd")">
    @Entry.CreatedAt.ToString("d MMMM yyyy")
</time>
```

**Przykład wyniku**: "14 października 2025"

**Wpływ na UI**: Data jest prezentowana w sposób naturalny dla użytkownika polskojęzycznego. Atrybut `datetime` zapewnia dostępność dla technologii asystujących i semantykę dla wyszukiwarek.

---

## 10. Obsługa błędów

### 10.1. Błąd 401 Unauthorized - wygasła sesja

**Przyczyna**: Token JWT użytkownika jest nieprawidłowy lub wygasł.

**Obsługa**:
```csharp
catch (Supabase.Exceptions.SupabaseException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
{
    _hasError = true;
    _errorMessage = "Sesja wygasła. Proszę zalogować się ponownie.";
    
    // Wylogowanie użytkownika i przekierowanie
    await SupabaseClient.Auth.SignOut();
    Navigation.NavigateTo("/login", forceLoad: true);
}
```

**Komunikat dla użytkownika**: "Sesja wygasła. Proszę zalogować się ponownie."

**Akcja**: Automatyczne wylogowanie i przekierowanie do strony logowania.

---

### 10.2. Błąd sieciowy (brak połączenia)

**Przyczyna**: Użytkownik jest offline lub serwer jest nieosiągalny.

**Obsługa**:
```csharp
catch (HttpRequestException ex)
{
    _hasError = true;
    _errorMessage = "Nie można połączyć się z serwerem. Sprawdź połączenie internetowe.";
    Console.Error.WriteLine($"Network error: {ex.Message}");
}
```

**Komunikat dla użytkownika**: "Nie można połączyć się z serwerem. Sprawdź połączenie internetowe."

**Akcja**: Wyświetlenie przycisku "Spróbuj ponownie" umożliwiającego ponowne wywołanie `LoadEntriesAsync()`.

---

### 10.3. Błąd serwera (5xx)

**Przyczyna**: Wewnętrzny błąd Supabase lub bazy danych PostgreSQL.

**Obsługa**:
```csharp
catch (Supabase.Exceptions.SupabaseException ex) when (ex.StatusCode >= System.Net.HttpStatusCode.InternalServerError)
{
    _hasError = true;
    _errorMessage = "Wystąpił błąd serwera. Spróbuj ponownie za chwilę.";
    Console.Error.WriteLine($"Server error: {ex.StatusCode} - {ex.Message}");
}
```

**Komunikat dla użytkownika**: "Wystąpił błąd serwera. Spróbuj ponownie za chwilę."

**Akcja**: Logowanie szczegółów błędu do konsoli (dla developera). Wyświetlenie przycisku "Spróbuj ponownie".

---

### 10.4. Nieoczekiwany błąd

**Przyczyna**: Nieobsłużony wyjątek (np. błąd parsowania, nieoczekiwana struktura danych).

**Obsługa**:
```csharp
catch (Exception ex)
{
    _hasError = true;
    _errorMessage = "Wystąpił nieoczekiwany błąd. Spróbuj ponownie lub skontaktuj się z pomocą techniczną.";
    Console.Error.WriteLine($"Unexpected error: {ex}");
}
```

**Komunikat dla użytkownika**: "Wystąpił nieoczekiwany błąd. Spróbuj ponownie lub skontaktuj się z pomocą techniczną."

**Akcja**: Pełne logowanie wyjątku do konsoli. W przyszłości: wysłanie do centralnego systemu logowania (Sentry, Seq, Application Insights).

---

### 10.5. Pusta odpowiedź (brak wpisów)

**Przyczyna**: Użytkownik nie utworzył jeszcze żadnych wpisów lub wszystkie wpisy zostały usunięte.

**Obsługa**: To nie jest błąd, ale poprawny stan aplikacji.

```razor
else if (_entries.Count == 0)
{
    <EmptyState />
}
```

**Komunikat dla użytkownika**: "Nie masz jeszcze żadnych wpisów. Dodaj swój pierwszy wpis!"

**Akcja**: Wyświetlenie komponentu `EmptyState` z zachęcającym komunikatem i przyciskiem CTA.

---

### 10.6. Błąd pobierania danych streak (niekrytyczny)

**Przyczyna**: Błąd podczas pobierania danych z tabeli `user_streaks` (np. użytkownik nie ma jeszcze rekordu).

**Obsługa**:
```csharp
private async Task LoadStreakAsync()
{
    try
    {
        var response = await SupabaseClient
            .From<UserStreak>()
            .Select("*")
            .Single();
        
        _currentStreak = response?.CurrentStreak ?? 0;
    }
    catch (Exception ex)
    {
        // Nie blokujemy wyświetlenia listy wpisów
        Console.Error.WriteLine($"Error loading streak: {ex.Message}");
        _currentStreak = 0;
    }
}
```

**Komunikat dla użytkownika**: Brak (błąd jest logowany, ale nie wyświetlany).

**Akcja**: Wskaźnik streak nie jest wyświetlany (`_currentStreak = 0`), ale reszta widoku działa normalnie.

---

### 10.7. Strategia retry (ponowne próby)

**Implementacja przycisku "Spróbuj ponownie"**:
```csharp
private async Task RetryLoadAsync()
{
    await LoadEntriesAsync();
    await LoadStreakAsync();
}
```

**Użycie w UI**:
```razor
<button @onclick="RetryLoadAsync">Spróbuj ponownie</button>
```

Umożliwia użytkownikowi ponowne wywołanie metod pobierających dane bez konieczności przeładowania całej strony.

---

## 11. Kroki implementacji

Poniżej przedstawiono szczegółowy, krok po kroku plan implementacji widoku **Lista Wpisów**. Zaleca się realizację zgodnie z kolejnością, aby zapewnić logiczny porządek i możliwość testowania na każdym etapie.

---

### Krok 1: Utworzenie modelu UserStreak
**Cel**: Dodanie nowego modelu DTO dla danych o serii wpisów użytkownika.

**Akcje**:
1. Utwórz plik `Features/JournalEntries/Models/UserStreak.cs`
2. Zaimplementuj klasę według specyfikacji z sekcji 5.2 (użyj atrybutów Postgrest, dziedzicz po `BaseModel`)
3. Dodaj XML komentarze dokumentujące każde pole
4. Zbuduj projekt i upewnij się, że nie ma błędów kompilacji

**Weryfikacja**: Pomyślna kompilacja projektu.

---

### Krok 2: Utworzenie komponentu SkeletonLoader
**Cel**: Implementacja komponentu wyświetlającego stan ładowania.

**Akcje**:
1. Utwórz plik `Shared/Components/SkeletonLoader.razor`
2. Zaimplementuj strukturę HTML według specyfikacji z sekcji 4.3
3. Użyj atrybutu `aria-busy="true"` dla dostępności
4. Dodaj style CSS w osobnym pliku `SkeletonLoader.razor.css` lub w globalnym `app.css`:
   - Animacja placeholder (np. pulse lub shimmer effect)
   - Odpowiednie wymiary imitujące prawdziwe elementy listy
5. Przetestuj komponent wizualnie, renderując go tymczasowo w innym widoku

**Weryfikacja**: Komponent renderuje się poprawnie z animacją ładowania.

---

### Krok 3: Utworzenie komponentu EmptyState
**Cel**: Implementacja komponentu wyświetlanego w stanie pustym (brak wpisów).

**Akcje**:
1. Utwórz plik `Shared/Components/EmptyState.razor`
2. Zaimplementuj strukturę HTML według specyfikacji z sekcji 4.4
3. Wstrzyknij `NavigationManager` lub dodaj parametr `EventCallback OnCreateClick`
4. Zaimplementuj metodę obsługi kliknięcia przycisku:
   ```csharp
   private void NavigateToNewEntry()
   {
       Navigation.NavigateTo("/app/journal/new");
   }
   ```
5. Dodaj style CSS dla wyśrodkowania i estetycznego wyglądu
6. Przetestuj komponent wizualnie

**Weryfikacja**: Komponent renderuje się poprawnie, przycisk nawiguje do właściwej ścieżki.

---

### Krok 4: Utworzenie komponentu EntryListItem
**Cel**: Implementacja komponentu wyświetlającego pojedynczy wpis na liście.

**Akcje**:
1. Utwórz plik `Features/JournalEntries/ListEntries/EntryListItem.razor`
2. Zaimplementuj strukturę HTML według specyfikacji z sekcji 4.2
3. Dodaj parametr `[Parameter, EditorRequired] public JournalEntry Entry { get; set; }`
4. Zaimplementuj metodę `GetDisplayTitle()` według specyfikacji z sekcji 9.6:
   - Obsługa pustej treści
   - Ekstrakcja pierwszej linii
   - Obcięcie do 100 znaków z wielokropkiem
5. Zastosuj semantyczne elementy HTML (`<article>`, `<time>`, `<h3>`)
6. Dodaj style CSS w `EntryListItem.razor.css` lub globalnie:
   - Hover effect na elemencie
   - Responsywny layout
   - Czytelna typografia
7. Przetestuj komponent, przekazując testowy obiekt `JournalEntry`

**Weryfikacja**: Komponent poprawnie wyświetla datę i tytuł, link działa poprawnie.

---

### Krok 5: Utworzenie strony ListEntries
**Cel**: Implementacja głównego komponentu strony z logiką pobierania danych.

**Akcje**:
1. Utwórz plik `Features/JournalEntries/ListEntries/ListEntries.razor`
2. Dodaj dyrektywę routingu: `@page "/app/journal"`
3. Dodaj atrybut autoryzacji: `@attribute [Authorize]`
4. Wstrzyknij zależności:
   ```csharp
   @inject Supabase.Client SupabaseClient
   @inject NavigationManager Navigation
   ```
5. Zdefiniuj zmienne stanu w bloku `@code` (sekcja 6)
6. Zaimplementuj strukturę HTML według specyfikacji z sekcji 4.1:
   - Warunkowe renderowanie dla wszystkich stanów (loading, error, empty, success)
   - Pętla `@foreach` z dyrektywą `@key="entry.Id"`
7. Użyj wcześniej utworzonych komponentów (`SkeletonLoader`, `EmptyState`, `EntryListItem`)

**Weryfikacja**: Podstawowa struktura strony jest gotowa (jeszcze bez logiki API).

---

### Krok 6: Implementacja logiki pobierania wpisów
**Cel**: Dodanie metody `LoadEntriesAsync()` z pełną obsługą błędów.

**Akcje**:
1. W bloku `@code` komponentu `ListEntries.razor`, zaimplementuj metodę `LoadEntriesAsync()` według specyfikacji z sekcji 7.1
2. Dodaj obsługę wszystkich typów błędów (401, network errors, server errors, ogólne wyjątki)
3. Zaimplementuj metodę pomocniczą `GetUserFriendlyErrorMessage(Exception ex)` (opcjonalnie, jeśli chcesz ujednolicić mapowanie wyjątków na komunikaty)
4. Wywołaj `LoadEntriesAsync()` w metodzie `OnInitializedAsync()`
5. Dodaj metodę `RetryLoadAsync()` dla przycisku "Spróbuj ponownie"
6. Dodaj logowanie błędów do konsoli (`Console.Error.WriteLine`)

**Weryfikacja**: 
- Uruchom aplikację i zaloguj się
- Sprawdź, czy lista wpisów jest pobierana i wyświetlana poprawnie
- Przetestuj scenariusze błędów (rozłącz internet, symuluj 401 itp.)

---

### Krok 7: Implementacja logiki pobierania danych streak
**Cel**: Dodanie metody `LoadStreakAsync()` i wyświetlanie wskaźnika 🔥.

**Akcje**:
1. Zaimplementuj metodę `LoadStreakAsync()` według specyfikacji z sekcji 7.2
2. Wywołaj `LoadStreakAsync()` w `OnInitializedAsync()` (może być równolegle z `LoadEntriesAsync()`)
3. Dodaj warunkowe renderowanie wskaźnika streak w HTML:
   ```razor
   @if (_currentStreak > 0)
   {
       <div class="streak-indicator">
           🔥 <span>@_currentStreak dni z rzędu</span>
       </div>
   }
   ```
4. Dodaj style CSS dla wskaźnika (pozycjonowanie, kolorystyka)

**Weryfikacja**: 
- Wskaźnik wyświetla się poprawnie, gdy użytkownik ma serię > 0
- Wskaźnik nie wyświetla się, gdy seria = 0 lub nie udało się pobrać danych

---

### Krok 8: Implementacja nawigacji do tworzenia nowego wpisu
**Cel**: Obsługa kliknięcia przycisku "Nowy wpis".

**Akcje**:
1. W bloku `@code` dodaj metodę:
   ```csharp
   private void NavigateToNewEntry()
   {
       Navigation.NavigateTo("/app/journal/new");
   }
   ```
2. Upewnij się, że przycisk ma atrybut `@onclick="NavigateToNewEntry"`

**Weryfikacja**: Kliknięcie przycisku przenosi użytkownika do widoku tworzenia wpisu (nawet jeśli ten widok jeszcze nie istnieje, powinien wystąpić błąd routingu, co potwierdza działanie nawigacji).

---

### Krok 9: Stylizacja widoku z użyciem Pico.css
**Cel**: Zastosowanie frameworka Pico.css dla spójnego i responsywnego wyglądu.

**Akcje**:
1. Upewnij się, że Pico.css jest włączony w `wwwroot/index.html` lub w głównym layoucie
2. Zastosuj klasy Pico.css do elementów:
   - `class="container"` dla głównego kontenera
   - Użyj semantycznych elementów HTML (Pico stylizuje je automatycznie)
   - Dostosuj kolory i odstępy według designu aplikacji
3. Dodaj dodatkowe style w `app.css` lub `ListEntries.razor.css` dla:
   - Wskaźnika streak
   - Elementów listy wpisów (hover effects, spacing)
   - Stanu pustego i błędów
   - Skeleton loader animacji
4. Przetestuj responsywność na różnych rozmiarach ekranu (desktop, tablet, mobile)

**Weryfikacja**: Widok wygląda estetycznie i jest w pełni responsywny.

---

### Krok 10: Testy funkcjonalne i scenariuszy użytkownika
**Cel**: Weryfikacja, że wszystkie user stories są zrealizowane.

**Akcje**:
1. **US#6**: Zaloguj się i sprawdź, czy lista wpisów jest dostępna
2. **US#11**: Zaloguj się jako nowy użytkownik i sprawdź, czy widoczny jest wpis powitalny
3. **US#12**: Sprawdź, czy wpisy są wyświetlane w kolejności od najnowszego do najstarszego
4. **US#13**: Kliknij "Nowy wpis" i sprawdź nawigację
5. **US#14**: Utwórz nowy wpis, wróć do listy i sprawdź, czy pojawił się na górze
6. **US#15**: Kliknij dowolny wpis i sprawdź, czy otwiera się w edytorze
7. **US#19**: Przetestuj widok na urządzeniu mobilnym (emulator lub prawdziwe urządzenie)
8. Przetestuj wszystkie scenariusze błędów (rozłącz sieć, wygasła sesja)
9. Przetestuj stan pusty (usuń wszystkie wpisy, zaloguj się na nowe konto bez triggera powitalnego)

**Weryfikacja**: Wszystkie scenariusze działają zgodnie z oczekiwaniami.

---

### Krok 11: Optymalizacja wydajności
**Cel**: Upewnienie się, że widok działa płynnie i wydajnie.

**Akcje**:
1. Sprawdź, czy w pętli `@foreach` użyta jest dyrektywa `@key="entry.Id"` (optymalizacja renderowania Blazor)
2. Zmierz czas pobierania danych z API (użyj narzędzi deweloperskich przeglądarki)
3. Upewnij się, że metody asynchroniczne używają `await` i nie blokują wątku UI
4. Przejrzyj kod pod kątem niepotrzebnych re-renderów (opcjonalnie użyj `ShouldRender()` dla bardziej zaawansowanych optymalizacji)
5. Jeśli liczba wpisów jest bardzo duża (>100), rozważ dodanie paginacji lub lazy loading (dla przyszłych wersji)

**Weryfikacja**: Widok ładuje się szybko (<2s), nawigacja jest płynna, brak lagów na urządzeniach mobilnych.

---

### Krok 12: Dokumentacja i czystość kodu
**Cel**: Zapewnienie, że kod jest czytelny i dobrze udokumentowany.

**Akcje**:
1. Dodaj XML komentarze (`///`) do wszystkich publicznych metod i właściwości
2. Upewnij się, że nazwy zmiennych są jasne i opisowe
3. Podziel długie metody na mniejsze funkcje pomocnicze (jeśli to konieczne)
4. Dodaj komentarze wyjaśniające "dlaczego" (nie "co") w nieoczywistych miejscach
5. Sprawdź, czy kod spełnia konwencje nazewnictwa C# (PascalCase, camelCase, _camelCase)
6. Uruchom narzędzie do analizy kodu (jeśli dostępne) i popraw ostrzeżenia

**Weryfikacja**: Kod jest łatwy do zrozumienia dla innego dewelopera, brak ostrzeżeń kompilatora.

### Krok 13: Code review i finalne poprawki
**Cel**: Ostateczna weryfikacja jakości kodu i gotowości do wdrożenia.

**Akcje**:
1. Przeprowadź self-review kodu (przejrzyj każdy plik, jakbyś był innym deweloperem)
2. Sprawdź zgodność z instrukcjami architektury (Vertical Slice)
3. Sprawdź zgodność z zasadami kodowania (KISS, czytelność, async/await)
4. Uruchom aplikację raz jeszcze i przejdź przez wszystkie scenariusze użytkownika
5. Popraw wszelkie znalezione problemy
6. Stwórz pull request (jeśli pracujesz w zespole) lub zmerguj do głównej gałęzi

**Weryfikacja**: Kod jest gotowy do wdrożenia produkcyjnego.

---

## Podsumowanie

Powyższy plan implementacji zapewnia kompleksowy, krok po kroku proces tworzenia widoku **Lista Wpisów** dla aplikacji 10xJournal. Realizując implementację zgodnie z tym planem, deweloper frontend będzie w stanie:

- Stworzyć w pełni funkcjonalny widok spełniający wszystkie wymagania z PRD i user stories
- Zapewnić poprawną integrację z API Supabase i Row Level Security
- Zaimplementować responsywny, dostępny i estetyczny interfejs użytkownika
- Obsłużyć wszystkie scenariusze błędów i przypadki brzegowe
- Dostarczyć kod wysokiej jakości, zgodny z zasadami architektury i kodowania projektu

Plan jest gotowy do przekazania deweloperowi do implementacji.
