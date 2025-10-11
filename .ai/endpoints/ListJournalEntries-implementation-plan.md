# API Endpoint Implementation Plan: List All Journal Entries

## 1. Przegląd punktu końcowego
Celem tego punktu końcowego jest umożliwienie uwierzytelnionemu użytkownikowi pobrania listy wszystkich jego wpisów w dzienniku. Wyniki będą posortowane od najnowszego do najstarszego, zgodnie ze specyfikacją. Punkt końcowy jest realizowany przez auto-generowane API PostgREST dostarczane przez Supabase.

## 2. Szczegóły żądania
- **Metoda HTTP**: `GET`
- **Struktura URL**: `/rest/v1/journal_entries`
- **Parametry Query String**:
  - **Wymagane**:
    - `select=*`: Zapewnia, że w odpowiedzi zostaną zwrócone wszystkie kolumny z tabeli `journal_entries`.
    - `order=created_at.desc`: Sortuje zwrócone wpisy według daty utworzenia w porządku malejącym.
- **Request Body**: Brak (N/A)

## 3. Wykorzystywane typy
- **`10xJournal.Client/Features/JournalEntries/Models/JournalEntry.cs`**: Model DTO (Data Transfer Object) reprezentujący pojedynczy wpis dziennika. Będzie używany do deserializacji odpowiedzi JSON z API Supabase.

```csharp
// 10xJournal.Client/Features/JournalEntries/Models/JournalEntry.cs
using System.Text.Json.Serialization;

namespace _10xJournal.Client.Features.JournalEntries.Models;

public class JournalEntry
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
```

## 4. Przepływ danych
1.  Komponent Blazor `ListEntries.razor` jest inicjowany.
2.  W metodzie cyklu życia `OnInitializedAsync`, komponent wywołuje klienta Supabase.
3.  Klient Supabase konstruuje żądanie `GET` do ścieżki `/rest/v1/journal_entries` z parametrami `select=*` i `order=created_at.desc`.
4.  Klient automatycznie dołącza nagłówek `Authorization: Bearer <JWT>` z tokenem zalogowanego użytkownika.
5.  Supabase (PostgREST) odbiera żądanie i weryfikuje token JWT.
6.  Polityka Row Level Security (RLS) na tabeli `journal_entries` jest stosowana, filtrując wiersze, aby zwrócić tylko te, gdzie `user_id` pasuje do `auth.uid()` z tokena.
7.  Baza danych wykonuje zapytanie `SELECT * FROM journal_entries WHERE user_id = <user_id> ORDER BY created_at DESC`.
8.  Supabase serializuje wyniki do formatu JSON i wysyła odpowiedź `200 OK` do klienta.
9.  Aplikacja Blazor deserializuje odpowiedź JSON do listy obiektów `JournalEntry` i aktualizuje stan komponentu, co powoduje wyrenderowanie listy wpisów na ekranie.

## 5. Względy bezpieczeństwa
- **Uwierzytelnianie**: Każde żądanie musi być uwierzytelnione za pomocą tokena JWT. Klient Supabase w Blazor jest odpowiedzialny za zarządzanie tokenem i dołączanie go do żądań.
- **Autoryzacja**: Autoryzacja jest w całości delegowana do bazy danych PostgreSQL i jej mechanizmu Row Level Security (RLS). Polityka `Users can CRUD their own journal entries` gwarantuje, że użytkownik ma dostęp wyłącznie do swoich danych. Nie jest wymagana żadna dodatkowa logika autoryzacji po stronie klienta.

## 6. Obsługa błędów
- **`401 Unauthorized`**: Jeśli Supabase zwróci ten status, oznacza to problem z sesją użytkownika. Aplikacja powinna przechwycić ten błąd, wylogować użytkownika i przekierować go do strony logowania.
- **Błędy sieciowe/serwera (HTTP 5xx)**: W przypadku problemów z połączeniem lub wewnętrznych błędów serwera, aplikacja powinna wyświetlić użytkownikowi ogólny, przyjazny komunikat o błędzie (np. "Wystąpił błąd podczas ładowania danych. Proszę spróbować ponownie.") i zalogować szczegóły wyjątku do konsoli deweloperskiej.
- **Brak wpisów**: Jeśli odpowiedź `200 OK` zawiera pustą tablicę, interfejs użytkownika powinien wyświetlić odpowiedni komunikat, np. "Nie masz jeszcze żadnych wpisów. Dodaj swój pierwszy wpis!".

## 7. Rozważania dotyczące wydajności
- **Indeksowanie**: Kluczowym czynnikiem wydajności jest istnienie indeksu na kolumnach `(user_id, created_at DESC)`. Zgodnie z dokumentacją bazy danych (`db.md`), taki indeks (`idx_journal_entries_user_id_created_at`) już istnieje, co zapewnia szybkie wyszukiwanie i sortowanie.
- **Paginacja**: W przyszłości, jeśli użytkownik będzie miał tysiące wpisów, należy zaimplementować paginację (np. za pomocą `limit` i `offset` w query string) lub nieskończone przewijanie (infinite scrolling), aby uniknąć ładowania ogromnej ilości danych naraz. Na etapie MVP nie jest to wymagane.
- **Rozmiar danych**: Zapytanie `select=*` pobiera wszystkie kolumny. Jeśli w przyszłości tabela zostanie rozszerzona o duże pola, które nie są potrzebne na liście, należy jawnie określić wymagane kolumny (np. `select=id,content,created_at`), aby zminimalizować transfer danych.

## 8. Etapy wdrożenia
1.  **Utworzenie modelu**: Upewnij się, że plik `10xJournal.Client/Features/JournalEntries/Models/JournalEntry.cs` istnieje i ma poprawną strukturę zgodną z sekcją 3.
2.  **Implementacja logiki w komponencie**: W pliku `10xJournal.Client/Features/JournalEntries/ListEntries/ListEntries.razor`:
    a. Wstrzyknij klienta Supabase (`Supabase.Client`) przez konstruktor lub atrybut `[Inject]`.
    b. W metodzie `OnInitializedAsync`, zaimplementuj blok `try-catch`.
    c. W bloku `try`, wywołaj metodę klienta Supabase, aby pobrać dane: `await supabase.From<JournalEntry>().Select("*").Order("created_at", Postgrest.Constants.Ordering.Descending).Get()`.
    d. Przypisz zwrócone dane do prywatnego pola komponentu (np. `private List<JournalEntry> entries;`).
    e. W bloku `catch`, zaloguj błąd i ustaw flagę informującą o błędzie, aby UI mogło wyświetlić stosowny komunikat.
3.  **Implementacja interfejsu użytkownika**: W części HTML pliku `ListEntries.razor`:
    a. Dodaj logikę warunkową do wyświetlania stanu ładowania (gdy dane są pobierane).
    b. Dodaj logikę do wyświetlania komunikatu o błędzie, jeśli wystąpił.
    c. Jeśli dane załadowały się pomyślnie, użyj pętli `@foreach`, aby wyrenderować listę wpisów.
    d. Zastosuj dyrektywę `@key` na elemencie w pętli, używając `entry.Id` dla optymalizacji renderowania przez Blazor.
    e. Obsłuż przypadek, gdy lista wpisów jest pusta i wyświetl odpowiedni komunikat.