Jesteś asystentem AI, którego zadaniem jest pomoc w zaplanowaniu architektury interfejsu użytkownika dla MVP (Minimum Viable Product) na podstawie dostarczonych informacji. Twoim celem jest wygenerowanie listy pytań i zaleceń, które zostaną wykorzystane w kolejnym promptowaniu do utworzenia szczegółowej architektury UI, map podróży użytkownika i struktury nawigacji.

Prosimy o uważne zapoznanie się z poniższymi informacjami:

<product_requirements>
## **Dokument Wymagań Produktu (PRD): 10xJournal MVP**

Wersja: 1.1

Data: 8 października 2025

### **1. Wprowadzenie i Cel**

**10xJournal** to minimalistyczna aplikacja webowa, która pełni rolę osobistego, cyfrowego dziennika. Jej głównym celem jest dostarczenie użytkownikowi prostej, bezpiecznej i wolnej od rozpraszaczy przestrzeni do regularnego pisania. Aplikacja została zaprojektowana z myślą o wspieraniu budowania nawyku prowadzenia dziennika.

**Grupa docelowa:** Użytkownicy (w tym osoby nietechniczne) przytłoczeni złożonością i wysokim progiem wejścia istniejących narzędzi do notatek (np. Notion, Obsidian), którzy szukają dedykowanego i intuicyjnego rozwiązania.

---

### **2. Problem i Rozwiązanie**

- **Problem:** Wiele popularnych aplikacji do notatek oferuje nadmiar funkcji (skomplikowane formatowanie, bazy danych, szablony), co tworzy barierę wejścia i odciąga uwagę od podstawowej czynności – pisania. To zniechęca do regularności i systematyczności.
    
- **Rozwiązanie:** 10xJournal eliminuje wszystkie zbędne funkcje, koncentrując się na absolutnych podstawach. Oferuje czysty, prosty interfejs, który stawia treść na pierwszym miejscu, a jego obsługa jest intuicyjna od pierwszego uruchomienia. Prywatność i bezpieczeństwo danych są kluczowymi filarami projektu.
    

---

### **3. Zakres i Funkcjonalności MVP**

#### **3.1. Zarządzanie Kontem Użytkownika**

- **Rejestracja:** Użytkownik może założyć konto, podając adres e-mail i hasło.
    
- **Logowanie:** Użytkownik może zalogować się na swoje konto.
    
- **Reset Hasła:** Dostępna jest podstawowa funkcja przypominania/resetowania hasła.
    
- **Wylogowanie:** Użytkownik może bezpiecznie wylogować się z aplikacji.
    
- **Usuwanie Konta:** W ustawieniach użytkownik może trwale usunąć swoje konto. Proces ten jest poprzedzony pytaniem o chęć eksportu wszystkich wpisów do pliku tekstowego.
    

#### **3.2. Zarządzanie Wpisami (CRUD)**

- **Tworzenie Wpisu:** Interfejs do pisania to czyste pole tekstowe (`textarea`) bez opcji formatowania. Wpis nie posiada oddzielnego pola na tytuł.
    
- **Przeglądanie Wpisów:** Wpisy są wyświetlane na liście w porządku odwrotnie chronologicznym. Każdy element listy pokazuje datę utworzenia oraz pierwszą linijkę tekstu (traktowaną jako tytuł).
    
- **Edycja Wpisu:** Kliknięcie na wpis z listy otwiera go w trybie edycji.
    
- **Usuwanie Wpisu:** Użytkownik może usunąć wpis. Akcja ta musi zostać potwierdzona w oknie dialogowym, aby zapobiec przypadkowej utracie danych.
    

#### **3.3. Interfejs i Doświadczenie Użytkownika (UI/UX)**

- **Onboarding:** Nowy użytkownik po zalogowaniu widzi pierwszy, automatycznie wygenerowany wpis powitalny, który wyjaśnia cel aplikacji i zachęca do działania.
    
- **Mechanizm Opinii:** W stopce aplikacji znajduje się link `mailto:`, umożliwiający szybkie przesłanie opinii do twórcy.
    
- **Ustawienia:** Strona ustawień w MVP zawiera wyłącznie opcje zmiany hasła i wylogowania.
    

#### **3.4. Wymagania Niefunkcjonalne**

- **Bezpieczeństwo:** Dane użytkownika są szyfrowane podczas przesyłania (`in-transit`) oraz na serwerze (`at-rest`). Dostęp do danych jest ograniczony politykami Row Level Security.
    
- **Responsywność:** Aplikacja jest zaprojektowana w podejściu `mobile-first` i jest w pełni funkcjonalna w przeglądarkach na urządzeniach mobilnych i stacjonarnych.
    
- **Tryb Offline i PWA (Progressive Web App):** Aplikacja będzie wdrożona jako PWA, aby zapewnić natychmiastowe działanie w trybie offline (dzięki buforowaniu zasobów aplikacji) oraz możliwość jej "instalacji" na urządzeniach. W przypadku utraty połączenia, aktualnie edytowany wpis jest dodatkowo zapisywany w lokalnej pamięci przeglądarki (`Local Storage`) i synchronizowany po odzyskaniu połączenia.
    

---

### **4. Kryteria Sukcesu MVP**

- **Wskaźnik ilościowy:** Wdrożenie anonimowego mechanizmu śledzącego **"streak"** (liczbę kolejnych dni z co najmniej jednym wpisem), który pozwoli ocenić, czy aplikacja faktycznie wspiera budowanie nawyku. Wskaźnik ten będzie widoczny dla użytkownika jako dyskretna ikona w interfejsie.
    
- **Wskaźnik jakościowy:** Aktywne zbieranie i analiza opinii od pierwszych użytkowników w celu zrozumienia, czy zachowany został właściwy balans między prostotą a użytecznością.
    

---

### **5. Stos Technologiczny, Infrastruktura i Ograniczenia**

- **Zespół:** Projekt realizowany jest przez jednego dewelopera.
    
- **Harmonogram:** Czas na wdrożenie wersji MVP wynosi **4 tygodnie**.
    
- **Stos Technologiczny (uproszczony):**
    
    - **Frontend:** **Blazor WebAssembly**.
        
    - **Backend as a Service (BaaS):** **Supabase**. Pełni rolę backendu, dostarczając bazę danych PostgreSQL oraz gotowy, bezpieczny system do autentykacji i zarządzania danymi. Eliminuje to potrzebę tworzenia własnego API na etapie MVP.
        
- **Infrastruktura i Wdrożenie (CI/CD):**
    
    - **Repozytorium Kodu:** **GitHub**.
        
    - **CI/CD:** **GitHub Actions** do automatycznej kompilacji i wdrażania aplikacji po każdym `push` do głównego brancha.
        
    - **Hosting:** **Azure Static Web Apps** (lub alternatywnie Netlify/Vercel) jako platforma do hostowania statycznych plików aplikacji Blazor WASM.
        

---

### **6. Przyszły Rozwój (Poza Zakresem MVP)**

Funkcje uszeregowane według priorytetów do wdrożenia w przyszłych wersjach:

1. **Wysoki priorytet:** Wdrożenie aplikacji jako **Progressive Web App (PWA)**, Tryb ciemny, Eksport wszystkich wpisów do pliku.
    
2. **Średni priorytet:** Wprowadzenie tagów i wyszukiwania w treści (w sposób nieinwazyjny dla interfejsu).
    
3. **Niski priorytet:** Implementacja edytora WYSIWYG generującego w tle składnię Markdown.
    
