# Plan implementacji widoku Strona Docelowa (Landing Page)

## 1. Przegląd
Celem tego widoku jest stworzenie strony głównej (`Landing Page`) dla aplikacji 10xJournal. Strona ma za zadanie w zwięzły sposób przedstawić kluczowe wartości aplikacji – prostotę i bezpieczeństwo – oraz zachęcić nowych użytkowników do rejestracji, a powracających do logowania. Widok jest w pełni statyczny i nie pobiera żadnych danych z API.

## 2. Routing widoku
Widok będzie dostępny pod główną ścieżką aplikacji:
- **Ścieżka:** `/`

## 3. Struktura komponentów
Hierarchia komponentów dla widoku Strony Docelowej będzie następująca:

```

/Pages/Public/LandingPage.razor (@page "/")
├── /Shared/Public/PublicHeader.razor
├── /Features/Landing/HeroSection.razor
└── /Shared/Public/PublicFooter.razor

```

- **`LandingPage.razor`**: Główny komponent strony, który agreguje pozostałe elementy w spójną całość.
- **`PublicHeader.razor`**: Komponent nagłówka wyświetlany na stronach publicznych.
- **`HeroSection.razor`**: Centralna część strony zawierająca hasło przewodnie i wezwania do działania (CTA).
- **`PublicFooter.razor`**: Komponent stopki z podstawowymi informacjami.

## 4. Szczegóły komponentów
### `LandingPage.razor`
- **Opis komponentu**: Jest to główny komponent routingu dla ścieżki `/`. Jego jedynym zadaniem jest złożenie nagłówka, sekcji "hero" oraz stopki w jeden spójny widok.
- **Główne elementy**: `PublicHeader`, `HeroSection`, `PublicFooter`.
- **Obsługiwane interakcje**: Brak.
- **Obsługiwana walidacja**: Brak.
- **Typy**: Brak.
- **Propsy**: Brak.

### `PublicHeader.razor`
- **Opis komponentu**: Prosty nagłówek zawierający nazwę aplikacji "10xJournal". Służy do budowania spójnej tożsamości wizualnej na stronach publicznych.
- **Główne elementy**: Element `<nav>` zawierający `<a>` z nazwą aplikacji.
- **Obsługiwane interakcje**:
    - Kliknięcie na nazwę aplikacji (opcjonalnie, może odświeżać stronę).
- **Obsługiwana walidacja**: Brak.
- **Typy**: Brak.
- **Propsy**: Brak.

### `HeroSection.razor`
- **Opis komponentu**: Najważniejsza sekcja strony, która ma przyciągnąć uwagę użytkownika. Zawiera główne hasło marketingowe, krótki opis korzyści oraz dwa główne przyciski akcji.
- **Główne elementy**:
    - `<h1>`: Główne hasło (np. "Twoje myśli, prosto i bezpiecznie.").
    - `<p>`: Krótki tekst opisujący zalety aplikacji (prostota, bezpieczeństwo).
    - Dwa elementy `<button>`: "Zaloguj się" i "Załóż konto".
- **Obsługiwane interakcje**:
    - Kliknięcie przycisku "Zaloguj się".
    - Kliknięcie przycisku "Załóż konto".
- **Obsługiwana walidacja**: Brak.
- **Typy**: Brak.
- **Propsy**: Brak.

### `PublicFooter.razor`
- **Opis komponentu**: Prosta stopka zamykająca stronę. Zawiera informacje o prawach autorskich oraz link do strony z opiniami/informacjami zwrotnymi.
- **Główne elementy**: Element `<footer>`, tekst z rokiem i nazwą aplikacji, link `<a>` do strony opinii.
- **Obsługiwane interakcje**:
    - Kliknięcie linku do strony opinii.
- **Obsługiwana walidacja**: Brak.
- **Typy**: Brak.
- **Propsy**: Brak.

## 5. Typy
Implementacja tego widoku jest czysto prezentacyjna i **nie wymaga definiowania żadnych nowych typów danych**, takich jak DTO (Data Transfer Object) czy ViewModel.

