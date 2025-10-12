# Architektura UI dla 10xJournal

## 1. Przegląd struktury UI

Architektura interfejsu użytkownika dla 10xJournal została zaprojektowana w oparciu o filozofię **minimalizmu, prostoty i pełnego skupienia na pisaniu**. Zbudowana w technologii **Blazor WebAssembly** i stylizowana za pomocą lekkiej biblioteki **Pico.css**, aplikacja jest w pełni responsywna, z podejściem **mobile-first**.

Kluczowe założenia architektoniczne:
* **Adaptacyjna nawigacja:** Interfejs wykorzystuje dolny pasek nawigacyjny na urządzeniach mobilnych (`<768px`), który płynnie przechodzi w klasyczny górny nagłówek na ekranach desktopowych.
* **Zunifikowany layout:** Wszystkie widoki opierają się na prostym, jednokolumnowym i wyśrodkowanym układzie, co zapewnia spójność i eliminuje rozpraszacze.
* **Zarządzanie stanem:** Stan uwierzytelnienia jest zarządzany centralnie przez `AuthenticationStateProvider` Blazora. Inne dane globalne, jak licznik "streak", są przechowywane w prostej, wstrzykiwanej usłudze (singleton), aby zapewnić do nich łatwy dostęp w całej aplikacji.
* **Obsługa offline:** Aplikacja jest zaprojektowana jako **Progressive Web App (PWA)**. Kluczowe zasoby są buforowane, a niezapisane zmiany w edytorze są automatycznie zapisywane w `Local Storage` w przypadku utraty połączenia, co chroni pracę użytkownika.
* **Bezpośrednia integracja z API:** Frontend komunikuje się bezpośrednio z backendem **Supabase**, wykorzystując jego punkty końcowe REST i RPC do wszystkich operacji na danych. Logika biznesowa (np. obliczanie "streak") jest hermetyzowana w bazie danych, co upraszcza klienta.

---

## 2. Lista widoków

### Widoki Publiczne (Dostępne dla niezalogowanych użytkowników)

* **Nazwa widoku:** Strona Docelowa (Landing Page)
    * **Ścieżka:** `/`
    * **Główny cel:** Przedstawienie wartości aplikacji i zachęcenie do rejestracji lub logowania.
    * **Kluczowe informacje:** Krótki opis 10xJournal, jego główne zalety (prostota, bezpieczeństwo), przyciski wezwania do działania.
    * **Kluczowe komponenty:** Nagłówek, sekcja "hero" z hasłem przewodnim, przyciski "Zaloguj się" i "Załóż konto", prosta stopka z linkiem do opinii.
    * **UX, dostępność i bezpieczeństwo:** Widok jest czysto informacyjny. Wszystkie elementy interaktywne mają wyraźny stan `:focus`.

* **Nazwa widoku:** Logowanie
    * **Ścieżka:** `/login`
    * **Główny cel:** Umożliwienie zarejestrowanym użytkownikom dostępu do ich kont.
    * **Kluczowe informacje:** Pola na e-mail i hasło, link do resetowania hasła.
    * **Kluczowe komponenty:** Formularz logowania, pole e-mail, pole hasła, przycisk "Zaloguj się", link "Nie pamiętam hasła".
    * **UX, dostępność i bezpieczeństwo:** Walidacja formularza po stronie klienta i serwera. Komunikaty o błędach wyświetlane pod odpowiednimi polami. Przycisk jest dezaktywowany podczas przetwarzania żądania.

* **Nazwa widoku:** Rejestracja
    * **Ścieżka:** `/register`
    * **Główny cel:** Umożliwienie nowym użytkownikom założenia konta.
    * **Kluczowe informacje:** Pola na e-mail i hasło, informacja o wymaganiach dotyczących hasła.
    * **Kluczowe komponenty:** Formularz rejestracji z polami na e-mail i hasło (wraz z jego potwierdzeniem), przycisk "Załóż konto".
    * **UX, dostępność i bezpieczeństwo:** Logika identyczna jak w widoku logowania. Jasne wskazówki dotyczące siły hasła.

* **Nazwa widoku:** Resetowanie Hasła
    * **Ścieżka:** `/reset-password`
    * **Główny cel:** Umożliwienie użytkownikom odzyskania dostępu do konta.
    * **Kluczowe informacje:** Pole na adres e-mail, instrukcje dotyczące dalszych kroków.
    * **Kluczowe komponenty:** Formularz z jednym polem na e-mail, przycisk "Wyślij link do resetu".
    * **UX, dostępność i bezpieczeństwo:** Wyraźny komunikat o powodzeniu lub błędzie po wysłaniu formularza.