4. **Do analizy:** Funkcja przetwarzania mowy na tekst (wymaga analizy kosztów i złożoności API).
    

---

### **7. Szczegółowe Historyjki Użytkownika (User Stories)**

#### **Moduł: Zarządzanie Kontem**

1. **Jako nowy użytkownik,** chcę móc zarejestrować się w aplikacji przy użyciu mojego adresu e-mail i hasła, aby stworzyć swoje prywatne konto.
    
2. **Jako użytkownik próbujący się zarejestrować,** chcę zobaczyć zrozumiały komunikat o błędzie, jeśli mój e-mail jest już zajęty lub hasło jest zbyt słabe, aby móc poprawić dane.
    
3. **Jako zarejestrowany użytkownik,** chcę móc zalogować się do aplikacji za pomocą mojego e-maila i hasła, aby uzyskać dostęp do mojego dziennika.
    
4. **Jako użytkownik próbujący się zalogować,** chcę zobaczyć komunikat o błędzie, jeśli podałem nieprawidłowe dane, abym wiedział, że muszę spróbować ponownie.
    
5. **Jako zalogowany użytkownik,** chcę mieć możliwość wylogowania się z aplikacji, aby zabezpieczyć dostęp do mojego konta na współdzielonym urządzeniu.
    
6. **Jako użytkownik, który zapomniał hasła,** chcę móc skorzystać z funkcji "resetuj hasło", aby odzyskać dostęp do mojego konta.
    
7. **Jako zalogowany użytkownik,** chcę mieć dostęp do strony "Ustawienia", gdzie mogę zmienić swoje hasło, aby dbać o bezpieczeństwo konta.
    

#### **Moduł: Zarządzanie Wpisami**

8. **Jako nowy użytkownik,** po pierwszym zalogowaniu chcę zobaczyć wpis powitalny, aby zrozumieć, jak działa aplikacja i od czego zacząć.
    
9. **Jako zalogowany użytkownik,** chcę widzieć listę wszystkich moich wpisów w porządku od najnowszego do najstarszego, aby mieć szybki przegląd swojej historii.
    
10. **Jako zalogowany użytkownik,** chcę móc kliknąć przycisk "Nowy wpis", aby otworzyć pusty edytor i zacząć pisać.
    
11. **Jako użytkownik piszący nowy wpis,** chcę, aby po zapisaniu wpis pojawił się na górze listy, abym miał pewność, że został dodany.
    
12. **Jako użytkownik przeglądający listę wpisów,** chcę móc kliknąć na dowolny wpis, aby otworzyć go do odczytu i edycji.
    
13. **Jako użytkownik edytujący istniejący wpis,** chcę, aby moje zmiany zostały zapisane, kiedy zakończę edycję.
    
14. **Jako użytkownik,** chcę móc usunąć wybrany wpis, ale najpierw chcę zobaczyć okno z prośbą o potwierdzenie, aby uniknąć przypadkowego skasowania danych.
    

#### **Moduł: Doświadczenie Użytkownika i Wymagania Niefunkcjonalne**

15. **Jako użytkownik piszący długi tekst,** chcę, aby aplikacja zapisywała moją pracę w tle w razie utraty połączenia internetowego, abym nie stracił/a niezapisanych fragmentów.
    
16. **Jako użytkownik,** chcę, aby interfejs aplikacji był czytelny i funkcjonalny na moim telefonie komórkowym, abym mógł prowadzić dziennik z dowolnego miejsca.
    
17. **Jako świadomy użytkownik,** chcę mieć możliwość trwałego usunięcia mojego konta i wszystkich moich danych, a przed tym krokiem chcę otrzymać propozycję ich eksportu, aby zachować swoją historię.
</product_requirements>

<tech_stack>
### **Frontend - Blazor WebAssembly do budowy interaktywnego interfejsu:**
* **Blazor WebAssembly** pozwala na tworzenie całej aplikacji w języku C# i ekosystemie .NET, co zapewnia spójność kodu i wykorzystanie istniejących umiejętności dewelopera.
* Logika aplikacji działa po stronie klienta (w przeglądarce), co przekłada się na wysoką interaktywność i szybkie działanie interfejsu użytkownika.
* Do stylowania zostanie użyty **Pico.css**, ultra-lekki, "bezklasowy" framework CSS. Zapewnia to nowoczesny i minimalistyczny wygląd standardowym elementom HTML przy niemal zerowym narzucie konfiguracyjnym i idealnie wpisuje się w filozofię prostoty projektu.

### **Backend - Supabase jako kompleksowe rozwiązanie Backend-as-a-Service (BaaS):**
* Zapewnia bazę danych **PostgreSQL**, idealną do przechowywania wpisów w dzienniku.
* Dostarcza gotowy i bezpieczny system **autentykacji użytkowników** (obsługuje m.in. logowanie, rejestrację i reset hasła).
* Oferuje biblioteki klienckie (**SDK**) dla .NET, co pozwala na bezpośrednią i bezpieczną komunikację z frontendu Blazor.
* Jest rozwiązaniem **open source**, które na start można używać w ramach hojnego darmowego planu hostingowego.

### **CI/CD i Hosting:**
* **GitHub Actions** do w pełni zautomatyzowanego procesu CI/CD (budowanie, testowanie i wdrażanie aplikacji po każdej zmianie w kodzie).
* **Azure Static Web Apps** do hostowania aplikacji, zapewniając darmowy certyfikat SSL, globalny CDN i idealną, natywną integrację ze środowiskiem .NET i Blazorem.

### **Monitoring, Logowanie i Obsługa Błędów:**
* **Serilog** jako standardowa biblioteka do logowania strukturalnego w aplikacjach .NET.
* Logi będą wysyłane do dedykowanej usługi w chmurze (np. **Sentry**, **Seq** lub **Azure Application Insights**), co umożliwi centralne gromadzenie i analizę błędów.
* Wdrożenie mechanizmu **ID Korelacji** (Correlation ID) do powiązania przyjaznego komunikatu o błędzie widocznego dla użytkownika z pełnym, technicznym logiem zapisanym w systemie.
* Stosowanie zasady oddzielenia informacji: użytkownik otrzymuje prosty komunikat, a deweloper kompletne dane diagnostyczne do debugowania.
</tech_stack>

<api_plan>
# REST API Plan

This document outlines the REST API for the 10xJournal application. The architecture leverages Supabase as a Backend-as-a-Service (BaaS), utilizing its auto-generated PostgREST API for standard CRUD operations and custom PostgreSQL functions (exposed as RPC endpoints) for specific business logic.

## 1\. Resources

The API exposes the following primary resources, which directly map to the database tables:

  * **Journal Entries**: The core content created by users.
      * Database Table: `public.journal_entries`
  * **User Streaks**: Data related to a user's writing habit.
      * Database Table: `public.user_streaks`
  * **User Profile**: Public profile information for a user.
      * Database Table: `public.profiles`

## 2\. Endpoints

All endpoints are relative to the Supabase project URL (e.g., `https://<project-ref>.supabase.co`).

-----

### **Resource: Journal Entries**

Provides full CRUD access to a user's own journal entries. Access is governed by Row Level Security (RLS) policies, ensuring users can only interact with their own data.