## 6. Zarządzanie stanem
Widok jest **bezstanowy**. Nie ma potrzeby zarządzania lokalnym stanem komponentów. Do obsługi nawigacji po kliknięciu przycisków CTA zostanie wykorzystany wbudowany w Blazor serwis `NavigationManager`, wstrzyknięty za pomocą dyrektywy `@inject`.

## 7. Integracja API
Ten widok **nie integruje się z żadnym endpointem API**. Cała zawartość jest statyczna i zdefiniowana bezpośrednio w komponentach.

## 8. Interakcje użytkownika
- **Użytkownik odwiedza stronę `/`**:
    - **Wynik**: Wyświetlana jest Strona Docelowa z nagłówkiem, sekcją "hero" i stopką.
- **Użytkownik klika przycisk "Zaloguj się"**:
    - **Wynik**: Aplikacja przekierowuje użytkownika na stronę logowania (np. `/login`).
- **Użytkownik klika przycisk "Załóż konto"**:
    - **Wynik**: Aplikacja przekierowuje użytkownika na stronę rejestracji (np. `/register`).
- **Użytkownik używa klawisza Tab do nawigacji**:
    - **Wynik**: Aktywne elementy (przyciski, linki) otrzymują wyraźny wizualnie stan `:focus`, zgodnie z wymogami dostępności i domyślnym stylem Pico.css.

## 9. Warunki i walidacja
W tym widoku **nie występują żadne warunki ani pola do walidacji**, ponieważ nie zawiera on żadnych formularzy ani danych wejściowych od użytkownika.

## 10. Obsługa błędów
Ponieważ widok jest statyczny, ryzyko wystąpienia błędów jest minimalne. Potencjalne problemy mogą dotyczyć całej aplikacji, a nie samego widoku:
- **Błąd ładowania aplikacji (WASM)**: Obsługiwany globalnie przez mechanizmy Blazora (np. ekran ładowania zdefiniowany w `index.html`).
- **Błędna nawigacja (nieistniejąca strona)**: Obsługiwana przez router Blazora, który powinien wyświetlić standardowy komponent `NotFound`. Należy upewnić się, że ścieżki nawigacji (`/login`, `/register`) są poprawnie zdefiniowane w aplikacji.

## 11. Kroki implementacji
1.  **Struktura plików**: Utwórz następującą strukturę katalogów i plików w projekcie Blazor:
    - `Pages/Public/LandingPage.razor`
    - `Shared/Public/PublicHeader.razor`
    - `Shared/Public/PublicFooter.razor`
    - `Features/Landing/HeroSection.razor`
2.  **Implementacja `PublicHeader.razor`**: Stwórz prosty nagłówek z nazwą aplikacji wewnątrz elementu `<nav>`.
3.  **Implementacja `PublicFooter.razor`**: Stwórz prostą stopkę z informacją o prawach autorskich i linkiem.
4.  **Implementacja `HeroSection.razor`**:
    - Dodaj nagłówek `<h1>`, paragraf `<p>` z treścią marketingową.
    - Dodaj dwa przyciski: "Zaloguj się" i "Załóż konto".
    - Wstrzyknij `NavigationManager` (`@inject NavigationManager Navigation`).
    - Utwórz metody `OnClick` dla przycisków, które będą wywoływać `Navigation.NavigateTo("/login")` i `Navigation.NavigateTo("/register")`.
5.  **Implementacja `LandingPage.razor`**:
    - Dodaj dyrektywę routingu na górze pliku: `@page "/"`.
    - Złóż widok, umieszczając w nim po kolei komponenty: `<PublicHeader />`, `<main class="container"><HeroSection /></main>`, `<PublicFooter />`. Użycie `<main>` i `div.container` zapewni odpowiednie marginesy i semantykę.
6.  **Stylowanie**: Upewnij się, że projekt ma zaimportowany plik `pico.css`. Wykorzystaj semantyczne tagi HTML, aby Pico.css automatycznie nadał im odpowiedni wygląd.
7.  **Testowanie**: Sprawdź poprawność wyświetlania strony na różnych szerokościach ekranu (desktop, tablet, mobile) oraz przetestuj działanie przycisków nawigacyjnych i dostępność (nawigacja klawiaturą).