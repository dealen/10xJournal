using System.Text.RegularExpressions;
using static Microsoft.Playwright.Assertions;
using System.Text.Json;
using System.IO;

namespace _10xJournal.E2E.Tests;

/// <summary>
/// End-to-End tests for critical user journeys.
/// As per our testing strategy, we focus on 2-3 critical "happy path" scenarios.
/// </summary>
public class JourneyE2ETests : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;
    
    // Test configuration - In real implementation, these would be loaded from environment variables or config
    private readonly string _baseUrl = "http://localhost:5212/";  // Local dev server URL
    private readonly string _testUsername = "e2etest@testmail.com";
    private readonly string _testPassword = "TestPassword123!";

    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;
    public JourneyE2ETests()
    {
        // Load from appsettings.test.json without relying on Microsoft.Extensions.Configuration
        try
        {
            var json = File.Exists("appsettings.test.json")
                ? File.ReadAllText("appsettings.test.json")
                : "{}";

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Supabase", out var supabaseElement))
            {
                _supabaseUrl = supabaseElement.TryGetProperty("Url", out var url) ? url.GetString() ?? string.Empty : string.Empty;
                _supabaseKey = supabaseElement.TryGetProperty("AnonKey", out var key) ? key.GetString() ?? string.Empty : string.Empty;
            }
            else
            {
                _supabaseUrl = string.Empty;
                _supabaseKey = string.Empty;
            }
        }
        catch
        {
            _supabaseUrl = string.Empty;
            _supabaseKey = string.Empty;
        }
    }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true, // Set to true for CI environments, false for local debugging
        });
        _page = await _browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
    }

    /// <summary>
    /// Critical user journey #1: User can log in, create a journal entry, and see it on their list.
    /// </summary>
    [Fact]
    public async Task UserCanLoginCreateEntryAndSeeItOnTheList()
    {
        // Go to the application
        await _page.GotoAsync(_baseUrl);
        
        // Expect to be on the landing page with login option
        await Expect(_page).ToHaveTitleAsync(new Regex("10xJournal"));
        
        // Click the login button
        await _page.GetByRole(AriaRole.Button, new() { Name = "Zaloguj się" }).ClickAsync();
        
        // Fill in login form
        await _page.GetByLabel("Adres e-mail").FillAsync(_testUsername);
        await _page.GetByLabel("Hasło").FillAsync(_testPassword);
        
        // Submit login form
        await _page.GetByRole(AriaRole.Button, new() { Name = "Zaloguj się" }).ClickAsync();
        
        // Wait a bit for the login to process
        await _page.WaitForTimeoutAsync(2000);
        
        // Check for error messages on the page
        var pageContent = await _page.ContentAsync();
        var currentUrl = _page.Url;
        
        // Take a screenshot for debugging if login fails
        if (currentUrl.Contains("/login"))
        {
            await _page.ScreenshotAsync(new() { Path = "login-failed-debug.png" });
            
            // Try to find any error messages
            var errorElements = await _page.QuerySelectorAllAsync("[role='alert'], .error, p");
            var errors = new List<string>();
            foreach (var el in errorElements)
            {
                var text = await el.TextContentAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    errors.Add(text);
                }
            }
            
            throw new Exception($"Login failed - still on login page.\nCurrent URL: {currentUrl}\nErrors found: {string.Join(", ", errors)}");
        }
        
        // Expect to be redirected to the journal entries list
        await Expect(_page).ToHaveURLAsync(new Regex(".*/app/journal"));
        
        // Click on 'New Entry' button
        await _page.GetByRole(AriaRole.Button, new() { Name = "Nowy wpis" }).ClickAsync();
        
        // Expect to be on the create entry page
        await Expect(_page).ToHaveURLAsync(new Regex(".*/app/journal/new"));
        
        // Create a unique entry to identify it later
        string uniqueEntryText = $"Test Entry {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        await _page.GetByLabel("Journal entry content").FillAsync(uniqueEntryText);
        
        // Wait for auto-save (since our app uses auto-save)
        await _page.WaitForTimeoutAsync(2000);
        
        // Look for the save indicator showing "Saved"
        await Expect(_page.GetByText("Zapisano")).ToBeVisibleAsync();
        
        // Go back to entries list
        await _page.GetByRole(AriaRole.Button, new() { Name = "Back" }).ClickAsync();
        
        // Expect to see our new entry in the list
        await Expect(_page.GetByText(uniqueEntryText)).ToBeVisibleAsync();
    }
    
    /// <summary>
    /// Critical user journey #2: User can log in, edit an existing entry, and see the updated content.
    /// </summary>
    [Fact]
    public async Task UserCanLoginEditEntryAndSeeUpdatedContent()
    {
        // Go to the application
        await _page.GotoAsync(_baseUrl);
        
        // Click the login button
        await _page.GetByRole(AriaRole.Button, new() { Name = "Zaloguj się" }).ClickAsync();
        
        // Fill in login form
        await _page.GetByLabel("Adres e-mail").FillAsync(_testUsername);
        await _page.GetByLabel("Hasło").FillAsync(_testPassword);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Zaloguj się" }).ClickAsync();
        
        // Expect to be redirected to the journal entries list
        await Expect(_page).ToHaveURLAsync(new Regex(".*/app/journal"));
        
        // First create a new entry to ensure we have something to edit
        string uniqueEntryText = $"Entry to edit {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        
        // Click on 'New Entry' button
        await _page.GetByRole(AriaRole.Button, new() { Name = "Nowy wpis" }).ClickAsync();
        await _page.GetByLabel("Journal entry content").FillAsync(uniqueEntryText);
        
        // Wait for auto-save
        await _page.WaitForTimeoutAsync(2000);
        await Expect(_page.GetByText("Zapisano")).ToBeVisibleAsync();
        
        // Go back to entries list
        await _page.GetByRole(AriaRole.Button, new() { Name = "Back" }).ClickAsync();
        
        // Find and click on the entry we just created
        await _page.GetByText(uniqueEntryText).ClickAsync();
        
        // Update the entry with new content
        string updatedContent = $"Updated content {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        
        // Clear the previous content and enter new content
        await _page.GetByLabel("Journal entry content").FillAsync(updatedContent);
        
        // Wait for auto-save
        await _page.WaitForTimeoutAsync(2000);
        await Expect(_page.GetByText("Zapisano")).ToBeVisibleAsync();
        
        // Go back to entries list
        await _page.GetByRole(AriaRole.Button, new() { Name = "Back" }).ClickAsync();
        
        // Expect to see our updated entry in the list
        await Expect(_page.GetByText(updatedContent)).ToBeVisibleAsync();
        
        // And the old content should not be visible
        await Expect(_page.GetByText(uniqueEntryText)).Not.ToBeVisibleAsync();
    }
    
    /// <summary>
    /// Critical user journey #3: User can log in, delete an entry, and verify it's removed from the list.
    /// </summary>
    [Fact]
    public async Task UserCanLoginDeleteEntryAndVerifyItIsRemoved()
    {
        // Go to the application
        await _page.GotoAsync(_baseUrl);
        
        // Click the login button
        await _page.GetByRole(AriaRole.Button, new() { Name = "Zaloguj się" }).ClickAsync();
        
        // Fill in login form
        await _page.GetByLabel("Adres e-mail").FillAsync(_testUsername);
        await _page.GetByLabel("Hasło").FillAsync(_testPassword);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Zaloguj się" }).ClickAsync();
        
        // Expect to be redirected to the journal entries list
        await Expect(_page).ToHaveURLAsync(new Regex(".*/app/journal"));
        
        // First create a new entry to ensure we have something to delete
        string uniqueEntryText = $"Entry to delete {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        
        // Click on 'New Entry' button
        await _page.GetByRole(AriaRole.Button, new() { Name = "Nowy wpis" }).ClickAsync();
        await _page.GetByLabel("Journal entry content").FillAsync(uniqueEntryText);
        
        // Wait for auto-save
        await _page.WaitForTimeoutAsync(2000);
        await Expect(_page.GetByText("Zapisano")).ToBeVisibleAsync();
        
        // Go back to entries list
        await _page.GetByRole(AriaRole.Button, new() { Name = "Back" }).ClickAsync();
        
        // Verify our entry appears in the list
        await Expect(_page.GetByText(uniqueEntryText)).ToBeVisibleAsync();
        
        // Find the entry and click the delete button (assuming entries have a delete button)
        // Note: The actual selector might differ based on your UI implementation
        await _page.GetByText(uniqueEntryText).HoverAsync();
        await _page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        
        // Confirm the deletion if there's a confirmation dialog
        await _page.GetByRole(AriaRole.Button, new() { Name = "Confirm" }).ClickAsync();
        
        // Wait for the entry to be removed
        await _page.WaitForTimeoutAsync(1000);
        
        // Verify the entry is no longer in the list
        await Expect(_page.GetByText(uniqueEntryText)).Not.ToBeVisibleAsync();
    }

    /// <summary>
    /// Critical user journey #4: User can change their password and login with new credentials.
    /// This E2E test verifies the complete password change flow including:
    /// - Navigating to settings
    /// - Changing password
    /// - Logging out
    /// - Logging back in with new password
    /// </summary>
    [Fact]
    public async Task UserCanChangePasswordAndLoginWithNewCredentials()
    {
        var newPassword = "NewTestPassword456!";

        try
        {
            // Go to the application
            await _page.GotoAsync(_baseUrl);

            // Click the login button
            await _page.GetByRole(AriaRole.Button, new() { Name = "Zaloguj się" }).ClickAsync();

            // Fill in login form with original password
            await _page.GetByLabel("Adres e-mail").FillAsync(_testUsername);
            await _page.GetByLabel("Hasło").FillAsync(_testPassword);
            await _page.GetByRole(AriaRole.Button, new() { Name = "Zaloguj się" }).ClickAsync();

            // Wait for login to complete
            await _page.WaitForTimeoutAsync(2000);

            // Expect to be redirected to the journal entries page
            await Expect(_page).ToHaveURLAsync(new Regex(".*/app/journal"));

            // Navigate to Settings page
            // Look for Settings link in navigation or menu
            await _page.GetByRole(AriaRole.Link, new() { Name = "Ustawienia" }).ClickAsync();

            // Expect to be on Settings page
            await Expect(_page).ToHaveURLAsync(new Regex(".*/app/settings"));

            // Find the password change section
            await Expect(_page.GetByText("Zmiana hasła")).ToBeVisibleAsync();

            // Fill in the password change form
            await _page.Locator("#current-password").FillAsync(_testPassword);
            await _page.Locator("#new-password").FillAsync(newPassword);
            await _page.Locator("#confirm-password").FillAsync(newPassword);

            // Submit the password change form
            await _page.GetByRole(AriaRole.Button, new() { Name = "Zmień hasło" }).ClickAsync();

            // Wait for success message
            await _page.WaitForTimeoutAsync(2000);
            await Expect(_page.GetByText("Hasło zostało pomyślnie zmienione")).ToBeVisibleAsync();

            // Logout
            await _page.GetByRole(AriaRole.Button, new() { Name = "Wyloguj" }).ClickAsync();

            // Wait for logout to complete
            await _page.WaitForTimeoutAsync(1000);

            // Click login button again
            await _page.GetByRole(AriaRole.Button, new() { Name = "Zaloguj się" }).ClickAsync();

            // Try to login with NEW password
            await _page.GetByLabel("Adres e-mail").FillAsync(_testUsername);
            await _page.GetByLabel("Hasło").FillAsync(newPassword);
            await _page.GetByRole(AriaRole.Button, new() { Name = "Zaloguj się" }).ClickAsync();

            // Wait for login
            await _page.WaitForTimeoutAsync(2000);

            // Assert - Should be logged in successfully with new password
            await Expect(_page).ToHaveURLAsync(new Regex(".*/app/journal"));

            // Verify we're actually logged in by checking for user-specific content
            await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Nowy wpis" })).ToBeVisibleAsync();
        }
        finally
        {
            // IMPORTANT: Reset password back to original for future tests
            try
            {
                // If test passed, we're logged in with new password
                // Navigate back to settings and change password back
                await _page.GotoAsync($"{_baseUrl}app/settings");
                await _page.WaitForTimeoutAsync(1000);

                await _page.Locator("#current-password").FillAsync(newPassword);
                await _page.Locator("#new-password").FillAsync(_testPassword);
                await _page.Locator("#confirm-password").FillAsync(_testPassword);

                await _page.GetByRole(AriaRole.Button, new() { Name = "Zmień hasło" }).ClickAsync();
                await _page.WaitForTimeoutAsync(2000);

                Console.WriteLine("✅ Password reset back to original value");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Warning: Could not reset password back to original: {ex.Message}");
                Console.WriteLine("⚠️  Manual password reset may be required for test user");
            }
        }
    }
}