#### **List All Journal Entries**

  * **Method**: `GET`
  * **Path**: `/rest/v1/journal_entries`
  * **Description**: Retrieves a list of all journal entries for the authenticated user, sorted from newest to oldest.
  * **Query Parameters**:
      * `select=*`: (Required) Specifies that all columns should be returned.
      * `order=created_at.desc`: (Required) Sorts the entries by creation date in descending order, as specified in the PRD.
  * **Request Payload**: N/A
  * **Success Response**:
      * **Code**: `200 OK`
      * **Payload**:
        ```json
        [
            {
                "id": "a1b2c3d4-e5f6-a7b8-c9d0-e1f2a3b4c5d6",
                "user_id": "f1g2h3i4-j5k6-l7m8-n9o0-p1q2r3s4t5u6",
                "content": "This is my latest journal entry.",
                "created_at": "2025-10-11T18:00:00Z",
                "updated_at": "2025-10-11T18:00:00Z"
            },
            {
                "id": "b2c3d4e5-f6a7-b8c9-d0e1-f2a3b4c5d6e7",
                "user_id": "f1g2h3i4-j5k6-l7m8-n9o0-p1q2r3s4t5u6",
                "content": "This was my first entry, the welcome message.",
                "created_at": "2025-10-10T10:00:00Z",
                "updated_at": "2025-10-10T10:00:00Z"
            }
        ]
        ```
  * **Error Response**:
      * **Code**: `401 Unauthorized` - If the request is missing or has an invalid JWT.

-----

#### **Create a New Journal Entry**

  * **Method**: `POST`
  * **Path**: `/rest/v1/journal_entries`
  * **Description**: Creates a new journal entry. The `user_id` is inferred from the authenticated user. A database trigger automatically updates the user's streak upon successful insertion.
  * **Request Payload**:
    ```json
    {
        "content": "A new thought I had today."
    }
    ```
  * **Success Response**:
      * **Code**: `201 Created`
      * **Payload**: The newly created journal entry object.
        ```json
        {
            "id": "c3d4e5f6-a7b8-c9d0-e1f2-a3b4c5d6e7f8",
            "user_id": "f1g2h3i4-j5k6-l7m8-n9o0-p1q2r3s4t5u6",
            "content": "A new thought I had today.",
            "created_at": "2025-10-12T09:30:00Z",
            "updated_at": "2025-10-12T09:30:00Z"
        }
        ```
  * **Error Responses**:
      * **Code**: `400 Bad Request` - If `content` is null or missing.
      * **Code**: `401 Unauthorized` - Invalid JWT.

-----

#### **Update a Journal Entry**

  * **Method**: `PATCH`
  * **Path**: `/rest/v1/journal_entries?id=eq.<entry_id>`
  * **Description**: Updates the content of a specific journal entry.
  * **Request Payload**:
    ```json
    {
        "content": "An updated version of my thoughts."
    }
    ```
  * **Success Response**:
      * **Code**: `200 OK`
      * **Payload**: The updated journal entry object.
  * **Error Responses**:
      * **Code**: `401 Unauthorized` - Invalid JWT.
      * **Code**: `404 Not Found` - If the entry does not exist or does not belong to the user.

-----

#### **Delete a Journal Entry**

  * **Method**: `DELETE`
  * **Path**: `/rest/v1/journal_entries?id=eq.<entry_id>`
  * **Description**: Permanently deletes a specific journal entry.
  * **Request Payload**: N/A
  * **Success Response**:
      * **Code**: `204 No Content`
  * **Error Responses**:
      * **Code**: `401 Unauthorized` - Invalid JWT.
      * **Code**: `404 Not Found` - If the entry does not exist or does not belong to the user.

-----

### **Resource: User Streaks**

Provides read-only access to the user's streak data. This data is calculated and updated by database triggers, not directly by the client.

#### **Get User Streak**

  * **Method**: `GET`
  * **Path**: `/rest/v1/user_streaks`
  * **Description**: Retrieves the writing streak data for the authenticated user.
  * **Query Parameters**:
      * `select=*`
  * **Request Payload**: N/A
  * **Success Response**:
      * **Code**: `200 OK`
      * **Payload**: A single object in an array.
        ```json
        [
            {
                "user_id": "f1g2h3i4-j5k6-l7m8-n9o0-p1q2r3s4t5u6",
                "current_streak": 5,
                "longest_streak": 12,
                "last_entry_date": "2025-10-11"
            }
        ]
        ```
  * **Error Response**:
      * **Code**: `401 Unauthorized` - Invalid JWT.

-----

### **Business Logic: Account Management (RPC)**

These endpoints handle business logic that goes beyond simple CRUD operations.

#### **Export All Journal Entries**

  * **Method**: `POST`
  * **Path**: `/rpc/export_journal_entries`
  * **Description**: A custom function that retrieves all journal entries for the authenticated user and formats them into a single JSON object for easy client-side file generation.
  * **Request Payload**: N/A
  * **Success Response**:
      * **Code**: `200 OK`
      * **Payload**:
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
  * **Error Response**:
      * **Code**: `401 Unauthorized` - Invalid JWT.

-----

#### **Delete User Account**

  * **Method**: `POST`
  * **Path**: `/rpc/delete_my_account`
  * **Description**: A secure function that permanently deletes the user's record from `auth.users`. Due to `ON DELETE CASCADE` constraints, this action will automatically and irreversibly delete the user's profile, all journal entries, and streak data. This must be called after the user has confirmed the action and exported their data.
  * **Request Payload**: N/A
  * **Success Response**:
      * **Code**: `200 OK`
      * **Payload**:
        ```json
        {
            "status": "success",
            "message": "Account and all associated data have been permanently deleted."
        }
        ```
  * **Error Response**:
      * **Code**: `401 Unauthorized` - Invalid JWT.

-----

## 3\. Authentication and Authorization

  * **Authentication Mechanism**: Authentication is handled by Supabase Auth. The client (Blazor WASM) is responsible for the sign-up and login flows. Upon successful authentication, Supabase provides a **JSON Web Token (JWT)**.
  * **Implementation**: Every request to the endpoints listed above (excluding auth-specific endpoints like login/signup) must include an `Authorization` header with the JWT as a Bearer token.
      * **Example Header**: `Authorization: Bearer <your_jwt_token>`
  * **Authorization**: Authorization is enforced at the database level using **PostgreSQL's Row Level Security (RLS)**. The policies defined in the database schema ensure that the `auth.uid()` from the JWT is matched against the `user_id` or `id` in the tables, strictly isolating data between users.

-----

## 4\. Validation and Business Logic

  * **Data Validation**: Primary data validation is enforced by the database schema's constraints (`NOT NULL`, `FOREIGN KEY`, data types). For example, attempting to create a journal entry with `content: null` will be rejected by the database, and PostgREST will return a `400 Bad Request` status code.
  * **Business Logic Implementation**: Key business logic is automated within the PostgreSQL database using **triggers and functions** to ensure consistency and security.
      * **User Onboarding**: A trigger on `auth.users` automatically creates corresponding records in `public.profiles` and `public.user_streaks` upon user registration.
      * **Welcome Entry**: A subsequent trigger on `public.profiles` creates the initial welcome message in `public.journal_entries`.
      * **Streak Calculation**: A trigger on `public.journal_entries` fires after every `INSERT` to call a function that recalculates and updates the `current_streak`, `longest_streak`, and `last_entry_date` in the `public.user_streaks` table.

This server-centric approach minimizes the logic required on the client side, making the frontend application simpler and ensuring that business rules are always applied, regardless of how the data is accessed.

