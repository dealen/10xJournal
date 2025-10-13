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

#### **Moduł: Strona Publiczna (dla Gości)**

1.  **Jako odwiedzający stronę,** chcę na stronie głównej (`/`) zrozumieć, czym jest aplikacja i jakie problemy rozwiązuje, aby podjąć świadomą decyzję o założeniu konta.
2.  **Jako odwiedzający stronę,** chcę łatwo znaleźć i przejść do strony logowania lub rejestracji, aby bez przeszkód rozpocząć korzystanie z aplikacji.
3.  **Jako odwiedzający stronę,** chcę zobaczyć prosty i estetyczny interfejs strony głównej, aby mieć pozytywne pierwsze wrażenie o aplikacji.

#### **Moduł: Zarządzanie Kontem**

4. **Jako nowy użytkownik,** chcę móc zarejestrować się w aplikacji przy użyciu mojego adresu e-mail i hasła, aby stworzyć swoje prywatne konto.
    
5. **Jako użytkownik próbujący się zarejestrować,** chcę zobaczyć zrozumiały komunikat o błędzie, jeśli mój e-mail jest już zajęty lub hasło jest zbyt słabe, aby móc poprawić dane.
    
6. **Jako zarejestrowany użytkownik,** chcę móc zalogować się do aplikacji za pomocą mojego e-maila i hasła, aby uzyskać dostęp do mojego dziennika.
    
7. **Jako użytkownik próbujący się zalogować,** chcę zobaczyć komunikat o błędzie, jeśli podałem nieprawidłowe dane, abym wiedział, że muszę spróbować ponownie.
    
8. **Jako zalogowany użytkownik,** chcę mieć możliwość wylogowania się z aplikacji, aby zabezpieczyć dostęp do mojego konta na współdzielonym urządzeniu.
    
9. **Jako użytkownik, który zapomniał hasła,** chcę móc skorzystać z funkcji "resetuj hasło", aby odzyskać dostęp do mojego konta.
    
10. **Jako zalogowany użytkownik,** chcę mieć dostęp do strony "Ustawienia", gdzie mogę zmienić swoje hasło, aby dbać o bezpieczeństwo konta.
    

#### **Moduł: Zarządzanie Wpisami**

11. **Jako nowy użytkownik,** po pierwszym zalogowaniu chcę zobaczyć wpis powitalny, aby zrozumieć, jak działa aplikacja i od czego zacząć.
    
12. **Jako zalogowany użytkownik,** chcę widzieć listę wszystkich moich wpisów w porządku od najnowszego do najstarszego, aby mieć szybki przegląd swojej historii.
    
13. **Jako zalogowany użytkownik,** chcę móc kliknąć przycisk "Nowy wpis", aby otworzyć pusty edytor i zacząć pisać.
    
14. **Jako użytkownik piszący nowy wpis,** chcę, aby po zapisaniu wpis pojawił się na górze listy, abym miał pewność, że został dodany.
  
15. **Jako użytkownik przeglądający listę wpisów,** chcę móc kliknąć na dowolny wpis, aby otworzyć go do odczytu i edycji.
    
16. **Jako użytkownik edytujący istniejący wpis,** chcę, aby moje zmiany zostały zapisane, kiedy zakończę edycję.
    
17. **Jako użytkownik,** chcę móc usunąć wybrany wpis, ale najpierw chcę zobaczyć okno z prośbą o potwierdzenie, aby uniknąć przypadkowego skasowania danych.
    

#### **Moduł: Doświadczenie Użytkownika i Wymagania Niefunkcjonalne**

18. **Jako użytkownik piszący długi tekst,** chcę, aby aplikacja zapisywała moją pracę w tle w razie utraty połączenia internetowego, abym nie stracił/a niezapisanych fragmentów.
    
19. **Jako użytkownik,** chcę, aby interfejs aplikacji był czytelny i funkcjonalny na moim telefonie komórkowym, abym mógł prowadzić dziennik z dowolnego miejsca.
    
20. **Jako świadomy użytkownik,** chcę mieć możliwość trwałego usunięcia mojego konta i wszystkich moich danych, a przed tym krokiem chcę otrzymać propozycję ich eksportu, aby zachować swoją historię.