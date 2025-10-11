
# API Endpoint Implementation Plan: `export_journal_entries`

## 1. Przegląd punktu końcowego
Celem tego punktu końcowego jest umożliwienie uwierzytelnionemu użytkownikowi wyeksportowania wszystkich jego wpisów z dziennika. Punkt końcowy jest realizowany jako wywołanie zdalnej procedury (RPC) do niestandardowej funkcji PostgreSQL `export_journal_entries` w Supabase. Funkcja ta agreguje dane i zwraca je w ustrukturyzowanym formacie JSON, gotowym do przetworzenia i pobrania przez klienta.

## 2. Szczegóły żądania
- **Metoda HTTP**: `POST`
- **Struktura URL**: `/rpc/export_journal_entries`
- **Parametry**:
  - **Wymagane**: Brak parametrów w URL lub ciele żądania.
  - **Opcjonalne**: Brak.
- **Request Body**: Brak (N/A).
- **Nagłówki**:
  - `Authorization: Bearer <jwt_token>` (Wymagane) - Token JWT uwierzytelniający użytkownika.
  - `Apikey: <supabase_anon_key>` (Wymagane) - Klucz publiczny Supabase.
  - `Content-Type: application/json`

## 3. Wykorzystywane typy
Zgodnie z zasadami Vertical Slice Architecture, następujące modele DTO zostaną utworzone w folderze funkcji `Features/Settings/ExportData/Models/`:

1.  **`ExportDataResponse.cs`**: Główny kontener na dane zwrócone przez RPC.
    ```csharp
    using System.Text.Json.Serialization;

    namespace _10xJournal.Client.Features.Settings.ExportData.Models;

    public class ExportDataResponse
    {
        [JsonPropertyName("export_date")]
        public DateTimeOffset ExportDate { get; set; }

        [JsonPropertyName("entry_count")]
        public int EntryCount { get; set; }

        [JsonPropertyName("entries")]
        public List<ExportedEntry> Entries { get; set; } = new();
    }
    ```

2.  **`ExportedEntry.cs`**: Reprezentuje pojedynczy wpis w eksportowanych danych.
    ```csharp
    using System.Text.Json.Serialization;

    namespace _10xJournal.Client.Features.Settings.ExportData.Models;

    public class ExportedEntry
    {
        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
    ```

## 4. Szczegóły odpowiedzi
- **Odpowiedź sukcesu (200 OK)**:
  ```json
  {
      "export_date": "2025-10-12T10:00:00Z",
      "entry_count": 2,
      "entries": [
          {
              "created_at": "2025-10-11T18:00:00Z",
              "content": "This is my latest journal entry."
          },
          {
              "created_at": "2025-10-10T10:00:00Z",
              "content": "This was my first entry, the welcome message."
          }
      ]
  }
  ```
- **Odpowiedzi błędów**:
  - `401 Unauthorized`: Użytkownik nie jest uwierzytelniony lub token JWT jest nieprawidłowy.
  - `500 Internal Server Error`: Wystąpił błąd podczas wykonywania funkcji po stronie bazy danych.

## 5. Przepływ danych
1.  Użytkownik w komponencie Blazor (`ExportData.razor`) klika przycisk "Eksportuj dane".
2.  Metoda w `@code` bloku komponentu jest wywoływana.
3.  Metoda używa wstrzykniętego klienta Supabase (`Supabase.Client`), aby wywołać funkcję RPC: `await _supabaseClient.Rpc("export_journal_entries", null);`.
4.  Klient Supabase automatycznie dołącza nagłówki `Authorization` i `Apikey`.
5.  PostgREST odbiera żądanie i wywołuje funkcję `export_journal_entries()` w PostgreSQL.
6.  Funkcja SQL pobiera wszystkie wpisy (`journal_entries`) dla `user_id` równego `auth.uid()`, sortuje je malejąco po dacie utworzenia, formatuje wynik do oczekiwanej struktury JSON i go zwraca.
7.  Odpowiedź JSON jest przesyłana z powrotem do klienta Blazor.
8.  Kod klienta deserializuje odpowiedź do modelu `ExportDataResponse`.
9.  Na podstawie otrzymanych danych generowany jest plik (np. `.json` lub `.txt`) po stronie klienta przy użyciu interoperacyjności z JavaScript, a następnie udostępniany do pobrania.

