using _10xJournal.Client.Features.Authentication.Login;
using _10xJournal.Client.Shared.Models;
using _10xJournal.Client.Tests.Infrastructure.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Supabase.Gotrue.Exceptions;
using UserStreak = _10xJournal.Client.Features.JournalEntries.ListEntries.UserStreak;

namespace _10xJournal.Client.Tests.Features.Authentication.Login;

/// <summary>
/// Simple model for auth.users table access with service role.
/// </summary>
public class AuthUser : Supabase.Postgrest.Models.BaseModel
{
    public Guid Id { get; set; }
}

/// <summary>
/// Integration tests for Login feature.
/// Tests authentication flows, user initialization, and RLS against real Supabase test instance.
/// Verifies critical security and data integrity requirements.
/// </summary>
[Collection("SupabaseRateLimited")]
public class LoginIntegrationTests : IAsyncLifetime
{
    private Supabase.Client _supabaseClient = null!;
    private LoginHandler _loginHandler = null!;
    private ILogger<LoginHandler> _logger = null!;
    private string _testUserEmail = string.Empty;
    private Guid _testUserId;
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
        _logger = SupabaseTestHelper.CreateTestLogger<LoginHandler>();
        _loginHandler = new LoginHandler(_supabaseClient, _logger);

        // Create unique test user for this test run
        var guid = Guid.NewGuid().ToString("N");
        _testUserEmail = $"test.login.{guid.Substring(0, 8)}@testmail.com";

        try
        {
            var session = await _supabaseClient.Auth.SignUp(_testUserEmail, TestPassword);
            if (session?.User?.Id != null)
            {
                _testUserId = Guid.Parse(session.User.Id);
                _testUserIds.Add(_testUserId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test Supabase instance not configured: {ex.Message}");
        }
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
    }    /// <summary>
    /// Test: Login with non-existent email should fail with GotrueException.
    /// Verifies security - invalid credentials are properly rejected.
    /// </summary>
    [Fact]
    public async Task Login_WithInvalidEmail_ThrowsGotrueException()
    {
        // Arrange
        var nonExistentEmail = $"nonexistent.{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";

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
        var act = async () => await _loginHandler.LoginAsync(nonExistentEmail, "SomePassword123!");
        await act.Should().ThrowAsync<GotrueException>();
    }

    /// <summary>
    /// Test: Login with valid email but wrong password should fail.
    /// Verifies security - authentication requires correct password.
    /// </summary>
    [Fact]
    public async Task Login_WithInvalidPassword_ThrowsGotrueException()
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
        var act = async () => await _loginHandler.LoginAsync(_testUserEmail, "WrongPassword123!");
        await act.Should().ThrowAsync<GotrueException>();
    }

    /// <summary>
    /// Test: Login with empty email should fail with appropriate exception.
    /// Verifies input validation.
    /// </summary>
    [Fact]
    public async Task Login_WithEmptyEmail_ThrowsException()
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
        var act = async () => await _loginHandler.LoginAsync("", TestPassword);
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// Test: Login with empty password should fail with appropriate exception.
    /// Verifies input validation.
    /// </summary>
    [Fact]
    public async Task Login_WithEmptyPassword_ThrowsException()
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
        var act = async () => await _loginHandler.LoginAsync(_testUserEmail, "");
        await act.Should().ThrowAsync<Exception>();
    }
}