### Widoki Prywatne (Dostępne po zalogowaniu)

* **Nazwa widoku:** Lista Wpisów
    * **Ścieżka:** `/app/journal` (domyślny widok po zalogowaniu)
    * **Główny cel:** Wyświetlanie wszystkich wpisów użytkownika i umożliwienie nawigacji do tworzenia nowego wpisu.
    * **Kluczowe informacje:** Lista wpisów w porządku odwrotnie chronologicznym, data utworzenia każdego wpisu, pierwsza linia wpisu jako jego tytuł.
    * **Kluczowe komponenty:**
        * Lista wpisów (komponent `EntryListItem`).
        * Przycisk "Nowy wpis".
        * Wskaźnik "Streak" (🔥 + liczba), widoczny tylko gdy > 0.
        * Wskaźnik stanu ładowania (`Skeleton UI`) podczas pobierania danych.
        * Komunikat dla stanu pustego (gdy użytkownik nie ma jeszcze wpisów).
    * **UX, dostępność i bezpieczeństwo:** Każdy element listy jest linkiem do edytora. Tytuł jest generowany z pierwszej linii (do 100 znaków) i renderowany jako nagłówek `<h3>` dla lepszej semantyki.

* **Nazwa widoku:** Edytor Wpisu
    * **Ścieżka:** `/app/entry/new` (tworzenie), `/app/entry/{id}` (edycja)
    * **Główny cel:** Zapewnienie wolnej od rozpraszaczy przestrzeni do pisania i edytowania wpisów.
    * **Kluczowe informacje:** Pole tekstowe do wprowadzania treści, status zapisu.
    * **Kluczowe komponenty:**
        * Główny element `<textarea>`.
        * Przycisk/link "Wróć" do listy wpisów.
        * Przycisk "Usuń" (widoczny tylko w trybie edycji).
        * Wskaźnik statusu auto-zapisu (np. "Zapisywanie...", "Zapisano", "Błąd").
    * **UX, dostępność i bezpieczeństwo:**
        * Przy tworzeniu nowego wpisu, fokus jest automatycznie ustawiany na polu tekstowym.
        * Zmiany są zapisywane automatycznie (z `debouncingiem`) po krótkiej przerwie w pisaniu.
        * Treść wprowadzana przez użytkownika jest traktowana jako czysty tekst, a przy wyświetlaniu zabezpieczana przed atakami XSS.
        * Usunięcie wpisu wymaga potwierdzenia przez natywne okno `window.confirm()`.

* **Nazwa widoku:** Ustawienia
    * **Ścieżka:** `/app/settings`
    * **Główny cel:** Umożliwienie użytkownikowi zarządzania swoim kontem.
    * **Kluczowe informacje:** Opcje zmiany hasła i usunięcia konta.
    * **Kluczowe komponenty:**
        * Formularz zmiany hasła.
        * Sekcja usuwania konta z przyciskiem inicjującym procedurę.
        * Przycisk "Wyloguj".
        * Okno modalne do potwierdzenia usunięcia konta.
    * **UX, dostępność i bezpieczeństwo:**
        * Proces usuwania konta jest wieloetapowy: wymaga eksportu danych, wpisania hasła oraz frazy "usuń moje dane" w celu aktywacji przycisku ostatecznego usunięcia.
        * Zmiana hasła unieważnia wszystkie inne aktywne sesje.

---

## 3. Mapa podróży użytkownika

**Główny przypadek użycia: Od rejestracji do stworzenia i edycji pierwszego wpisu.**

