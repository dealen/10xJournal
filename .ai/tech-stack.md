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