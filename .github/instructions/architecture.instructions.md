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

---

## ## How to Create New Features, Services, and Components

This section provides a step-by-step guide for creating new elements within the 10xJournal project while maintaining our vertical slice architecture.

### Creating a New Feature

1. **Identify the Feature Boundary**: 
   - Define what user need the feature addresses
   - Keep the scope focused and contained

2. **Create the Feature Folder Structure**:
   ```
   /Features
     /{FeatureName}                   # Use PascalCase
       /{UseCase}                     # Specific use case within feature (e.g., Create, Update, List)
         - {UseCase}.razor            # Main component
         - {UseCase}Model.cs          # Data model if needed
         - {UseCase}Request.cs        # Request model if needed
         - {UseCase}Handler.cs        # Complex logic handler if needed
         - {UseCase}Validator.cs      # Validation logic if needed
   ```

3. **Implement the Component and Logic**:
   - For simple components, place all logic in the `@code` block of the .razor file
   - For complex logic, extract to a dedicated Handler class in the same folder
   - Always inject Supabase.Client directly when needed for data access

4. **Data Models**:
   - Feature-specific models should be placed in the feature folder
   - Models shared across multiple use cases within a feature can go in a `Models` folder within the feature folder
   - Example: `/Features/JournalEntries/Models/JournalEntry.cs`

5. **Route Configuration**:
   - Add `@page` directive at the top of your component
   - Follow the routing convention: `/app/{feature}/{action}`
   - Example: `@page "/app/journal/create"`

6. **Connect to Supabase**:
   - Inject `Supabase.Client` directly in your component or handler class
   - Use it to directly access the database without additional abstractions

### When to Create Shared/Infrastructure Code

There are legitimate cases for code outside the feature slices:

1. **Cross-Cutting Infrastructure**:
   - Authentication systems that work across all features
   - Error handling mechanisms used throughout the application
   - Logging services used by multiple features

   Place these in `/Infrastructure` with appropriate subfolders.

2. **Shared UI Components**:
   - UI components used across multiple features (buttons, cards, etc.)
   - Place these in `/Shared/Components` 
   - Focus only on presentation concerns, not business logic

3. **Shared Models**:
   - Only for models that truly represent common domain concepts used across many features
   - Place these in `/Shared/Models`
   - Example: `UserProfile.cs` (used by many features)

### Practical Example: Creating a "Reminders" Feature

Let's walk through creating a new feature for journal entry reminders:

1. **Create the folder structure**:
   ```
   /Features
     /Reminders
       /CreateReminder
         - CreateReminder.razor
         - CreateReminderRequest.cs
       /ListReminders
         - ListReminders.razor
       /Models
         - Reminder.cs
   ```

2. **Implement the Reminder model**:
   ```csharp
   // /Features/Reminders/Models/Reminder.cs
   using Supabase.Postgrest.Attributes;
   using Supabase.Postgrest.Models;
   
   namespace _10xJournal.Client.Features.Reminders.Models
   {
       [Table("reminders")]
       public class Reminder : BaseModel
       {
           [PrimaryKey("id", false)]
           public Guid? Id { get; set; }
           
           [Column("user_id")]
           public Guid UserId { get; set; }
           
           [Column("title")]
           public string Title { get; set; } = string.Empty;
           
           [Column("scheduled_for")]
           public DateTimeOffset ScheduledFor { get; set; }
           
           [Column("created_at", ignoreOnInsert: true)]
           public DateTimeOffset CreatedAt { get; set; }
       }
   }
   ```

3. **Create the request model**:
   ```csharp
   // /Features/Reminders/CreateReminder/CreateReminderRequest.cs
   using System.ComponentModel.DataAnnotations;
   
   namespace _10xJournal.Client.Features.Reminders.CreateReminder
   {
       public class CreateReminderRequest
       {
           [Required(ErrorMessage = "Title is required.")]
           public string Title { get; set; } = string.Empty;
           
           [Required(ErrorMessage = "Reminder time is required.")]
           public DateTimeOffset ScheduledFor { get; set; }
       }
   }
   ```

4. **Implement the component**:
   ```csharp
   // /Features/Reminders/CreateReminder/CreateReminder.razor
   @page "/app/reminders/create"
   @using _10xJournal.Client.Features.Reminders.Models
   @using _10xJournal.Client.Infrastructure.Authentication
   @inject Supabase.Client SupabaseClient
   @inject NavigationManager NavigationManager
   @inject ILogger<CreateReminder> Logger
   @inject CurrentUserAccessor CurrentUserAccessor
   
   <h2>Create New Reminder</h2>
   
   <EditForm Model="@request" OnValidSubmit="HandleSubmitAsync">
       <DataAnnotationsValidator />
       
       <div class="form-group">
           <label for="title">Title:</label>
           <InputText id="title" @bind-Value="request.Title" class="form-control" />
           <ValidationMessage For="@(() => request.Title)" />
       </div>
       
       <div class="form-group">
           <label for="scheduledFor">When:</label>
           <InputDateTime id="scheduledFor" @bind-Value="request.ScheduledFor" class="form-control" />
           <ValidationMessage For="@(() => request.ScheduledFor)" />
       </div>
       
       <button type="submit" class="btn btn-primary">Create Reminder</button>
   </EditForm>
   
   @code {
       private CreateReminderRequest request = new();
       private string? errorMessage;
       
       private async Task HandleSubmitAsync()
       {
           try
           {
               var currentUserId = await CurrentUserAccessor.GetCurrentUserIdAsync();
               
               var reminder = new Reminder
               {
                   Title = request.Title,
                   ScheduledFor = request.ScheduledFor,
                   UserId = currentUserId.Value
               };
               
               var response = await SupabaseClient.From<Reminder>().Insert(reminder);
               
               if (response.Model != null)
               {
                   NavigationManager.NavigateTo("/app/reminders");
               }
           }
           catch (Exception ex)
           {
               Logger.LogError(ex, "Failed to create reminder");
               errorMessage = "Failed to create reminder. Please try again.";
           }
       }
   }
   ```

### Testing New Features

Follow these guidelines when testing new features:

1. **Integration Tests** (Highest Priority):
   - Create tests that verify your feature's interaction with Supabase
   - Test both happy path and error scenarios
   - Place tests in the Client.Tests project in a structure mirroring your feature

2. **End-to-End Tests** (For Critical Features):
   - Use Playwright to test critical user journeys
   - Focus on "happy path" scenarios
   - Place these tests in the E2E.Tests project

3. **Unit Tests** (For Complex Logic):
   - Only for complex business logic with no external dependencies
   - Not needed for simple CRUD operations

Remember that all tests should verify that your vertical slice is working correctly as a whole, not just isolated parts.
````