1.  **Lądowanie:** Użytkownik trafia na **Stronę Docelową (`/`)**, gdzie zapoznaje się z aplikacją.
2.  **Rejestracja:** Klika "Załóż konto", co przenosi go do **Widoku Rejestracji (`/register`)**. Wypełnia i wysyła formularz.
3.  **Onboarding:** Po potwierdzeniu adresu e-mail (jeśli wymagane), użytkownik jest automatycznie logowany i przekierowywany do **Listy Wpisów (`/app/journal`)**. Widzi tam pierwszy, automatycznie wygenerowany wpis powitalny.
4.  **Tworzenie Wpisu:** Klika przycisk "Nowy wpis". Następuje nawigacja do **Widoku Edytora (`/app/entry/new`)**. Kursor automatycznie ustawia się w polu tekstowym.
5.  **Pisanie:** Użytkownik wprowadza treść. Po chwili bezczynności, mechanizm auto-zapisu wysyła dane na serwer, a **wskaźnik statusu** informuje o pomyślnym zapisaniu.
6.  **Powrót do Listy:** Użytkownik klika "Wróć", wracając na **Listę Wpisów**. Nowy wpis jest widoczny na samej górze. Licznik **"Streak"** (🔥 1) pojawia się w interfejsie.
7.  **Edycja Wpisu:** Użytkownik klika na właśnie utworzony wpis. Przechodzi ponownie do **Widoku Edytora**, tym razem pod adresem `/app/entry/{id}`, gdzie może kontynuować edycję. Zmiany są ponownie zapisywane automatycznie.
8.  **Obsługa trybu offline:** Jeśli podczas pisania użytkownik straci połączenie z internetem, **globalny wskaźnik offline** staje się widoczny. Wpisywana treść jest buforowana w `Local Storage`. Po odzyskaniu połączenia, dane są automatycznie synchronizowane z serwerem.

---

## 4. Układ i struktura nawigacji

**Nawigacja jest adaptacyjna i zależy od szerokości ekranu:**

* **Urządzenia mobilne (szerokość < 768px):**
    * **Stały dolny pasek nawigacyjny** zawierający dwie główne ikony (emoji):
        * **Dziennik (📖):** Link do **Listy Wpisów** (`/app/journal`).
        * **Ustawienia (⚙️):** Link do **Widoku Ustawień** (`/app/settings`).
    * Główny przycisk akcji "Nowy wpis" jest umieszczony na **Liście Wpisów**.

* **Urządzenia desktopowe (szerokość >= 768px):**
    * **Górny pasek nagłówka (header)** zastępuje dolną nawigację i zawiera:
        * Logo/nazwę aplikacji po lewej stronie (link do `/app/journal`).
        * Wskaźnik "Streak" pośrodku/po prawej.
        * Linki nawigacyjne: "Dziennik", "Ustawienia".
        * Przycisk "Nowy wpis".
        * Przycisk "Wyloguj".

Taka struktura zapewnia ergonomię na urządzeniach mobilnych i efektywne wykorzystanie przestrzeni na większych ekranach.

---

## 5. Kluczowe komponenty

Poniższe komponenty są reużywalne i stanowią fundament interfejsu aplikacji:

* **`MainLayout` (Główny Układ):**
    * Odpowiada za renderowanie adaptacyjnej nawigacji (dolny pasek vs. górny nagłówek).
    * Zawiera globalny wskaźnik trybu offline oraz komponent do wyświetlania powiadomień "toast".
    * Pobiera i wyświetla wskaźnik "Streak".

* **`EntryListItem` (Element Listy Wpisów):**
    * Pojedynczy wiersz na **Liście Wpisów**.
    * Wyświetla datę utworzenia i pierwszą linię tekstu jako tytuł.
    * Jest w całości klikalnym linkiem prowadzącym do edytora danego wpisu.

* **`AuthForm` (Formularz Autoryzacji):**
    * Generyczny komponent używany w widokach **Logowania** i **Rejestracji**.
    * Zawiera logikę walidacji pól, obsługę stanu ładowania i wyświetlanie komunikatów o błędach.

* **`AutoSaveStatusIndicator` (Wskaźnik Statusu Auto-zapisu):**
    * Mały, dyskretny element tekstowy pod edytorem w **Widoku Edytora**.
    * Informuje użytkownika o bieżącym stanie zapisu (np. "Piszesz...", "Zapisano", "Błąd zapisu"), dając poczucie bezpieczeństwa.

* **`DeleteAccountModal` (Okno Modalne Usuwania Konta):**
    * Komponent modalny aktywowany w **Widoku Ustawień**.
    * Prowadzi użytkownika przez bezpieczny, wieloetapowy proces usuwania konta, wymuszając potwierdzenie i eksport danych.

* **`ToastNotification` (Powiadomienie Toast):**
    * Globalny komponent do wyświetlania krótkich, samo znikających powiadomień o błędach serwera lub innych zdarzeniach w aplikacji.