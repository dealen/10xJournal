10xJournal - Best Practices & Development Guidelines
This document outlines the core development practices for the 10xJournal project. As a solo developer project, this serves as a checklist to maintain code quality, consistency, and alignment with the project's minimalist philosophy.

1. Core Principles (The "Why")
Simplicity First: Every feature, component, and line of code should be evaluated against the question: "Does this make journaling simpler?" If not, reconsider.

Mobile-First is Non-Negotiable: All UI/UX decisions must be validated on a mobile viewport first. The desktop experience is an adaptation of the mobile one.

Focus on the Core Loop: The user's primary journey is: Open App -> Create/Edit Entry -> Close App. Protect this flow from any distractions.

Security by Design: Data privacy is paramount. Assume all client-side code is public and secure the application at the BaaS (Supabase) layer.

2. Development Workflow
Branching Strategy:

Use main as the stable, deployable branch.

Create feature branches from main using the pattern feature/<feature-name> (e.g., feature/offline-entry-sync).

Use fix/<bug-description> for bug fixes.

Commits:

Follow the Conventional Commits specification.

Examples: feat: Add entry deletion confirmation dialog, fix: Correctly handle timezone on entry list, docs: Update RLS policy notes.

Pull Requests (PRs):

Even as a solo dev, use PRs to merge into main. This provides a code review checkpoint and a clear history of changes.

Ensure the CI pipeline (GitHub Actions) passes before merging.

CI/CD Pipeline (GitHub Actions):

The workflow should be triggered on every push to main.

Key steps:

actions/checkout@v3: Checkout the code.

actions/setup-dotnet@v3: Set up the correct .NET SDK version.

dotnet restore: Restore dependencies.

dotnet build --configuration Release: Build the project in Release mode.

dotnet test: Run all unit tests. The build will fail if any test fails. (Future step)

Azure/static-web-apps-deploy@v1: Deploy to Azure Static Web Apps. Store secrets (AZURE_STATIC_WEB_APPS_API_TOKEN) in GitHub repository secrets.

3. Blazor WASM & UI
Component Design:

Keep components small and focused on a single responsibility.

Use @bind for simple two-way data binding in forms.

For state that needs to be shared (e.g., user authentication status), use a cascading parameter from a top-level MainLayout.razor component. Avoid complex state management libraries for the MVP.

PWA & Offline-First:

The primary goal of offline support is to prevent data loss.

When creating/editing an entry, automatically save the current draft to localStorage on a timer (e.g., every 5 seconds).

Create a simple SyncService that checks for unsynced local data upon application startup (when online) and pushes it to Supabase.

Ensure the service-worker.js caches all necessary application assets (_framework, CSS, etc.) for instant loading.

Styling (Pico.css):

Embrace semantic HTML. Use <main>, <article>, <header>, <footer>, <dialog> etc. Pico styles these elements directly, reducing the need for custom classes.

Avoid adding utility classes. Let the framework handle the base styling.

For layout, use Pico's simple grid system if necessary, but prefer native CSS Flexbox or Grid for simplicity.

4. Supabase Integration (Backend as a Service)
SDK Usage:

Use the official supabase-csharp client.

Register the Supabase client as a singleton service in Program.cs for dependency injection.

Authentication:

Handle the auth flow securely using the SDK. Never handle tokens manually on the client side.

Use supabase.Auth.OnAuthStateChanged to create an event stream that the Blazor app can subscribe to, updating the UI in real-time when the user logs in or out.

Row Level Security (RLS) is CRITICAL:

Rule #1: Enable RLS on every table containing user data.

Rule #2: The default policy for a new table should be to DENY all access.

Rule #3: Create explicit POLICY rules for SELECT, INSERT, UPDATE, DELETE operations.

Example Policy (for entries table):

-- Users can only see their own entries
CREATE POLICY "Enable read access for own entries"
ON public.entries FOR SELECT
USING (auth.uid() = user_id);

