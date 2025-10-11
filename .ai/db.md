
### **1. Lista Tabel z Kolumnami, Typami Danych i Ograniczeniami**

#### **Tabela: `public.profiles`**
Przechowuje publiczne informacje o profilu użytkownika, połączone z tabelą `auth.users` z Supabase.

| Nazwa Kolumny | Typ Danych  | Ograniczenia                                               | Opis                                           |
| :------------- | :---------- | :--------------------------------------------------------- | :--------------------------------------------- |
| `id`           | `UUID`      | `PRIMARY KEY`, `REFERENCES auth.users(id) ON DELETE CASCADE` | Unikalny identyfikator użytkownika (z `auth.users`). |
| `created_at`   | `TIMESTAMPTZ` | `NOT NULL`, `DEFAULT now()`                                | Znacznik czasu utworzenia profilu.             |
| `updated_at`   | `TIMESTAMPTZ` | `NOT NULL`, `DEFAULT now()`                                | Znacznik czasu ostatniej aktualizacji profilu. |

---

#### **Tabela: `public.journal_entries`**
Zawiera wszystkie wpisy dziennika utworzone przez użytkowników.

| Nazwa Kolumny | Typ Danych  | Ograniczenia                                           | Opis                                            |
| :------------- | :---------- | :----------------------------------------------------- | :---------------------------------------------- |
| `id`           | `UUID`      | `PRIMARY KEY`, `DEFAULT gen_random_uuid()`             | Unikalny identyfikator wpisu.                   |
| `user_id`      | `UUID`      | `NOT NULL`, `REFERENCES public.profiles(id) ON DELETE CASCADE` | Identyfikator użytkownika, który jest autorem wpisu. |
| `content`      | `TEXT`      | `NOT NULL`                                             | Treść wpisu w dzienniku.                        |
| `created_at`   | `TIMESTAMPTZ` | `NOT NULL`, `DEFAULT now()`                            | Znacznik czasu utworzenia wpisu.                |
| `updated_at`   | `TIMESTAMPTZ` | `NOT NULL`, `DEFAULT now()`                            | Znacznik czasu ostatniej aktualizacji wpisu.    |

---

#### **Tabela: `public.user_streaks`**
Śledzi nawyk regularnego pisania (tzw. "streak") dla każdego użytkownika.

| Nazwa Kolumny   | Typ Danych | Ograniczenia                                           | Opis                                                       |
| :-------------- | :--------- | :----------------------------------------------------- | :--------------------------------------------------------- |
| `user_id`       | `UUID`     | `PRIMARY KEY`, `REFERENCES public.profiles(id) ON DELETE CASCADE` | Identyfikator użytkownika, którego dotyczy rekord.         |
| `current_streak`  | `INTEGER`  | `NOT NULL`, `DEFAULT 0`                                | Liczba kolejnych dni z co najmniej jednym wpisem.          |
| `longest_streak`  | `INTEGER`  | `NOT NULL`, `DEFAULT 0`                                | Najdłuższa historyczna seria kolejnych dni z wpisami.      |
| `last_entry_date` | `DATE`     | `NULLABLE`                                             | Data ostatniego wpisu, który został wliczony do "streaka". |

---

### **2. Relacje Między Tabelami**

* **`auth.users` -> `public.profiles`**: Relacja **jeden-do-jednego**. Każdy użytkownik w systemie autentykacji ma dokładnie jeden profil w aplikacji. Klucz `profiles.id` jest kluczem obcym wskazującym na `auth.users.id`.
* **`public.profiles` -> `public.journal_entries`**: Relacja **jeden-do-wielu**. Każdy profil użytkownika może mieć wiele wpisów w dzienniku, ale każdy wpis należy tylko do jednego użytkownika.
* **`public.profiles` -> `public.user_streaks`**: Relacja **jeden-do-jednego**. Każdy profil użytkownika ma dokładnie jeden rekord śledzący jego postępy w regularnym pisaniu.

---

### **3. Indeksy**

* **Indeks dla `journal_entries`**: W celu optymalizacji wydajności zapytań o listę wpisów dla danego użytkownika (najczęstsza operacja), posortowaną od najnowszych do najstarszych, zostanie utworzony indeks złożony.
    ```sql
    CREATE INDEX idx_journal_entries_user_id_created_at 
    ON public.journal_entries (user_id, created_at DESC);
    ```

