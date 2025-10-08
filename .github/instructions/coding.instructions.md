---
applyTo: '**'
---
# General Coding Style Guide for GitHub Copilot

This document defines the coding standards and best practices for the 10xJournal project. The primary goal is to produce code that is exceptionally **readable**, performant, and simple.

### **Our Priorities**

1.  **📖 Readability:** Code is written for humans first. Clarity is more important than cleverness or overly concise syntax.
2.  **🚀 Performance:** The application must be fast and responsive. Use efficient patterns and be mindful of resource usage.
3.  **✨ Simplicity:** Avoid unnecessary complexity (KISS). A simpler solution is always better if it meets the requirements.

---

## ## C# Naming Conventions

Adhere strictly to standard Microsoft C# naming conventions to ensure consistency.

* **Classes, Interfaces, Enums, Public Methods, and Properties:** Use `PascalCase`.
    * *Example:* `class JournalEntry`, `interface ILogger`, `Task SaveEntryAsync()`
* **Method Parameters and Local Variables:** Use `camelCase`.
    * *Example:* `var newEntry = new JournalEntry();`, `void UpdateEntry(JournalEntry entryToUpdate)`
* **Private Fields:** Use `_camelCase` (leading underscore).
    * *Example:* `private readonly Supabase.Client _supabaseClient;`
* **Clarity Over Brevity:** Use descriptive names. `isSaving` is better than `isS`. `entryToDelete` is better than `e`.
* **Async Suffix:** All methods that are awaitable must end with the `Async` suffix.
    * *Example:* `GetEntriesAsync`, `SaveChangesAsync`

---

## ## Simplicity and Readability

This is our most important principle.

* **Small, Focused Methods:** Each method should do one thing and do it well (Single Responsibility Principle). Aim for methods no longer than 15-20 lines.
* **Comment the "Why", Not the "What":** Good code should be self-documenting in *what* it does. Use comments to explain the *why*—the business reasons, the context, or the trade-offs of a particular implementation.
    * *Example:* `// We must validate the user ID here because of legacy import rules.`
* **No "Magic" Values:** Do not use hardcoded strings or numbers directly in your code. Define them as `const` or `static readonly` fields with descriptive names.
* **Readable LINQ:** Use LINQ for its expressiveness, but if a query becomes a long, unreadable chain, break it into multiple steps or rewrite it as a simpler loop.
* **XML Docs for Public Members:** Add `///` XML documentation comments to all public methods and properties to describe what they do, their parameters, and what they return.

---

## ## Performance Considerations

* **Async Everywhere:** For all I/O-bound operations (especially database calls to Supabase), use `async` and `await` to prevent blocking threads and keep the UI responsive. **Never use `.Result` or `.Wait()`.**
* **Efficient Data Fetching:** Fetch only the data you need from Supabase. Use `.Select()` to project your query into a smaller model rather than pulling down entire objects with all their properties.
* **Blazor Rendering:**
    * Use the `@key` directive in loops (`@foreach`) to help Blazor's diffing algorithm efficiently update the DOM.
    * Avoid performing heavy computations within the component's rendering lifecycle.
* **Mindful Enumeration:** When using LINQ on an `IEnumerable`, be aware of multiple enumerations. If you need to iterate over a collection more than once, cache the results with `.ToList()` or `.ToArray()`.

---

## ## Blazor-Specific Best Practices

* **Dependency Injection:** Always use constructor injection to get services in your components and classes.
* **Component Structure:** As per our architecture, keep UI and logic in the same `.razor` file for most components. The logic goes in the `@code` block.
* **Parameter-less Constructors:** Ensure all Blazor components have a parameter-less constructor for the framework to be able to instantiate them (this is the default).
* **State Management:** For the MVP, manage state at the component level. Avoid introducing a global state management library until a clear need arises.

---

## ## Error Handling

* **Use `try-catch` blocks** for any operation that can fail, especially network calls to Supabase or file system access.
* **Log Exceptions:** Always log the full exception details for debugging purposes.
* **User-Friendly Messages:** Never show a raw exception stack trace to the user. Catch the exception and display a clear, friendly error message in the UI.