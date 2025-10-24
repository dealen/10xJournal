using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using _10xJournal.E2E.Tests.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace _10xJournal.E2E.Tests.Features.Authentication;

/// <summary>
/// E2E tests for authentication user journeys.
/// Tests the complete flow from UI interaction to database state.
/// Each test includes automatic cleanup of test data via TestDataCleanupHelper.
/// </summary>
public class AuthenticationJourneyE2ETests : E2ETestBase
{
    /// <summary>
    /// Critical Test 1: Verifies that a new user can successfully register and see the welcome entry.
    /// This is the primary happy path for user onboarding.
    /// </summary>
    [Fact]
    public async Task NewUser_CanRegisterAndSeeWelcomeEntry()
    {
        // Arrange
        var testEmail = GenerateTestUserEmail();
        var testPassword = GenerateTestPassword();
        
        // Register for cleanup BEFORE attempting registration
        RegisterUserForCleanup(testEmail);

        Logger.LogInformation("Testing registration for user: {Email}", testEmail);

        // Act - Navigate to registration page
        await Page.GotoAsync($"{BaseUrl}/register");

        // Wait for the page to load
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Fill registration form
        var emailInput = Page.Locator("input[type='email'], input[name='email']");
        await emailInput.FillAsync(testEmail);

        var passwordInput = Page.Locator("input[type='password'], input[name='password']").First;
        await passwordInput.FillAsync(testPassword);

        var confirmPasswordInput = Page.Locator("input[type='password'], input[name='password']").Nth(1);
        await confirmPasswordInput.FillAsync(testPassword);

        // Submit the form
        var submitButton = Page.Locator("button[type='submit']");
        await submitButton.ClickAsync();
        
        // Wait for URL change (SPA navigation, not full page load)
        await Page.WaitForURLAsync(new Regex(".*/app/journal|.*/registration-success"), new() { Timeout = 15000 });
        
        // Verify we're on the journal page (auto-login when email confirmation disabled)
        var currentUrl = Page.Url;
        Logger.LogInformation("After registration, navigated to: {Url}", currentUrl);
        
        if (!currentUrl.Contains("/app/journal"))
        {
            Logger.LogError("Expected redirect to /app/journal but got: {Url}", currentUrl);
            throw new InvalidOperationException($"Unexpected redirect to {currentUrl}. Expected /app/journal");
        }

        // Verify welcome entry is visible (content is in Polish)
        var welcomeEntry = Page.Locator("text=/Witaj w 10xJournal/i");
        await Expect(welcomeEntry).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Verify authenticated state (logout button should exist in DOM)
        var logoutButton = Page.Locator("button:has-text('Logout'), a:has-text('Logout'), button:has-text('Wyloguj'), a:has-text('Profile')");
        await Expect(logoutButton.First).ToBeAttachedAsync(new() { Timeout = 5000 });

        Logger.LogInformation("✅ Registration test passed for user: {Email}", testEmail);
        
        // Cleanup happens automatically in DisposeAsync
    }

    /// <summary>
    /// Critical Test 2: Verifies that a registered user can login and access their entries.
    /// Tests the complete login journey and entry list access.
    /// </summary>
    [Fact]
    public async Task RegisteredUser_CanLoginAndAccessEntries()
    {
        // Arrange - Pre-create test user via registration
        var testEmail = GenerateTestUserEmail();
        var testPassword = GenerateTestPassword();
        
        RegisterUserForCleanup(testEmail);

        Logger.LogInformation("Testing login for user: {Email}", testEmail);

        // Create user via registration first
        await Page.GotoAsync($"{BaseUrl}/register");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        await Page.Locator("input[type='email'], input[name='email']").FillAsync(testEmail);
        await Page.Locator("input[type='password'], input[name='password']").First.FillAsync(testPassword);
        await Page.Locator("input[type='password'], input[name='password']").Nth(1).FillAsync(testPassword);
        
        // Submit and wait for URL change
        await Page.Locator("button[type='submit']").ClickAsync();
        await Page.WaitForURLAsync(new Regex(".*/app/journal|.*/registration-success"), new() { Timeout = 15000 });

        // Logout - use JavaScript click to bypass viewport restrictions
        var logoutButton = Page.Locator("button:has-text('Logout'), a:has-text('Logout'), button:has-text('Wyloguj')").First;
        await logoutButton.EvaluateAsync("element => element.click()");
        await Page.WaitForURLAsync(new Regex(".*/login|/$"), new() { Timeout = 10000 });

        // Act - Login with existing user
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("input[type='email'], input[name='email']").FillAsync(testEmail);
        await Page.Locator("input[type='password'], input[name='password']").FillAsync(testPassword);
        
        // Submit and wait for URL change
        await Page.Locator("button[type='submit']").ClickAsync();
        await Page.WaitForURLAsync(new Regex(".*/app/journal"), new() { Timeout = 15000 });

        // Assert - Verify successful login and redirect

        // Verify entries page is accessible
        var entriesContent = Page.Locator("h1, h2, h3").Filter(new() { HasTextRegex = new Regex("entries|journal|dziennik", RegexOptions.IgnoreCase) });
        await Expect(entriesContent.First).ToBeVisibleAsync();

        // Verify welcome entry persists (content is in Polish)
        var welcomeEntry = Page.Locator("text=/Witaj w 10xJournal/i");
        await Expect(welcomeEntry).ToBeVisibleAsync();

        // Verify navigation works (can access profile or other authenticated areas)
        var navigationLinks = Page.Locator("a[href*='profile'], a[href*='settings'], a[href*='create']");
        await Expect(navigationLinks.First).ToBeVisibleAsync();

        Logger.LogInformation("✅ Login test passed for user: {Email}", testEmail);
    }

    /// <summary>
    /// Critical Test 3: Verifies that invalid login credentials show appropriate error messages.
    /// Tests error handling and user feedback for authentication failures.
    /// </summary>
    [Fact]
    public async Task Login_WithInvalidCredentials_ShowsErrorMessage()
    {
        // Arrange
        var invalidEmail = "nonexistent@test.com";
        var invalidPassword = "WrongPassword123!";

        Logger.LogInformation("Testing invalid login with credentials: {Email}", invalidEmail);

        // Act - Navigate to login page
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Enter invalid credentials
        await Page.Locator("input[type='email'], input[name='email']").FillAsync(invalidEmail);
        await Page.Locator("input[type='password'], input[name='password']").FillAsync(invalidPassword);

        // Submit the form
        await Page.Locator("button[type='submit']").ClickAsync();

        // Assert - Verify error message appears
        // Look for role="alert" element which should contain the error message
        // The error message is in Polish: "Nieprawidłowy e-mail lub hasło."
        var errorMessage = Page.Locator("[role='alert']");

        await Expect(errorMessage.First).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Verify user remains on login page (no redirect occurred)
        await Expect(Page).ToHaveURLAsync(new Regex(".*/login"));

        // Verify form is still functional (can be resubmitted)
        var emailInput = Page.Locator("input[type='email'], input[name='email']");
        await Expect(emailInput).ToBeEnabledAsync();

        Logger.LogInformation("✅ Invalid login error handling test passed");
        
        // No cleanup needed - no data created
    }
}