---

### **4. Zasady PostgreSQL (Row Level Security)**

Wszystkie poniższe tabele będą miały włączone zabezpieczenia na poziomie wiersza (`ENABLE ROW LEVEL SECURITY`).

#### **Zasady dla `public.profiles`**:
* **Dostęp do odczytu**: Użytkownicy mogą odczytywać (`SELECT`) wyłącznie swój własny profil.
    ```sql
    CREATE POLICY "Users can view their own profile."
    ON public.profiles FOR SELECT
    USING (auth.uid() = id);
    ```

#### **Zasady dla `public.journal_entries`**:
* **Pełny dostęp (CRUD)**: Użytkownicy mają pełny dostęp (SELECT, INSERT, UPDATE, DELETE) wyłącznie do swoich własnych wpisów w dzienniku.
    ```sql
    CREATE POLICY "Users can CRUD their own journal entries."
    ON public.journal_entries FOR ALL
    USING (auth.uid() = user_id);
    ```

#### **Zasady dla `public.user_streaks`**:
* **Dostęp do odczytu**: Użytkownicy mogą odczytywać (`SELECT`) wyłącznie swój własny rekord "streaka". Modyfikacje tej tabeli będą wykonywane wyłącznie przez funkcje bazodanowe z uprawnieniami `SECURITY DEFINER`.
    ```sql
    CREATE POLICY "Users can view their own streaks."
    ON public.user_streaks FOR SELECT
    USING (auth.uid() = user_id);
    ```

---

### **5. Dodatkowe Uwagi i Decyzje Projektowe**

* **Automatyzacja za pomocą wyzwalaczy (Triggers)**: Kluczowe procesy biznesowe zostaną zaimplementowane bezpośrednio w bazie danych, aby zapewnić spójność i odciążyć aplikację kliencką:
    * **Onboarding użytkownika**: Wyzwalacz na tabeli `auth.users` po operacji `INSERT` automatycznie utworzy powiązane rekordy w `public.profiles` i `public.user_streaks`.
    * **Wpis powitalny**: Wyzwalacz na tabeli `public.profiles` po operacji `INSERT` automatycznie utworzy pierwszy, powitalny wpis w `journal_entries`.
    * **Aktualizacja "Streaka"**: Wyzwalacz na tabeli `journal_entries` po operacji `INSERT` uruchomi funkcję, która zaktualizuje `current_streak`, `longest_streak` i `last_entry_date` w tabeli `user_streaks`.
    * **Znacznik `updated_at`**: Generyczny wyzwalacz będzie automatycznie aktualizował pole `updated_at` przy każdej operacji `UPDATE` na tabelach `profiles` i `journal_entries`.
* **Integralność danych**: Zastosowanie polityki `ON DELETE CASCADE` na wszystkich kluczach obcych zapewnia, że usunięcie konta użytkownika z systemu `auth.users` spowoduje automatyczne i spójne usunięcie wszystkich powiązanych z nim danych (profilu, wpisów i rekordu "streaka").
* **Typ `TEXT` bez limitu**: Zgodnie z decyzją, pole `content` w `journal_entries` jest typu `TEXT` bez sztucznych ograniczeń długości, co wpisuje się w filozofię prostoty i wolności, jaką ma oferować aplikacja.

---

### **6. Schemat Bazy Danych (Diagram ASCII)**

Poniższy diagram przedstawia wizualną reprezentację relacji między tabelami.

````

(Supabase Auth)
\+--------------+
|  auth.users  |
\+--------------+
| id (PK)      |
| ...          |
\+--------------+
|
| 1..1
|
\+--------------+      1..1      +-----------------+
|   profiles   |----------------|   user\_streaks  |
\+--------------+                +-----------------+
| id (PK, FK)  |                | user\_id (PK, FK)|
| created\_at   |                | current\_streak  |
| updated\_at   |                | longest\_streak  |
\+--------------+                | last\_entry\_date |
|                        +-----------------+
| 1..\*
|
\+-------------------+
|  journal\_entries  |
\+-------------------+
| id (PK)           |
| user\_id (FK)      |
| content           |
| created\_at        |
| updated\_at        |
\+-------------------+

```

