using _10xJournal.Client.Features.Settings.ChangePassword.Models;
using _10xJournal.Client.Tests.Infrastructure.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Supabase.Gotrue.Exceptions;
using Xunit;

namespace _10xJournal.Client.Tests.Features.Settings;

/// <summary>
/// Integration tests for ChangePassword feature.
/// Tests password change flows against real Supabase test instance.
/// Verifies authentication, error handling, and password update functionality.
/// </summary>
[Collection("SupabaseRateLimited")]
public class ChangePasswordIntegrationTests : IAsyncLifetime
{
    private Supabase.Client _supabaseClient = null!;
    private string _testUserEmail = string.Empty;
    private Guid _testUserId;
    private const string InitialPassword = "TestPassword123!";
    private const string NewPassword = "NewPassword456!";

    public async Task InitializeAsync()
    {
        // Load test configuration
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .Build();

        var supabaseUrl = config["Supabase:TestUrl"] ?? "https://test-instance-url.supabase.co";
        var supabaseKey = config["Supabase:TestKey"] ?? "test-key";

        var options = new Supabase.SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        };

        _supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey, options);

        // Create unique test user
        var guid = Guid.NewGuid().ToString("N");
        _testUserEmail = $"test.changepw.{guid.Substring(0, 8)}@testmail.com";

        try
        {
            var signUpResult = await _supabaseClient.Auth.SignUp(_testUserEmail, InitialPassword);
            if (signUpResult?.User?.Id != null)
            {
                _testUserId = Guid.Parse(signUpResult.User.Id);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test Supabase instance not configured: {ex.Message}");
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _supabaseClient.Auth.SignOut();
        }
        catch
        {
            // Ignore cleanup errors
        }

        // Cleanup test user using service role
        await CleanupTestUserAsync();
    }

    /// <summary>
    /// Cleans up the test user created during initialization.
    /// Uses admin client to delete user from auth.users table.
    /// Cascade deletes will handle profiles, streaks, and journal entries.
    /// </summary>
    private async Task CleanupTestUserAsync()
    {
        if (_testUserId == Guid.Empty)
        {
            return; // No test user to clean up
        }

        try
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();

            var serviceRoleKey = config["Supabase:ServiceRoleKey"];

            if (string.IsNullOrEmpty(serviceRoleKey))
            {
                // Can't clean up without service role key
                return;
            }

            var supabaseUrl = config["Supabase:TestUrl"] ?? "https://test-instance-url.supabase.co";
            var adminOptions = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = false,
                AutoConnectRealtime = false
            };

            var adminClient = new Supabase.Client(supabaseUrl, serviceRoleKey, adminOptions);
            await adminClient.InitializeAsync();

            // Delete user using HTTP REST API to auth endpoint
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {serviceRoleKey}");

            var deleteUrl = $"{supabaseUrl}/auth/v1/admin/users/{_testUserId}";
            await httpClient.DeleteAsync(deleteUrl);
        }
        catch (Exception ex)
        {
            // Log cleanup failure but don't throw - we're in cleanup phase
            Console.WriteLine($"Warning: Failed to cleanup test user: {ex.Message}");
        }
    }

    /// <summary>
    /// Test: User can successfully change password and login with new credentials.
    /// This is the primary happy path scenario.
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithValidNewPassword_UpdatesPasswordSuccessfully()
    {
        // Arrange - Login with initial password
        try
        {
            await _supabaseClient.Auth.SignIn(_testUserEmail, InitialPassword);
        }
        catch
        {
            // Skip if test environment not configured
            return;
        }

        // Act - Change password using Supabase Auth API
        await _supabaseClient.Auth.Update(new Supabase.Gotrue.UserAttributes
        {
            Password = NewPassword
        });

        // Logout to verify new password
        await _supabaseClient.Auth.SignOut();

        // Assert - Can login with new password
        var loginResult = await _supabaseClient.Auth.SignIn(_testUserEmail, NewPassword);
        loginResult.Should().NotBeNull();
        loginResult.User.Should().NotBeNull();
        loginResult.User!.Email.Should().Be(_testUserEmail);
    }

    /// <summary>
    /// Test: After changing password, old password no longer works.
    /// Verifies that password change invalidates old credentials.
    /// </summary>
    [Fact]
    public async Task ChangePassword_AfterChange_OldPasswordNoLongerWorks()
    {
        // Arrange - Login and change password
        try
        {
            await _supabaseClient.Auth.SignIn(_testUserEmail, InitialPassword);
        }
        catch
        {
            return;
        }

        await _supabaseClient.Auth.Update(new Supabase.Gotrue.UserAttributes
        {
            Password = NewPassword
        });

        await _supabaseClient.Auth.SignOut();

        // Act & Assert - Old password should fail
        var loginAttempt = async () => await _supabaseClient.Auth.SignIn(_testUserEmail, InitialPassword);
        
        await loginAttempt.Should().ThrowAsync<GotrueException>()
            .WithMessage("*Invalid login credentials*");
    }

    /// <summary>
    /// Test: Weak passwords are rejected by Supabase.
    /// Verifies password strength requirements are enforced.
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithWeakPassword_FailsValidation()
    {
        // Arrange - Login with valid credentials
        try
        {
            await _supabaseClient.Auth.SignIn(_testUserEmail, InitialPassword);
        }
        catch
        {
            return;
        }

        // Act & Assert - Weak password should be rejected
        var weakPassword = "12345"; // Too short
        var updateAttempt = async () => await _supabaseClient.Auth.Update(
            new Supabase.Gotrue.UserAttributes { Password = weakPassword });

        await updateAttempt.Should().ThrowAsync<GotrueException>();
    }

    /// <summary>
    /// Test: Unauthenticated users cannot change password.
    /// Verifies authentication is required for password changes.
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithoutAuthentication_Fails()
    {
        // Arrange - Ensure user is logged out
        try
        {
            await _supabaseClient.Auth.SignOut();
        }
        catch
        {
            // Already logged out
        }

        // Act & Assert - Password change should fail without authentication
        var updateAttempt = async () => await _supabaseClient.Auth.Update(
            new Supabase.Gotrue.UserAttributes { Password = NewPassword });

        await updateAttempt.Should().ThrowAsync<GotrueException>();
    }
}