All those endpoints are on the side of Supabase and can be directly consumed by the Blazor WebAssembly frontend using the Supabase .NET SDK, which abstracts away much of the HTTP request handling and JWT management. This project do not require a custom backend server, as Supabase provides all necessary backend functionality out-of-the-box.
</api_plan>

Przeanalizuj dostarczone informacje, koncentrując się na aspektach istotnych dla projektowania interfejsu użytkownika. Rozważ następujące kwestie:

1. Zidentyfikuj kluczowe widoki i ekrany na podstawie wymagań produktu i dostępnych endpointów API.
2. Określ potencjalne przepływy użytkownika i nawigację między widokami, uwzględniając możliwości API.
3. Rozważ komponenty UI i wzorce interakcji, które mogą być konieczne do efektywnej komunikacji z API.
4. Pomyśl o responsywności i dostępności interfejsu.
5. Oceń wymagania bezpieczeństwa i uwierzytelniania w kontekście integracji z API.
6. Rozważ wszelkie konkretne biblioteki UI lub frameworki, które mogą być korzystne dla projektu.
7. Przeanalizuj, jak struktura API wpływa na projekt UI i przepływy danych w aplikacji.

Na podstawie analizy wygeneruj listę pytań i zaleceń. Powinny one dotyczyć wszelkich niejasności, potencjalnych problemów lub obszarów, w których potrzeba więcej informacji, aby stworzyć efektywną architekturę UI. Rozważ pytania dotyczące:

1. Hierarchia i organizacja widoków w odniesieniu do struktury API
2. Przepływy użytkownika i nawigacja wspierane przez dostępne endpointy
3. Responsywność i adaptacja do różnych urządzeń
4. Dostępność i inkluzywność
5. Bezpieczeństwo i autoryzacja na poziomie UI w powiązaniu z mechanizmami API
6. Spójność designu i doświadczenia użytkownika
7. Strategia zarządzania stanem aplikacji i synchronizacji z API
8. Obsługa stanów błędów i wyjątków zwracanych przez API
9. Strategie buforowania i optymalizacji wydajności w komunikacji z API

Dane wyjściowe powinny mieć następującą strukturę:

<pytania>
W tym miejscu proszę wymienić pytania i zalecenia, dla przejrzystości opatrzone numerami:

Na przykład:
1. Czy na stronie wpisu powinien znajdować się tytuł jako osbne pole tekstowe?

Rekomendacja: Nie, tytuł nie będzie edytowalny osobno, będzie generowany automatycznie na podstawie pierwszych słów treści wpisu.
</pytania>

Pamiętaj, że Twoim celem jest dostarczenie kompleksowej listy pytań i zaleceń, które pomogą w stworzeniu solidnej architektury UI dla MVP, w pełni zintegrowanej z dostępnymi endpointami API. Skoncentruj się na jasności, trafności i dokładności swoich wyników. Nie dołączaj żadnych dodatkowych komentarzy ani wyjaśnień poza określonym formatem wyjściowym.

Kontynuuj ten proces, generując nowe pytania i rekomendacje w oparciu o przekazany kontekst i odpowiedzi użytkownika, dopóki użytkownik wyraźnie nie poprosi o podsumowanie.

Pamiętaj, aby skupić się na jasności, trafności i dokładności wyników. Nie dołączaj żadnych dodatkowych komentarzy ani wyjaśnień poza określonym formatem wyjściowym.









\<pytania\>

1.  Jaka powinna być główna struktura nawigacyjna i układ aplikacji dla zalogowanego użytkownika?

    **Odpowiedź:** Proponuję zastosowanie jednokolumnowego, wyśrodkowanego layoutu z maksymalną szerokością dla czytelności na desktopie. Wersja mobilna (`mobile-first`) powinna mieć stały pasek nawigacyjny na dole ekranu zawierający linki do: **Listy wpisów** (główny widok), przycisku **"Nowy Wpis"** oraz **Ustawień**. Wskaźnik "streak" powinien być dyskretnie umieszczony obok linku do Ustawień.

2.  W jaki sposób ma być realizowane zapisywanie zmian w edytorze wpisu — czy ma być to akcja manualna (przycisk "Zapisz"), czy automatyczna?

    **Odpowiedź:** Zgodnie z filozofią minimalizmu, rekomenduję wdrożenie mechanizmu **automatycznego zapisu (auto-save)**. Zmiany powinny być wysyłane do endpointu `PATCH /rest/v1/journal_entries` kilka sekund po tym, jak użytkownik przestanie pisać (tzw. debouncing), aby zminimalizować liczbę zapytań. W interfejsie powinien pojawić się subtelny wskaźnik statusu (np. tekst "Zapisano" lub ikona chmury).

3.  Jak interfejs użytkownika powinien komunikować stan offline oraz proces synchronizacji danych z `Local Storage` po odzyskaniu połączenia?

    **Odpowiedź:** Aplikacja powinna wyświetlać globalny, nietablokujący wskaźnik (np. mała ikona lub pasek na górze/dole ekranu) informujący o utracie połączenia. W widoku edycji wpisu, obok wskaźnika statusu zapisu, powinien pojawić się komunikat "Pracujesz w trybie offline. Zmiany zostaną zsynchronizowane". Po odzyskaniu połączenia, wskaźnik powinien zmienić status na "Synchronizowanie...", a następnie zniknąć po pomyślnym zapisaniu danych.

4.  Jaki jest dokładny, krok po kroku, przepływ interakcji użytkownika podczas usuwania konta, uwzględniając wymóg eksportu danych?

    **Odpowiedź:** Na stronie Ustawień, kliknięcie przycisku "Usuń konto" powinno otworzyć okno modalne, które:

      * Wyraźnie informuje o nieodwracalności operacji i usunięciu wszystkich danych użytkownika z bazy nieodwracalnie.
      * Zawiera przycisk **"Eksportuj wszystkie wpisy"**, który wywołuje endpoint `POST /rpc/export_journal_entries` i inicjuje pobieranie pliku po stronie klienta.
      * Wymaga od użytkownika wpisania swojego hasła oraz frazy "usuń moje dane", aby potwierdzić świadomą decyzję.
      * Posiada przycisk **"Usuń konto na stałe"**, który jest nieaktywny do momentu poprawnego potwierdzenia hasła oraz wpisania frazy "usuń moje dane" i wywołuje endpoint `POST /rpc/delete_my_account`.

5.  Jak technicznie i wizualnie ma być zrealizowane wyświetlanie pierwszej linijki tekstu jako tytułu na liście wpisów?

    **Odpowiedź:** Po stronie frontendu (Blazor), pierwsza linia tekstu z pola `content` powinna być ekstrahowana (np. tekst do znaku nowej linii `\n`). Należy nałożyć limit długości (np. 100 znaków) i w razie potrzeby dodać elipsę (`...`). W kodzie HTML pierwsza linia powinna być opakowana w element nagłówka (np. `<h3>`), a reszta podglądu w paragraf (`<p>`), aby semantycznie odzwierciedlić jej rolę.

