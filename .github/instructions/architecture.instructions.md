---
applyTo: '**'
---

Of course. Here is the content for an `instructions.md` file tailored for GitHub Copilot, explaining the Vertical Slice Architecture for your project. You can place this file in the root of your repository or in a `.github` folder.

-----

````markdown
# Project Architecture & Coding Style Guide for GitHub Copilot

This document outlines the architectural principles and coding conventions for the 10xJournal project. Please adhere to these guidelines when generating code.

The primary architectural pattern for this project is **Vertical Slice Architecture**. Our goal is to organize code by feature, not by technical layer.

---

## ## Core Principles

1.  **Organize by Feature:** All code related to a single feature (e.g., creating a journal entry) should be located in the same folder or "slice".
2.  **Slices are Self-Contained:** A feature slice should contain everything it needs to function, from the UI component to the data access logic. Minimize dependencies between slices.
3.  **NO Horizontal Layers:** Do **not** create folders for traditional technical layers like `Services`, `Repositories`, `ViewModels`, or `BusinessLogic`. Logic should be co-located with the feature it serves.
4.  **Direct Communication with BaaS:** The frontend (Blazor WASM) communicates directly with the Backend-as-a-Service (Supabase). Do not abstract this communication behind generic repository or service layers.
5.  **Keep it Simple (KISS):** Avoid unnecessary complexity and abstractions. Prefer simple, direct code over heavily engineered patterns.

---

## ## File and Folder Structure

The code should be organized within a top-level `Features` directory. Each feature gets its own folder, and each use case within that feature gets its own sub-folder.

**✅ DO THIS:**

```
/10xJournal.Client
│
└─── Features/
     │
     ├─── Authentication/         // Feature: Authentication
     │    ├─── Login/
     │    │    └─── Login.razor    // The component for the login feature
     │    └─── Register/
     │         └─── Register.razor
     │
     └─── JournalEntries/          // Feature: Journal Entries
          ├─── CreateEntry/
          │    ├─── CreateEntry.razor  // UI and logic for creating an entry
          │    └─── CreateEntry.cs     // (Optional) Model or state for this feature
          │
          ├─── DeleteEntry/
          │    └─── DeleteEntryHandler.cs // Contains logic to call Supabase and delete an entry
          │
          └─── ListEntries/
               ├─── ListEntries.razor      // The component to display all entries
               └─── EntrySummary.cs      // A model specific to this feature slice
```

---

## ## Code Style and Best Practices

* **Component Logic:** For simple slices, place the C# logic directly inside the `@code` block of the Blazor component. For more complex logic (e.g., multiple dependencies, complex state), you can create a separate handler class (e.g., `DeleteEntryHandler.cs`), but it must remain inside the feature's folder.
* **Naming:** Name files and folders clearly based on the feature and its action. For example, a feature for editing a user profile should be in `Features/Profile/EditProfile/EditProfile.razor`.
* **Validation:** If you use a validation library like FluentValidation, the validator class for a feature must also reside within that feature's folder (e.g., `CreateEntryValidator.cs` would be inside `Features/JournalEntries/CreateEntry/`).
* **Shared Code:** Code that is truly shared across multiple slices (e.g., a custom button component, base page layouts) should be placed in the standard Blazor `Shared` or `Components` folders, but *not* business logic.

---

## ## Example: Creating a New Journal Entry

To illustrate the difference, here is the correct and incorrect way to structure the code for a new feature.

### ✅ DO THIS (Vertical Slice Approach)

All files for the "Create Entry" feature are in one place.

```
/Features
  /JournalEntries
    /CreateEntry
      - CreateEntry.razor
      - CreateEntry.cs          // Request/Model for the form
      - CreateEntryValidator.cs // Validation logic
```

### ❌ AVOID THIS (Traditional Layered Approach)

Do not spread the files for a single feature across multiple technical folders.

```
/Services
  - JournalEntryService.cs   // Contains the creation logic

/ViewModels
  - CreateEntryViewModel.cs  // Contains the model for the form

/Validators
  - CreateEntryValidator.cs  // Contains the validation logic

/Pages
  - CreateEntry.razor        // Contains the UI
```
````