-- Users can only insert entries for themselves
CREATE POLICY "Enable insert for own entries"
ON public.entries FOR INSERT
WITH CHECK (auth.uid() = user_id);

5. Logging & Error Handling
Structured Logging: Use Serilog for all logging. Inject ILogger into your components and services.

Global Exception Handling: Implement a global handler to catch unhandled exceptions.

Correlation ID:

When an exception is caught, generate a unique ID (e.g., a GUID).

To the User: Display a simple, friendly error message like: "Sorry, something went wrong. Please contact support with error ID: [Generated-ID]".

To the Logs (Sentry/Seq): Log the full exception details, stack trace, and context, along with the [Generated-ID]. This allows you to instantly find the exact technical error from a user's report.

6. Tooling (VS Code & GitHub Copilot)
VS Code Extensions: Ensure you have the official C#, .NET Runtime, and Blazor WASM debugging extensions installed and up to date.

GitHub Copilot Prompts:

Use comments to guide Copilot effectively.

For Blazor: // Blazor component with a textarea and a save button

For Logic: // C# function to check if the user is online using Blazor's IJSRuntime

For Supabase: // C# function using supabase-csharp to fetch all entries for the current user

For RLS: // SQL policy for Supabase to allow users to update their own entries

Always review Copilot's suggestions. It's a powerful assistant, not a replacement for understanding the code.

7. Testing Strategy
Unit Tests (xUnit):

Focus on testing business logic within services (e.g., SyncService).

Use a mocking library like Moq or NSubstitute to mock dependencies (e.g., IJSRuntime, Supabase client).

Tests should be fast, reliable, and run as part of the CI pipeline.

Component Tests (bUnit):

For more complex UI components, use bUnit to verify rendering logic and user interactions without a browser. This is a good candidate post-MVP.

End-to-End (E2E) Tests:

E2E tests are out of scope for the MVP but should be considered later. Tools like Playwright can automate browser interactions to test full user flows (e.g., login -> create entry -> logout).

8. Code Style & Naming Conventions
C# Conventions:

Follow the standard Microsoft C# Coding Conventions.

Use PascalCase for class names, method names, and properties (MyClass, GetUserEntries).

Use camelCase for local variables and method parameters (var userEntries = ...).

Use an underscore prefix for private fields (private readonly ILogger _logger;).

Blazor (.razor) Conventions:

Filenames should be PascalCase (e.g., EntryList.razor).

Component parameters should be decorated with [Parameter] and be public properties.

Use @code { ... } for C# logic within the component. Keep it clean and move complex logic to services.

CSS:

Since Pico.css is class-less, custom CSS should be minimal. If needed, create a separate app.css and use semantic custom properties (CSS variables) for theming.

9. Accessibility (a11y)
Semantic HTML: Continue to use semantic HTML as it's the foundation of accessibility.

ARIA Roles: For custom interactive components (if any are built), use appropriate ARIA (Accessible Rich Internet Applications) roles.

Keyboard Navigation: Ensure all interactive elements are reachable and operable via the keyboard.

Contrast: Pico.css has good default contrast ratios, but double-check any custom colors.

10. Documentation
Code Comments: Use XML documentation comments for public APIs (classes, methods, etc.) to generate API documentation.

README.md: Maintain a clear and concise README.md file at the root of the project to explain the project setup, usage, and development guidelines.

In-line Documentation: Use comments judiciously within the code to explain complex logic or decisions.

13. Information about frameworks and libraries used
Maintain a document (e.g., FRAMEWORKS.md) that lists all major frameworks and libraries used in the project, along with their versions and brief descriptions of their roles.
Also add information about why they were chosen over alternatives.
During implementation of new features, update this document if new libraries are added or existing ones are removed. Also include information about usage and new things learned about them.

12. Summary

Summarise each development stem by writing a short paragraph about what was done, why it was done, and how it aligns with the project's principles. This helps maintain clarity and focus throughout the development process. Place it in SUMMARY.md.