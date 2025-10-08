---
applyTo: '**'
---
# Testing Strategy Guide for GitHub Copilot

This document outlines the testing strategy for the 10xJournal project. Please follow these guidelines when generating tests or code that needs to be tested.

Our testing philosophy is **pragmatic and risk-based**. We prioritize tests that provide the most confidence in our application's stability with the least amount of effort, especially for the MVP.

**The order of importance for tests in this project is: 1. Integration Tests, 2. End-to-End Tests, 3. Unit Tests.**

---

## ## Integration Tests (Highest Priority)

This is the most important type of test in our project.

* **Primary Goal:** Verify the interaction between our code and the Supabase BaaS.
* **What to Test:**
    * Focus on testing all **CRUD operations** (Create, Read, Update, Delete) against the database.
    * Critically, test our **Row Level Security (RLS)** policies. For example, create a test that proves `User A` cannot access data belonging to `User B`.
    * Test authentication flows like user creation and login.
* **How to Test:**
    * These tests **must run against a real, dedicated test Supabase instance**.
    * **Do not mock the Supabase client** or database interactions. We are testing the actual integration.
* **Tool:** Use **xUnit** as the test runner.

---

## ## End-to-End (E2E) Tests (High Priority)

These tests act as a final safety net to ensure critical user flows are working.

* **Primary Goal:** Verify critical user journeys from start to finish in an automated browser.
* **What to Test:**
    * Focus on **2-3 critical "happy path" scenarios**. Do not aim for complete coverage of all edge cases.
    * **Example Scenario 1:** A user can successfully register, log in, create a new entry, see it on the list, and then log out.
    * **Example Scenario 2:** A user can log in, navigate to an entry, delete it, and verify it has been removed from the list.
* **Tool:** Use **Playwright** with its .NET library.

---

## ## Unit Tests (As-Needed Basis)

Unit tests are the lowest priority for this MVP due to the nature of the application.

* **Primary Goal:** Test small, isolated pieces of pure logic.
* **What to Test:**
    * Write unit tests **only** for utility functions or complex business logic that has **no external dependencies** (no UI, no database calls, no HTTP requests).
    * An example could be a custom text formatting utility or a complex client-side validation rule.
* **What NOT to Test:**
    * Do not write unit tests for Blazor component rendering.
    * Do not write unit tests for simple methods that just call the Supabase client (this is covered by integration tests).
* **Tool:** Use **xUnit** as the test runner.