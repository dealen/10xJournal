# 10xJournal

*A minimalist, distraction-free digital journal for those who just want to write.*

---

## Table of Contents

1.  [Project Description](#project-description)
2.  [Tech Stack](#tech-stack)
3.  [Getting Started Locally](#getting-started-locally)
4.  [Available Scripts](#available-scripts)
5.  [Project Scope (MVP)](#project-scope-mvp)
6.  [Project Status](#project-status)
7.  [License](#license)

---

## Project Description

**10xJournal** is a web application designed to be a simple, secure, and private space for regular writing. It addresses the problem of modern note-taking applications that are often bloated with excessive features (complex formatting, databases, templates), creating a barrier to the simple act of writing.

Our solution is to eliminate all unnecessary distractions and focus on the absolute fundamentals. The interface is clean, intuitive, and puts your content first, encouraging the habit of consistent journaling.

**Target Audience:** Users who feel overwhelmed by the complexity of tools like Notion or Obsidian and are looking for a dedicated, straightforward journaling experience.

---

## Tech Stack

The project is built with a modern, scalable, and maintainable tech stack.

| Category          | Technology / Service                                                              | Purpose                                                 |
| ----------------- | --------------------------------------------------------------------------------- | ------------------------------------------------------- |
| **Frontend**      | [Blazor WebAssembly](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)      | Building a rich, interactive client-side UI with C#     |
|                   | [Pico.css](https://picocss.com/)                                                  | Ultra-lightweight, classless CSS for minimalist styling |
| **Backend**       | [Supabase](https://supabase.io/)                                                  | Backend-as-a-Service (BaaS)                             |
|                   | [PostgreSQL](https://www.postgresql.org/)                                         | Database for storing journal entries                    |
|                   | Supabase Auth                                                                     | Secure user authentication and management               |
| **CI/CD & Hosting** | [GitHub Actions](https://github.com/features/actions)                             | Automated build, test, and deployment workflows         |
|                   | [Azure Static Web Apps](https://azure.microsoft.com/en-us/services/app-service/static/) | Hosting, global CDN, and SSL                            |
| **Monitoring**    | [Serilog](https://serilog.net/)                                                   | Structured logging for .NET                             |

---

## Getting Started Locally

To run the project on your local machine, follow these steps.

### Prerequisites

*   [.NET 8 SDK (or newer)](https://dotnet.microsoft.com/download)
*   A free [Supabase Account](https://app.supabase.io/)

### Installation & Setup

1.  **Clone the repository:**
    ```sh
    git clone https://github.com/dealen/10xJournal.git
    cd 10xJournal
    ```

2.  **Set up Supabase:**
    *   Create a new project in your Supabase dashboard.
    *   Go to `Project Settings` > `API`.
    *   Find your `Project URL` and `Project API keys` (the `anon` `public` key).

3.  **Configure your environment variables:**
    *   In the `10xJournal.Client` project, create a file named `appsettings.Development.json` if it doesn't exist.
    *   Add your Supabase credentials to it:
        ```json
        {
          "Supabase": {
            "Url": "YOUR_SUPABASE_URL",
            "AnonKey": "YOUR_SUPABASE_ANON_KEY"
          }
        }
        ```

4.  **Run the application:**
    ```sh
    dotnet run --project 10xJournal.Client
    ```
    The application should now be running on `http://localhost:5000` (or another port specified in the console).

### Supabase Configuration Checklist

To guarantee the Blazor client and Supabase stay in sync across environments, apply the following settings after creating your project:

1.  **Auth → Sign In / Providers**
    *   Enable **Allow new users to sign up**.
    *   Disable **Allow anonymous sign-ins**.
    *   Enable **Confirm email** so Supabase emails activation links automatically.
    *   (Optional) Keep **Allow manual linking** off unless you plan multi-provider accounts.
2.  **Auth → URL Configuration**
    *   Set **Site URL** to your deployed frontend origin.
    *   Add development (`https://localhost:5001`) and production URLs to **Redirect URLs** to support email confirmation and password resets.
3.  **Auth → Email Templates**
    *   Customize the confirmation/reset templates if you need localization; default English copy works for the MVP.
4.  **Auth → Password Recovery**
    *   Ensure password reset emails are enabled; future flows depend on this toggle.
5.  **API Keys**
    *   Use the `anon` public key in `appsettings.Development.json` under `Supabase:AnonKey`.
    *   Never expose the `service_role` key in client code—store it only in secure server-side environments.

Document any deviations from this checklist in `SUMMARY.md` so new contributors inherit the latest guidance.

---

## Available Scripts

The following commands are available for managing the project from the command line.

*   **Build the project:**
    ```sh
    dotnet build
    ```

*   **Run the project locally:**
    ```sh
    dotnet run --project 10xJournal.Client
    ```

*   **Run the project with hot-reload for development:**
    ```sh
    dotnet watch --project 10xJournal.Client
    ```

*   **Run tests:**
    ```sh
    dotnet test
    ```

---

## Project Scope (MVP)

The scope for the Minimum Viable Product (MVP) is focused on delivering the core journaling experience.

*   **User Account Management:**
    *   Register with email and password.
    *   Login / Logout.
    *   Password reset functionality.
    *   Ability to permanently delete an account.

*   **Journal Entry Management (CRUD):**
    *   **Create:** A simple `textarea` editor with no formatting options.
    *   **Read:** A list of all entries, displayed in reverse chronological order.
    *   **Update:** Click any entry from the list to open it in edit mode.
    *   **Delete:** Remove an entry with a confirmation dialog.

*   **User Experience:**
    *   A welcome entry is automatically created for new users.
    *   A `mailto:` link in the footer for easy feedback.
    *   Mobile-first, fully responsive design.
    *   **Offline Support:** The currently edited entry is saved to the browser's Local Storage and synced when the connection is restored.

---

## Project Status

**Status:** 🚀 **MVP Development in Progress**

This project is currently under active development. The goal is to deliver the MVP within a 4-week timeframe.

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

&copy; 2025, dealen
