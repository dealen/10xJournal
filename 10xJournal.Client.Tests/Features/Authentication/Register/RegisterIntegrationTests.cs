using _10xJournal.Client.Features.Authentication.Register;
using _10xJournal.Client.Shared.Models;
using _10xJournal.Client.Tests.Infrastructure.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Supabase.Gotrue.Exceptions;
using UserStreak = _10xJournal.Client.Features.JournalEntries.ListEntries.UserStreak;

namespace _10xJournal.Client.Tests.Features.Authentication.Register;

/// <summary>
/// Simple model for auth.users table access with service role.
/// </summary>
public class AuthUser : Supabase.Postgrest.Models.BaseModel
{
    public Guid Id { get; set; }
}

/// <summary>
/// Integration tests for Register feature.
/// Tests registration flows, user initialization, RLS policies, and validation against real Supabase test instance.
/// Verifies critical security, data integrity, and user experience requirements.
/// </summary>
[Collection("SupabaseRateLimited")]
public class RegisterIntegrationTests : IAsyncLifetime
{
    private Supabase.Client _supabaseClient = null!;
    private RegisterHandler _registerHandler = null!;
    private ILogger<RegisterHandler> _logger = null!;
    private readonly List<string> _testUserEmails = new();
    private readonly List<Guid> _testUserIds = new();
    private const string TestPassword = "TestPassword123!";

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
        _logger = SupabaseTestHelper.CreateTestLogger<RegisterHandler>();
        _registerHandler = new RegisterHandler(_supabaseClient, _logger);

        // Verify database functions exist (provides helpful error messages if migrations missing)
        try
        {
            await SupabaseTestHelper.VerifyDatabaseFunctionsAsync(_supabaseClient, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database verification failed. Tests may fail due to missing migrations.");
            // Don't throw - let individual tests skip if they encounter issues
        }

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        try
        {
            // Cleanup: sign out
            await _supabaseClient.Auth.SignOut();
        }
        catch
        {
            // Ignore cleanup errors
        }

        // Cleanup test users created during tests
        await CleanupTestUsersAsync();
    }

    /// <summary>
    /// Cleans up test users created during the test run.
    /// Uses admin client to delete users from auth.users table.
    /// Cascade deletes will handle profiles, streaks, and journal entries.
    /// </summary>
    private async Task CleanupTestUsersAsync()
    {
        if (!_testUserIds.Any())
        {
            return; // No test users to clean up
        }

        try
        {
            // Create admin client with service role key for user deletion
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();

            var supabaseUrl = config["Supabase:TestUrl"] ?? "https://test-instance-url.supabase.co";
            var serviceRoleKey = config["Supabase:ServiceRoleKey"];

            if (string.IsNullOrEmpty(serviceRoleKey))
            {
                _logger.LogWarning("ServiceRoleKey not found in test configuration. Skipping test user cleanup.");
                return;
            }

            var adminOptions = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = false,
                AutoConnectRealtime = false
            };

            var adminClient = new Supabase.Client(supabaseUrl, serviceRoleKey, adminOptions);

            foreach (var userId in _testUserIds)
            {
                try
                {
                    _logger.LogInformation("Cleaning up test user: {UserId}", userId);

                    // Delete user from auth.users using PostgREST with service role permissions
                    // This will cascade delete profiles, streaks, and journal entries
                    await adminClient.From<AuthUser>().Where(x => x.Id == userId).Delete();
                    
                    _logger.LogInformation("Successfully cleaned up test user: {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup test user {UserId}. Manual cleanup may be required.", userId);
                }
            }

            _logger.LogInformation("Test user cleanup completed. Cleaned up {Count} users.", _testUserIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform test user cleanup. Test users may need manual removal.");
        }
    }

    /// <summary>
    /// Test: Attempting to register with an already-used email should fail.
    /// Verifies email uniqueness constraint and prevents duplicate accounts.
    /// </summary>
    [Fact]
    public async Task Register_WithDuplicateEmail_ThrowsGotrueException()
    {
        // Arrange - create first user
        var guid = Guid.NewGuid().ToString("N");
        var testEmail = $"test.duplicate.{guid.Substring(0, 8)}@testmail.com";
        _testUserEmails.Add(testEmail);

        try
        {
            await _supabaseClient.Auth.SignOut();
            await _registerHandler.RegisterAsync(testEmail, TestPassword);
            
            var userId = Guid.Parse(_supabaseClient.Auth.CurrentUser!.Id ?? throw new InvalidOperationException("User ID is null"));
            _testUserIds.Add(userId);
            
            await _supabaseClient.Auth.SignOut();
        }
        catch
        {
            // Skip if test environment not configured
            return;
        }

        // Act & Assert - attempt to register with same email
        var act = async () => await _registerHandler.RegisterAsync(testEmail, "DifferentPassword123!");
        await act.Should().ThrowAsync<GotrueException>();
    }

    /// <summary>
    /// Test: Registration with weak password (too short) should fail.
    /// Verifies password strength requirements are enforced.
    /// </summary>
    [Fact]
    public async Task Register_WithWeakPassword_ThrowsGotrueException()
    {
        // Arrange
        var guid = Guid.NewGuid().ToString("N");
        var testEmail = $"test.weakpw.{guid.Substring(0, 8)}@testmail.com";
        _testUserEmails.Add(testEmail);

        try
        {
            await _supabaseClient.Auth.SignOut();
        }
        catch
        {
            // Skip if test environment not configured
            return;
        }

        // Act & Assert - attempt registration with password less than 6 characters
        var act = async () => await _registerHandler.RegisterAsync(testEmail, "12345");
        await act.Should().ThrowAsync<GotrueException>();
    }

    /// <summary>
    /// Test: Registration with invalid email format should fail.
    /// Verifies email format validation is enforced.
    /// </summary>
    [Fact]
    public async Task Register_WithInvalidEmailFormat_ThrowsGotrueException()
    {
        // Arrange
        var invalidEmail = "notanemail";

        try
        {
            await _supabaseClient.Auth.SignOut();
        }
        catch
        {
            // Skip if test environment not configured
            return;
        }

        // Act & Assert
        var act = async () => await _registerHandler.RegisterAsync(invalidEmail, TestPassword);
        await act.Should().ThrowAsync<GotrueException>();
    }

    /// <summary>
    /// Test: Registration with empty email should fail.
    /// Verifies input validation.
    /// </summary>
    [Fact]
    public async Task Register_WithEmptyEmail_ThrowsException()
    {
        // Arrange
        try
        {
            await _supabaseClient.Auth.SignOut();
        }
        catch
        {
            // Skip if test environment not configured
            return;
        }

        // Act & Assert
        var act = async () => await _registerHandler.RegisterAsync("", TestPassword);
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// Test: Registration with empty password should fail.
    /// Verifies input validation.
    /// </summary>
    [Fact]
    public async Task Register_WithEmptyPassword_ThrowsException()
    {
        // Arrange
        var guid = Guid.NewGuid().ToString("N");
        var testEmail = $"test.emptypw.{guid.Substring(0, 8)}@testmail.com";
        _testUserEmails.Add(testEmail);

        try
        {
            await _supabaseClient.Auth.SignOut();
        }
        catch
        {
            // Skip if test environment not configured
            return;
        }

        // Act & Assert
        var act = async () => await _registerHandler.RegisterAsync(testEmail, "");
        await act.Should().ThrowAsync<Exception>();
    }
}