6.  Jak aplikacja powinna obsługiwać i prezentować użytkownikowi różne stany ładowania danych oraz błędy zwracane przez API?

    **Odpowiedź:**
      * **Stany ładowania:** Podczas pobierania listy wpisów lub otwierania pojedynczego wpisu, należy wyświetlić szkieletowy interfejs prosty wskaźnik ładowania, aby uniknąć pustych ekranów.
      * **Obsługa błędów:** Należy użyć globalnego systemu powiadomień (tzw. "toastów"). Błędy walidacji (400) powinny wyświetlać komunikat "Wystąpił błąd, spróbuj ponownie". Błąd autoryzacji (401) powinien automatycznie wylogować użytkownika i przekierować go na stronę logowania.
      Jeśli wystąpi to podczas edytowania wpisu, należy wyświetlić komunikat "Twoja sesja wygasła, zaloguj się ponownie". Błędy serwera (500) powinny informować "Wystąpił problem po stronie serwera, spróbuj ponownie później". Wpis użytkownika powinien być zachowany lokalnie, aby nie utracić danych.
      Do każdego komunikatu błędu powinien być dołączony Correlation ID, który użytkownik może podać w mailu zwrotnym.

7.  W jaki sposób aplikacja ma zachęcać użytkownika do jej "instalacji" jako PWA (Progressive Web App)?

    **Odpowiedź:** Po napisaniu 2-3 wpisów, aplikacja może wyświetlić dyskretny, możliwy do zamknięcia baner z komunikatem "Dodaj 10xJournal do ekranu głównego, aby mieć szybszy dostęp i pisać offline" wraz z przyciskiem inicjującym proces instalacji PWA.
    W sekcji pomocy lub w stopce aplikacji powinien znajdować się link do instrukcji ręcznej instalacji PWA na różnych platformach (iOS, Android, Desktop).

8.  Czy widok edycji wpisu i tworzenia nowego wpisu to ten sam komponent/strona, czy dwa oddzielne?

    **Odpowiedź:** Powinien to być **jeden, ten sam widok/komponent** routowalny pod adresem, np. `/entry/{id}` dla edycji istniejącego wpisu oraz `/entry/new` dla tworzenia nowego. Użycie jednego komponentu upraszcza logikę i utrzymanie kodu. Komponent ten będzie odpowiedzialny za pobieranie danych (jeśli `id` istnieje) lub inicjowanie pustego stanu (dla `new`).

9.  Jak ma wyglądać interakcja z oknem dialogowym potwierdzającym usunięcie pojedynczego wpisu?

    **Odpowiedź:** Po kliknięciu ikony usuwania przy wpisie, należy użyć natywnego okna dialogowego przeglądarki (`window.confirm()`) z prostym pytaniem: "Czy na pewno chcesz usunąć ten wpis? Tej operacji nie można cofnąć.". Jest to najprostsze rozwiązanie, zgodne z minimalistycznym podejściem i nie wymaga dodatkowych bibliotek ani komponentów.

\</pytania\>


\<pytania\>

1.  Jaki jest dokładny przepływ onboardingu dla nowego użytkownika zaraz po pomyślnej rejestracji?

    **Odpowiedź:** Aby zapewnić jak najbardziej płynne pierwsze doświadczenie, użytkownik po pomyślnej rejestracji (potwierdzeniu adresu e-mail) powinien zostać **automatycznie zalogowany** i natychmiast przekierowany do głównego widoku listy wpisów. Wpis powitalny powinien być już widoczny na górze listy, co od razu pokaże mu, jak wygląda aplikacja i zachęci do interakcji.

2.  Jak powinny być zaprojektowane i zorganizowane formularze rejestracji, logowania i resetowania hasła?

    **Odpowiedź:** Dla zachowania prostoty i zgodności z powszechnymi wzorcami, każdy z tych procesów powinien mieć swoją dedykowaną stronę/ścieżkę (np. `/login`, `/register`, `/reset-password`). Wszystkie trzy widoki powinny współdzielić minimalistyczny, wyśrodkowany układ, zawierający logo aplikacji, niezbędne pola formularza, przycisk akcji oraz link do alternatywnego procesu (np. na stronie logowania link "Nie masz konta? Zarejestruj się").

3.  Jak dokładnie ma być wizualizowany wskaźnik "streak" i jak ma komunikować różne stany (np. aktywna seria, brak serii)?

    **Odpowiedź:** Proponuję użycie kombinacji ikony ognia (np. emoji 🔥) oraz liczby dni. Gdy seria jest aktywna, wskaźnik wyglądałby tak: "🔥 5". Jeśli użytkownik nie ma aktywnej serii (streak = 0), ikona powinna być całkowicie ukryta, aby nie rozpraszać. Dodanie atrybutu `title` do elementu wyświetli pomocniczy tekst po najechaniu myszką, np. "Twoja seria pisania: 5 dni z rzędu".

4.  Jak lista wpisów powinna się zachowywać przy dużej liczbie wpisów (np. \>100)?

    **Odpowiedź:** Na potrzeby MVP najprostszym i w pełni wystarczającym rozwiązaniem jest **pobieranie wszystkich wpisów** za jednym razem. Biorąc pod uwagę, że wpisy to sam tekst, problemy z wydajnością przy kilkuset wpisach są mało prawdopodobne. Implementacja "nieskończonego przewijania" (infinite scroll) lub paginacji może zostać dodana w przyszłości jako optymalizacja, jeśli dane o użytkowaniu wskażą taką potrzebę.

5.  Jak widok listy wpisów powinien wyglądać, gdy użytkownik nie ma żadnych wpisów (np. po usunięciu wszystkich)?

    **Odpowiedź:** Zamiast pustego ekranu należy wyświetlić pomocny komunikat "pustego stanu" (empty state). Powinien on zawierać wyśrodkowany tekst, np. "Twoja historia czeka na zapisanie. Stwórz swój pierwszy wpis\!", oraz wyraźny przycisk "Nowy wpis", który bezpośrednio przenosi użytkownika do edytora.

6.  Jakie konkretne kroki, oprócz semantycznego HTML, zostaną podjęte, aby zapewnić dostępność (Accessibility, a11y) interfejsu?

    **Odpowiedź:** Należy zapewnić, aby wszystkie elementy interaktywne (przyciski, linki) miały wyraźnie widoczny stan `:focus` (np. obramowanie). Dla dynamicznie pojawiających się komunikatów (np. "Zapisano", błędy) należy użyć atrybutu `aria-live="polite"`, aby czytniki ekranu informowały o nich użytkownika. Projekt wizualny musi od początku spełniać wymogi kontrastu WCAG na poziomie AA.

7.  Jak edytor wpisów powinien zarządzać stanem, w którym istnieją niezapisane zmiany (tzw. `dirty state`), zwłaszcza w kontekście utraty połączenia?

    **Odpowiedź:** Komponent edytora powinien śledzić, czy wprowadzone zmiany zostały pomyślnie wysłane do serwera. Jeśli użytkownik spróbuje zamknąć stronę lub przejść do innego widoku, a istnieją zmiany, które nie mogły zostać zapisane (np. z powodu trybu offline), należy użyć zdarzenia `window.beforeunload`, aby wyświetlić natywne okno przeglądarki z ostrzeżeniem o możliwej utracie danych.

8.  Jaka strategia zostanie przyjęta do zarządzania stanem globalnym aplikacji w Blazorze, takim jak status uwierzytelnienia użytkownika czy wartość "streak"?

    **Odpowiedź:** Dla MVP wystarczy zastosować wbudowane mechanizmy Blazora. Status autentykacji można zarządzać za pomocą `AuthenticationStateProvider`. Dla innych globalnych danych, jak licznik "streak", można stworzyć prostą, singletonową usługę (np. `UserDataService`) wstrzykiwaną przez Dependency Injection do komponentów, które jej potrzebują (np. do paska nawigacyjnego). Unikniemy w ten sposób potrzeby implementacji bardziej złożonych bibliotek do zarządzania stanem.

\</pytania\>




\<pytania\>