## 6. Względy bezpieczeństwa
- **Autoryzacja**: Dostęp jest ściśle kontrolowany przez RLS i logikę wewnątrz funkcji PostgreSQL. Funkcja `export_journal_entries` musi używać `auth.uid()` do filtrowania danych, aby zapewnić, że użytkownicy mogą uzyskać dostęp wyłącznie do własnych wpisów.
- **Definicja funkcji**: Funkcja w PostgreSQL powinna być zdefiniowana z opcją `SECURITY DEFINER`, aby mogła działać z podwyższonymi uprawnieniami (jeśli to konieczne), ale logika wewnątrz musi być bezpieczna i oparta na `auth.uid()`.
- **Walidacja po stronie klienta**: Chociaż nie ma danych wejściowych, kod klienta musi być przygotowany na obsługę błędów `401` i odpowiednio zareagować (np. wylogowując użytkownika).

## 7. Rozważania dotyczące wydajności
- **Indeksowanie**: Tabela `journal_entries` musi mieć złożony indeks na `(user_id, created_at DESC)`, aby zapytanie wewnątrz funkcji RPC było wysoce wydajne. Zgodnie z dokumentacją bazy danych, taki indeks już istnieje.
- **Rozmiar danych**: W przypadku użytkowników z bardzo dużą liczbą wpisów, odpowiedź może być duża, co może wpłynąć na czas odpowiedzi i zużycie pamięci po stronie klienta. Na etapie MVP jest to akceptowalne ryzyko. W przyszłości można rozważyć paginację lub streaming.

## 8. Etapy wdrożenia
1.  **Baza danych (SQL)**:
    -   Utworzyć plik migracji Supabase dla nowej funkcji `supabase/migrations/YYYYMMDDHHMMSS_create_export_function.sql`.
    -   Zaimplementować funkcję `export_journal_entries()` w PostgreSQL. Funkcja powinna:
        -   Przyjmować brak argumentów.
        -   Zwracać `json`.
        -   Być zdefiniowana z `SECURITY DEFINER`.
        -   Pobierać wpisy z `journal_entries` gdzie `user_id = auth.uid()`.
        -   Używać `json_build_object` i `json_agg` do skonstruowania odpowiedzi zgodnej ze specyfikacją.
    -   Uruchomić migrację w lokalnym środowisku Supabase (`supabase db reset`).

2.  **Frontend (Blazor)**:
    -   Utworzyć folder `10xJournal.Client/Features/Settings/ExportData/`.
    -   W folderze `Models/` wewnątrz `ExportData/` zdefiniować klasy `ExportDataResponse.cs` i `ExportedEntry.cs`.
    -   Stworzyć komponent `ExportData.razor` w folderze `ExportData/`.
    -   W komponencie `ExportData.razor`:
        -   Dodać przycisk "Eksportuj moje dane".
        -   Wstrzyknąć `Supabase.Client` i `ILogger`.
        -   Zaimplementować metodę `HandleExportAsync`, która będzie wywoływana po kliknięciu przycisku.
        -   Wewnątrz `HandleExportAsync`, umieścić wywołanie RPC w bloku `try-catch`: `var response = await _supabaseClient.Rpc<ExportDataResponse>("export_journal_entries", null);`.
        -   Obsłużyć błędy, logując je i wyświetlając komunikat dla użytkownika.
        -   Po pomyślnym pobraniu danych, zaimplementować logikę generowania i pobierania pliku po stronie klienta (prawdopodobnie z pomocą JS interop do stworzenia `Blob` i linku do pobrania).
    -   Dodać nawigację do nowego komponentu w `Settings.razor`.