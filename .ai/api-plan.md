# REST API Plan

This document outlines the REST API for the 10xJournal application. The architecture leverages Supabase as a Backend-as-a-Service (BaaS), utilizing its auto-generated PostgREST API for standard CRUD operations and custom PostgreSQL functions (exposed as RPC endpoints) for specific business logic.

## 1\. Resources

The API exposes the following primary resources, which directly map to the database tables:

  * **Journal Entries**: The core content created by users.
      * Database Table: `public.journal_entries`
  * **User Streaks**: Data related to a user's writing habit.
      * Database Table: `public.user_streaks`
  * **User Profile**: Public profile information for a user.
      * Database Table: `public.profiles`

## 2\. Endpoints

All endpoints are relative to the Supabase project URL (e.g., `https://<project-ref>.supabase.co`).

-----

### **Resource: Journal Entries**

Provides full CRUD access to a user's own journal entries. Access is governed by Row Level Security (RLS) policies, ensuring users can only interact with their own data.

#### **List All Journal Entries**

  * **Method**: `GET`
  * **Path**: `/rest/v1/journal_entries`
  * **Description**: Retrieves a list of all journal entries for the authenticated user, sorted from newest to oldest.
  * **Query Parameters**:
      * `select=*`: (Required) Specifies that all columns should be returned.
      * `order=created_at.desc`: (Required) Sorts the entries by creation date in descending order, as specified in the PRD.
  * **Request Payload**: N/A
  * **Success Response**:
      * **Code**: `200 OK`
      * **Payload**:
        ```json
        [
            {
                "id": "a1b2c3d4-e5f6-a7b8-c9d0-e1f2a3b4c5d6",
                "user_id": "f1g2h3i4-j5k6-l7m8-n9o0-p1q2r3s4t5u6",
                "content": "This is my latest journal entry.",
                "created_at": "2025-10-11T18:00:00Z",
                "updated_at": "2025-10-11T18:00:00Z"
            },
            {
                "id": "b2c3d4e5-f6a7-b8c9-d0e1-f2a3b4c5d6e7",
                "user_id": "f1g2h3i4-j5k6-l7m8-n9o0-p1q2r3s4t5u6",
                "content": "This was my first entry, the welcome message.",
                "created_at": "2025-10-10T10:00:00Z",
                "updated_at": "2025-10-10T10:00:00Z"
            }
        ]
        ```
  * **Error Response**:
      * **Code**: `401 Unauthorized` - If the request is missing or has an invalid JWT.

-----

#### **Create a New Journal Entry**

  * **Method**: `POST`
  * **Path**: `/rest/v1/journal_entries`
  * **Description**: Creates a new journal entry. The `user_id` is inferred from the authenticated user. A database trigger automatically updates the user's streak upon successful insertion.
  * **Request Payload**:
    ```json
    {
        "content": "A new thought I had today."
    }
    ```
  * **Success Response**:
      * **Code**: `201 Created`
      * **Payload**: The newly created journal entry object.
        ```json
        {
            "id": "c3d4e5f6-a7b8-c9d0-e1f2-a3b4c5d6e7f8",
            "user_id": "f1g2h3i4-j5k6-l7m8-n9o0-p1q2r3s4t5u6",
            "content": "A new thought I had today.",
            "created_at": "2025-10-12T09:30:00Z",
            "updated_at": "2025-10-12T09:30:00Z"
        }
        ```
  * **Error Responses**:
      * **Code**: `400 Bad Request` - If `content` is null or missing.
      * **Code**: `401 Unauthorized` - Invalid JWT.

-----

#### **Update a Journal Entry**

  * **Method**: `PATCH`
  * **Path**: `/rest/v1/journal_entries?id=eq.<entry_id>`
  * **Description**: Updates the content of a specific journal entry.
  * **Request Payload**:
    ```json
    {
        "content": "An updated version of my thoughts."
    }
    ```
  * **Success Response**:
      * **Code**: `200 OK`
      * **Payload**: The updated journal entry object.
  * **Error Responses**:
      * **Code**: `401 Unauthorized` - Invalid JWT.
      * **Code**: `404 Not Found` - If the entry does not exist or does not belong to the user.

-----

#### **Delete a Journal Entry**

  * **Method**: `DELETE`
  * **Path**: `/rest/v1/journal_entries?id=eq.<entry_id>`
  * **Description**: Permanently deletes a specific journal entry.
  * **Request Payload**: N/A
  * **Success Response**:
      * **Code**: `204 No Content`
  * **Error Responses**:
      * **Code**: `401 Unauthorized` - Invalid JWT.
      * **Code**: `404 Not Found` - If the entry does not exist or does not belong to the user.