1.  Jaki jest dokładny przepływ użytkownika podczas procesu resetowania hasła?

    **Odpowiedź:** Po wpisaniu adresu e-mail na stronie `/reset-password` i kliknięciu przycisku, użytkownik powinien zobaczyć komunikat: "Jeśli konto istnieje, na podany adres e-mail wysłano instrukcję resetowania hasła." Następnie użytkownik otrzymuje e-mail od Supabase z linkiem, który prowadzi go do specjalnej strony w aplikacji (np. `/update-password`), gdzie może wprowadzić i potwierdzić nowe hasło. Po pomyślnej zmianie hasła, użytkownik jest automatycznie przekierowywany na stronę logowania z komunikatem "Hasło zostało zmienione. Możesz się teraz zalogować."

2.  W jaki sposób interfejs ma reagować na kliknięcie przycisku "Nowy wpis"? Czy lista wpisów powinna pozostać widoczna w tle?

    **Odpowiedź:** Po kliknięciu "Nowy wpis", aplikacja powinna natychmiast przekierować użytkownika do widoku edytora pod adresem `/entry/new`. Widok listy powinien zostać całkowicie zastąpiony przez interfejs edytora, aby maksymalnie skupić uwagę użytkownika na pisaniu i zachować spójność z nawigacją opartą na URL. Powrót do listy następuje przez kliknięcie linku "Lista wpisów" w nawigacji lub użycie przycisku "wstecz" w przeglądarce.

3.  Jakie elementy powinien zawierać widok Ustawień, oprócz zmiany hasła i usuwania konta?

    **Odpowiedź:** Zgodnie z PRD, na etapie MVP widok Ustawień powinien być skrajnie minimalistyczny i zawierać wyłącznie dwie sekcje: "Zmień hasło" (z polami na stare i nowe hasło) oraz "Strefa niebezpieczna" z przyciskiem "Usuń konto na stałe". Dodatkowo, w stopce tej strony można umieścić link `mailto:` do kontaktu oraz informację o wersji aplikacji.

4.  Czy interfejs użytkownika powinien w jakiś sposób ograniczać długość pojedynczego wpisu?

    **Odpowiedź:** Tak, ale do rozmiaru pola `textarea`. Na etapie MVP nie trzeba wprowadzać żadnych dodatkowych ograniczeń długości wpisu po stronie frontendu. Pole `textarea` powinno rosnąć dynamicznie wraz z treścią. Ograniczenia wynikające z typu danych w bazie PostgreSQL są na tyle wysokie, że są praktycznie nieosiągalne dla typowego użytkownika.

5.  Jak aplikacja powinna obsłużyć sytuację, gdy token JWT użytkownika wygaśnie podczas aktywnej sesji (np. podczas pisania)?

    **Odpowiedź:** Jeśli wywołanie API (np. auto-zapis) zwróci błąd `401 Unauthorized`, aplikacja powinna zapisać bieżącą treść wpisu w `Local Storage`, a następnie wyświetlić okno modalne z komunikatem "Twoja sesja wygasła. Zaloguj się ponownie, aby kontynuować." i przyciskiem przekierowującym na stronę logowania. Po ponownym zalogowaniu aplikacja powinna sprawdzić, czy w `Local Storage` znajduje się niezapisany wpis i zapytać użytkownika, czy chce go przywrócić.

6.  Jaka dokładnie treść powinna znaleźć się w automatycznie generowanym wpisie powitalnym?

    **Odpowiedź:** Wpis powitalny powinien być krótki i motywujący. Proponowana treść: "Witaj w 10xJournal\!\\n\\nTo jest Twoja prywatna przestrzeń do myślenia i pisania, wolna od rozpraszaczy. Celem tej aplikacji jest pomóc Ci w budowaniu nawyku regularnego prowadzenia dziennika.\\n\\nMożesz edytować lub usunąć ten wpis. Kliknij przycisk 'Nowy wpis', aby rozpocząć swoją historię."

7.  Czy aplikacja powinna prosić użytkownika o potwierdzenie przed opuszczeniem widoku edycji, jeśli wpis jest pusty?

    **Odpowiedź:** Nie. Jeśli użytkownik wejdzie do widoku tworzenia nowego wpisu (`/entry/new`), ale nic nie napisze i spróbuje wrócić do listy, aplikacja powinna pozwolić mu na to bez żadnych ostrzeżeń. Mechanizm `window.beforeunload` powinien być aktywowany tylko wtedy, gdy w edytorze znajduje się jakakolwiek treść, która nie została jeszcze zapisana na serwerze.

\</pytania\>





\<pytania\>

1.  W jaki sposób interfejs ma sygnalizować poszczególne stany mechanizmu auto-zapisu (w trakcie zapisywania, zapisano, błąd)?

    **Odpowiedź:** Proponuję użycie dyskretnego wskaźnika tekstowego w widoku edytora. Gdy użytkownik przestaje pisać i uruchamiany jest zapis, pojawia się tekst "Zapisywanie..." pod oknem edytora. Po pomyślnym zapisie zmienia się on na "Zapisano" i znika po 2 sekundach. W przypadku błędu (np. utraty połączenia), wyświetlany jest stały komunikat "Błąd zapisu. Dane są bezpieczne lokalnie." pod oknem edytora, który zniknie dopiero po udanej synchronizacji - jest dodawany jako element danego wpisu i kiedy użytkownik przejdzie do innego wpisu, a potem wróci do niezapisanego wpisu - komunikat powinien być widoczny.


2.  W jakim formacie powinny być wyświetlane daty na liście wpisów, aby były jak najbardziej czytelne i intuicyjne?

    **Odpowiedź:** Zastosujmy formatowanie relatywne dla najnowszych wpisów i absolutne dla starszych. Na przykład:

      * Wpisy z dzisiaj: "dzisiaj, 15:30"
      * Wpisy z wczoraj: "wczoraj, 20:10"
      * Wpisy z ostatniego tygodnia: "środa, 11:45"
      * Starsze wpisy: "8 października 2025"

3.  Czy po kliknięciu przycisku "Nowy Wpis" i przejściu do widoku edytora, kursor powinien automatycznie znaleźć się w polu tekstowym?

    **Odpowiedź:** Tak. W celu minimalizacji liczby kliknięć i natychmiastowego umożliwienia pisania, fokus powinien być automatycznie ustawiony na elemencie `<textarea>` zaraz po załadowaniu widoku `/entry/new`.

4.  Jak dokładnie powinna wyglądać walidacja w oknie modalnym usuwania konta, które wymaga podania hasła i frazy "usuń moje dane"?

    **Odpowiedź:** Przycisk "Usuń konto na stałe" powinien być domyślnie nieaktywny. Jego aktywacja powinna nastąpić dopiero wtedy, gdy oba warunki są spełnione: pole hasła nie jest puste, a treść drugiego pola tekstowego jest w 100% zgodna z wymaganą frazą "usuń moje dane" (sprawdzanie w czasie rzeczywistym).

5.  Jaka jest dokładna logika wyświetlania baneru zachęcającego do instalacji PWA i jak aplikacja ma "pamiętać" decyzję użytkownika?

    **Odpowiedź:** Baner powinien pojawić się po tym, jak użytkownik pomyślnie zapisze swój trzeci wpis. Jeśli użytkownik zamknie baner, w `Local Storage` przeglądarki należy zapisać flagę (np. `pwaPromptDismissed: true`). Aplikacja przed wyświetleniem baneru będzie sprawdzać obecność tej flagi co 3 wpisy, dzięki czemu prośba nie pojawi się ponownie za każdym razem.

6.  Co powinien widzieć użytkownik w miejscu wskaźnika "streak", zanim dane zostaną pobrane z API?

    **Odpowiedź:** Aby uniknąć "mrugania" interfejsu (wyświetlania placeholdera, a potem właściwej wartości), wskaźnik "streak" nie powinien być w ogóle renderowany, dopóki dane z API nie zostaną pomyślnie załadowane. Jeśli pobranie danych się nie powiedzie, wskaźnik po prostu się nie pojawi.

\</pytania\>



\<pytania\>

1.  Jak aplikacja powinna zachować się w sytuacji, gdy użytkownik otworzy ją w wielu kartach przeglądarki i w jednej z nich się wyloguje?

    **Odpowiedź:** W celu zapewnienia spójności i bezpieczeństwa, aplikacja powinna co jakiś czas (np. przy próbie nawigacji lub co minutę) sprawdzać w tle ważność tokena. Gdy wykryje, że sesja wygasła (użytkownik wylogował się w innej karcie), pozostałe otwarte karty powinny automatycznie przekierować użytkownika do strony logowania z komunikatem informacyjnym, np. "Sesja została zakończona".

2.  Jaki jest oczekiwany przepływ, gdy użytkownik zmieni swoje hasło na jednym urządzeniu, będąc jednocześnie zalogowanym na innym?

    **Odpowiedź:** Podobnie jak w przypadku wylogowania, zmiana hasła unieważnia wszystkie istniejące sesje. Gdy drugie urządzenie spróbuje wykonać jakąkolwiek operację wymagającą autoryzacji (np. auto-zapis), otrzyma od API błąd `401 Unauthorized`. Aplikacja powinna wtedy obsłużyć ten błąd, wylogowując użytkownika i przekierowując go na stronę logowania z komunikatem "Twoje hasło zostało zmienione. Zaloguj się ponownie."

3.  Czy istnieje potrzeba wizualnego odróżnienia wpisu powitalnego od wpisów stworzonych przez użytkownika na liście?

    **Odpowiedź:** Nie. Wpis powitalny powinien wyglądać i zachowywać się dokładnie tak samo jak każdy inny wpis. Pozwoli to użytkownikowi od razu nauczyć się, jak działają wpisy (edycja, usuwanie) na znanym przykładzie i wpisuje się w filozofię minimalizmu, unikając dodawania specjalnych, jednorazowych elementów UI.

4.  Jakie animacje lub przejścia między widokami (np. z listy do edytora) powinny zostać zaimplementowane, aby poprawić odczucia z użytkowania (UX)?

    **Odpowiedź:** Dla MVP, w celu zachowania prostoty i szybkości wdrożenia, nie należy implementować skomplikowanych animacji. Wystarczy subtelne, domyślne przejście typu "fade-in/fade-out" (płynne pojawianie się i znikanie) przy zmianie widoku. `Pico.css` wspiera takie delikatne przejścia, co zapewni przyjemne odczucia przy minimalnym wysiłku.

5.  Jakie dokładnie dane (np. temat, treść) powinien zawierać link `mailto:` w stopce do zbierania opinii?

    **Odpowiedź:** Link powinien mieć predefiniowany temat, aby ułatwić sortowanie opinii. Proponuję strukturę: `mailto:feedback@10xjournal.app?subject=Opinia%20o%2010xJournal%20MVP`. Pozostawienie treści maila pustej da użytkownikowi pełną swobodę w formułowaniu swoich myśli.

6.  Jak interfejs powinien zakomunikować użytkownikowi, że jego "streak" został przerwany i wyzerowany?

    **Odpowiedź:** Aplikacja nie powinna wyświetlać żadnego specjalnego, negatywnego komunikatu o przerwaniu serii, aby nie zniechęcać użytkownika. Po prostu, zgodnie z ustaleniami, wskaźnik "streak" (ikona ognia i liczba) zniknie z interfejsu, gdy jego wartość spadnie do zera. Użytkownik naturalnie zrozumie, że seria została przerwana, a jej ponowne pojawienie się po kolejnym wpisie będzie pozytywnym wzmocnieniem.

\</pytania\>




\<pytania\>

1.  Jak główna nawigacja aplikacji powinna adaptować się do większych ekranów (desktop)?

    **Odpowiedź:** Na ekranach o szerokości powyżej określonego progu (np. 768px), dolny pasek nawigacyjny, idealny dla urządzeń mobilnych, powinien przekształcić się w tradycyjny, minimalistyczny nagłówek na górze strony. Powinien on zawierać te same elementy: link do listy wpisów, przycisk "Nowy Wpis" oraz link do Ustawień wraz ze wskaźnikiem "streak".

2.  W jaki sposób powinny być wyświetlane błędy walidacji bezpośrednio w formularzach (rejestracja, logowanie, zmiana hasła)?

    **Odpowiedź:** Należy zaimplementować walidację po stronie klienta, aby dać użytkownikowi natychmiastową informację zwrotną. Komunikaty o błędach (np. "Nieprawidłowy format e-mail", "Hasła nie są takie same") powinny pojawiać się bezpośrednio pod polem, którego dotyczą, w kontrastującym kolorze. To podejście jest bardziej precyzyjne niż używanie globalnych powiadomień "toast" dla błędów formularzy.

3.  Jaką informację zwrotną powinien otrzymać użytkownik po kliknięciu przycisku "Eksportuj wszystkie wpisy"?

    **Odpowiedź:** Po kliknięciu, przycisk powinien zmienić stan na nieaktywny z tekstem "Przygotowuję plik...". Gdy frontend otrzyma dane z API i wygeneruje plik, powinno automatycznie uruchomić się okno pobierania w przeglądarce z sugerowaną nazwą pliku `Journal_export_RRRR-MM-DD.txt`. W razie niepowodzenia operacji, użytkownik powinien zostać poinformowany o błędzie za pomocą powiadomienia "toast".

4.  Czy w widoku edytora powinien znajdować się licznik słów lub znaków?

    **Odpowiedź:** Zdecydowanie nie. Zgodnie z kluczową zasadą minimalizmu i eliminacji rozpraszaczy, interfejs edytora nie powinien zawierać żadnych liczników. Ich brak wzmacnia cel aplikacji, którym jest skupienie się wyłącznie na pisaniu.

5.  Czy po pomyślnym usunięciu wpisu użytkownik powinien otrzymać dodatkowe wizualne potwierdzenie, np. powiadomienie "toast"?

    **Odpowiedź:** Nie. Po potwierdzeniu w oknie dialogowym, wpis powinien po prostu zniknąć z listy. Subtelna animacja usunięcia elementu (np. płynne "zniknięcie") będzie wystarczającym i eleganckim potwierdzeniem, które nie wprowadza zbędnego "szumu" do interfejsu.

6.  Co dokładnie powinna zawierać strona z instrukcjami manualnej instalacji PWA?

    **Odpowiedź:** Należy stworzyć prostą, statyczną podstronę w aplikacji (np. pod adresem `/install-guide`) zawierającą krótkie, punktowe instrukcje z ikonami dla najpopularniejszych platform: Chrome (Android/Desktop), Safari (iOS) oraz Edge. Przykładowa instrukcja: "Dla Safari na iPhonie: dotknij ikony Udostępnij, a następnie wybierz 'Do ekranu początkowego'".

\</pytania\>



\<pytania\>