-----

### **Resource: User Streaks**

Provides read-only access to the user's streak data. This data is calculated and updated by database triggers, not directly by the client.

#### **Get User Streak**

  * **Method**: `GET`
  * **Path**: `/rest/v1/user_streaks`
  * **Description**: Retrieves the writing streak data for the authenticated user.
  * **Query Parameters**:
      * `select=*`
  * **Request Payload**: N/A
  * **Success Response**:
      * **Code**: `200 OK`
      * **Payload**: A single object in an array.
        ```json
        [
            {
                "user_id": "f1g2h3i4-j5k6-l7m8-n9o0-p1q2r3s4t5u6",
                "current_streak": 5,
                "longest_streak": 12,
                "last_entry_date": "2025-10-11"
            }
        ]
        ```
  * **Error Response**:
      * **Code**: `401 Unauthorized` - Invalid JWT.

-----

### **Business Logic: Account Management (RPC)**

These endpoints handle business logic that goes beyond simple CRUD operations.

#### **Export All Journal Entries**

  * **Method**: `POST`
  * **Path**: `/rpc/export_journal_entries`
  * **Description**: A custom function that retrieves all journal entries for the authenticated user and formats them into a single JSON object for easy client-side file generation.
  * **Request Payload**: N/A
  * **Success Response**:
      * **Code**: `200 OK`
      * **Payload**:
        ```json
        {
            "export_date": "2025-10-12T10:00:00Z",
            "entry_count": 2,
            "entries": [
                {
                    "created_at": "2025-10-11T18:00:00Z",
                    "content": "This is my latest journal entry."
                },
                {
                    "created_at": "2025-10-10T10:00:00Z",
                    "content": "This was my first entry, the welcome message."
                }
            ]
        }
        ```
  * **Error Response**:
      * **Code**: `401 Unauthorized` - Invalid JWT.

-----

#### **Delete User Account**

  * **Method**: `POST`
  * **Path**: `/rpc/delete_my_account`
  * **Description**: A secure function that permanently deletes the user's record from `auth.users`. Due to `ON DELETE CASCADE` constraints, this action will automatically and irreversibly delete the user's profile, all journal entries, and streak data. This must be called after the user has confirmed the action and exported their data.
  * **Request Payload**: N/A
  * **Success Response**:
      * **Code**: `200 OK`
      * **Payload**:
        ```json
        {
            "status": "success",
            "message": "Account and all associated data have been permanently deleted."
        }
        ```
  * **Error Response**:
      * **Code**: `401 Unauthorized` - Invalid JWT.

-----

## 3\. Authentication and Authorization

  * **Authentication Mechanism**: Authentication is handled by Supabase Auth. The client (Blazor WASM) is responsible for the sign-up and login flows. Upon successful authentication, Supabase provides a **JSON Web Token (JWT)**.
  * **Implementation**: Every request to the endpoints listed above (excluding auth-specific endpoints like login/signup) must include an `Authorization` header with the JWT as a Bearer token.
      * **Example Header**: `Authorization: Bearer <your_jwt_token>`
  * **Authorization**: Authorization is enforced at the database level using **PostgreSQL's Row Level Security (RLS)**. The policies defined in the database schema ensure that the `auth.uid()` from the JWT is matched against the `user_id` or `id` in the tables, strictly isolating data between users.

-----

## 4\. Validation and Business Logic

  * **Data Validation**: Primary data validation is enforced by the database schema's constraints (`NOT NULL`, `FOREIGN KEY`, data types). For example, attempting to create a journal entry with `content: null` will be rejected by the database, and PostgREST will return a `400 Bad Request` status code.
  * **Business Logic Implementation**: Key business logic is automated within the PostgreSQL database using **triggers and functions** to ensure consistency and security.
      * **User Onboarding**: A trigger on `auth.users` automatically creates corresponding records in `public.profiles` and `public.user_streaks` upon user registration.
      * **Welcome Entry**: A subsequent trigger on `public.profiles` creates the initial welcome message in `public.journal_entries`.
      * **Streak Calculation**: A trigger on `public.journal_entries` fires after every `INSERT` to call a function that recalculates and updates the `current_streak`, `longest_streak`, and `last_entry_date` in the `public.user_streaks` table.

This server-centric approach minimizes the logic required on the client side, making the frontend application simpler and ensuring that business rules are always applied, regardless of how the data is accessed.