1.  Co powinien zobaczyć niezalogowany użytkownik, odwiedzając główny adres aplikacji (`/`)?

    **Odpowiedź:** Należy stworzyć minimalistyczną stronę docelową (landing page), która w kilku zdaniach wyjaśnia filozofię 10xJournal (prostota, skupienie na pisaniu). Powinna zawierać nagłówek, krótki opis, zrzut ekranu przedstawiający czysty interfejs aplikacji oraz dwa wyraźne przyciski z wezwaniem do działania: "Zaloguj się" i "Załóż darmowe konto".

2.  Jaka jest strategia dotycząca motywu kolorystycznego (jasny/ciemny) dla wersji MVP? PRD wymienia tryb ciemny jako funkcję po MVP, ale `Pico.css` domyślnie go wspiera.

    **Odpowiedź:** Wykorzystajmy wbudowaną w `Pico.css` funkcjonalność automatycznego dostosowywania się do motywu systemu operacyjnego użytkownika. Nie dodawajmy ręcznego przełącznika w interfejsie. Takie podejście nie wymaga dodatkowej pracy, wpisuje się w minimalizm, a jednocześnie od razu dostarcza wartość użytkownikom preferującym tryb ciemny. Oficjalne wdrożenie przełącznika w UI pozostanie funkcją na później, zgodnie z PRD.

3.  Czy na stronie logowania powinna znajdować się opcja "Nie wylogowuj mnie" (Remember me)?

    **Odpowiedź:** Tak. Należy dodać standardowe pole wyboru (`checkbox`) "Nie wylogowuj mnie". Jest to oczekiwana funkcjonalność, która znacząco poprawia wygodę użytkowania. Supabase domyślnie obsługuje długotrwałe sesje, więc implementacja po stronie klienta jest prosta i nie narusza zasad MVP.

4.  Jaka konkretna czcionka powinna być używana w edytorze tekstu, aby zapewnić najlepszą czytelność i doświadczenie pisania?

    **Odpowiedź:** Dla głównego tekstu wpisów rekomenduję użycie nowoczesnej czcionki szeryfowej, np. `Lora` lub `Merriweather`, które są bardzo czytelne w dłuższych formach. Dla reszty interfejsu (przyciski, nawigacja) w zupełności wystarczy domyślna, bezszeryfowa czcionka systemowa, którą dostarcza `Pico.css`.

5.  Jaka strategia zostanie przyjęta dla ikon w aplikacji (np. ikona usuwania, ognia dla "streak")? Czy będziemy używać emoji, czy dedykowanej biblioteki ikon?

    **Odpowiedź:** Dla absolutnej prostoty i zerowego narzutu na wydajność w MVP, rekomenduję konsekwentne używanie wysokiej jakości, minimalistycznych emoji (np. 🗑️ dla usuwania, 🔥 dla serii). Są one natychmiastowo zrozumiałe i nie wymagają dołączania zewnętrznych bibliotek. Przejście na bibliotekę ikon SVG (np. Feather Icons) może być rozważone w przyszłości w ramach prac nad ujednoliceniem designu.

6.  Czy aplikacja powinna mieć dedykowaną stronę 404 (Not Found)?

    **Odpowiedź:** Tak. Należy stworzyć prostą, statyczną stronę 404, która będzie spójna wizualnie z resztą aplikacji. Powinna zawierać komunikat "Strona nie została znaleziona" oraz wyraźny link lub przycisk "Wróć do strony głównej", który przekierowuje zalogowanego użytkownika do listy wpisów, a niezalogowanego do strony docelowej.

\</pytania\>




\<pytania\>

1.  Jakie metadane (tytuł, opis, ikona) powinna posiadać aplikacja dla przeglądarek i mediów społecznościowych?

    **Odpowiedź:** Należy zdefiniować:
    * **Tytuł strony:** `<title>Journal - Twój minimalistyczny dziennik cyfrowy</title>`
    * **Opis:** `<meta name="description" content="Skup się na pisaniu bez rozpraszaczy. Journal to prosta i bezpieczna przestrzeń do budowania nawyku prowadzenia dziennika.">`
    * **Favicon:** Przygotować prostą, rozpoznawalną ikonę aplikacji (favicon), która będzie wyświetlana w karcie przeglądarki i po dodaniu do ekranu głównego jako PWA.
    * **Open Graph Tags:** Dodać podstawowe tagi OG (`og:title`, `og:description`, `og:image`) dla estetycznego wyglądu linku podczas udostępniania w mediach społecznościowych.

2.  Jak aplikacja ma się zachować, gdy użytkownik wklei sformatowany tekst (np. z Worda lub strony internetowej) do edytora?

    **Odpowiedź:** Interfejs powinien traktować każdy wklejony tekst jako czysty tekst (`plain text`). Wszelkie znaczniki HTML lub inne formatowanie muszą być ignorowane lub "oczyszczone" (stripped) w momencie wklejenia. Podczas wyświetlania treści wpisu, tekst powinien być zawsze renderowany z zabezpieczeniem przed interpretacją HTML (HTML escaping), aby zapobiec problemom z wyświetlaniem i zagrożeniom bezpieczeństwa (XSS).

3.  Jak powinien wyglądać stan przycisków akcji (np. "Zaloguj się", "Zapisz zmiany") w trakcie oczekiwania na odpowiedź z serwera?

    **Odpowiedź:** Po kliknięciu, przycisk powinien natychmiast przejść w stan nieaktywny (`disabled`) i wyświetlić wskaźnik ładowania (np. zamienić tekst na "Logowanie..." lub pokazać obok niego ikonę spinnera). Zapobiega to wielokrotnemu przesyłaniu tego samego żądania i daje użytkownikowi jasną informację zwrotną, że system przetwarza jego akcję.

4.  Jaka jest strategia buforowania (caching) zasobów aplikacji w kontekście PWA i zapewnienia aktualizacji?

    **Odpowiedź:** Service Worker wdrażający PWA powinien stosować strategię "stale-while-revalidate". Oznacza to, że użytkownik zawsze otrzymuje natychmiastowo wersję aplikacji z pamięci podręcznej (zapewniając szybkie ładowanie), a w tle sprawdzana jest dostępność nowszej wersji. Jeśli aktualizacja zostanie znaleziona, zostanie ona pobrana i zainstalowana, a użytkownik otrzyma ją przy następnej wizycie w aplikacji.

5.  Jaką informację zwrotną otrzymuje użytkownik po pomyślnym wykonaniu akcji, np. po zmianie hasła?

    **Odpowiedź:** Po pomyślnej zmianie hasła użytkownik jest przekierowywany na stronę logowania. Powinien tam zobaczyć dyskretne, ale jednoznaczne powiadomienie "toast" z komunikatem: "Hasło zostało pomyślnie zmienione". Podobne pozytywne potwierdzenia powinny być stosowane dla innych kluczowych, ale nieoczywistych akcji.

6.  W jaki sposób aplikacja powinna zarządzać tytułem dokumentu (`document.title`) w przeglądarce w zależności od aktualnego widoku?

    **Odpowiedź:** Tytuł w karcie przeglądarki powinien dynamicznie się zmieniać, aby odzwierciedlać aktualny kontekst. Na przykład:

      * Lista wpisów: "Twoje wpisy - 10xJournal"
      * Edycja wpisu: "Edycja wpisu... - 10xJournal"
      * Ustawienia: "Ustawienia - 10xJournal"
      * Strona logowania: "Zaloguj się - 10xJournal"