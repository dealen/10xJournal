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
    }

    /// <summary>
    /// Test: Successful login with valid credentials authenticates user and creates session.
    /// Priority: 🔴 Critical (Happy path)
    /// Verifies: User authentication, session creation, access token generation
    /// </summary>
    [Fact]
    public async Task Login_WithValidCredentials_AuthenticatesSuccessfully()
    {
        // Arrange - Create a fresh user for this test
        var testEmail = $"login-happy-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";
        
        try
        {
            await _supabaseClient.Auth.SignOut();
            
            // Create user
            var signUpSession = await _supabaseClient.Auth.SignUp(testEmail, TestPassword);
            if (signUpSession?.User?.Id != null)
            {
                var userId = Guid.Parse(signUpSession.User.Id);
                _testUserIds.Add(userId);
            }
            else
            {
                // Skip if test environment not configured
                return;
            }
            
            // Wait for user to be ready
            await Task.Delay(1000);
            
            // Sign out to test login
            await _supabaseClient.Auth.SignOut();
        }
        catch
        {
            // Skip if test environment not configured
            return;
        }

        // Act
        await SupabaseTestHelper.ExecuteWithRetryAsync(async () =>
        {
            await _loginHandler.LoginAsync(testEmail, TestPassword);
        }, _logger);

        // Assert
        var session = _supabaseClient.Auth.CurrentSession;
        Assert.NotNull(session);
        Assert.NotNull(session.User);
        Assert.Equal(testEmail, session.User.Email);
        Assert.NotNull(session.AccessToken);

        _logger.LogInformation("✅ Login successful for user: {Email}", testEmail);
    }

    /// <summary>
    /// Test: First login initializes user profile and streak records.
    /// Priority: 🔴 Critical (User initialization)
    /// Verifies: RPC function initialize_new_user creates profile and streak
    /// Verifies: Idempotency - second login doesn't create duplicates
    /// </summary>
    [Fact]
    public async Task Login_InitializesUserProfileAndStreak_WhenNotExists()
    {
        // Arrange - Create a brand new user
        var testEmail = $"login-init-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";
        Guid newUserId = Guid.Empty;
        
        try
        {
            var signUpSession = await _supabaseClient.Auth.SignUp(testEmail, TestPassword);
            if (signUpSession?.User?.Id != null)
            {
                newUserId = Guid.Parse(signUpSession.User.Id);
                _testUserIds.Add(newUserId);
            }
            else
            {
                // Skip if test environment not configured
                return;
            }

            // Sign out to test login initialization
            await _supabaseClient.Auth.SignOut();
        }
        catch
        {
            // Skip if test environment not configured
            return;
        }

        // Act - First login triggers initialization
        await SupabaseTestHelper.ExecuteWithRetryAsync(async () =>
        {
            await _loginHandler.LoginAsync(testEmail, TestPassword);
        }, _logger);

        await Task.Delay(1000); // Wait for RPC to complete

        // Assert - Verify profile created
        var profiles = await _supabaseClient
            .From<UserProfile>()
            .Where(p => p.Id == newUserId)
            .Get();

        Assert.NotNull(profiles);
        Assert.Single(profiles.Models);

        // Assert - Verify streak created
        var streaks = await _supabaseClient
            .From<UserStreak>()
            .Where(s => s.UserId == newUserId)
            .Get();

        Assert.NotNull(streaks);
        Assert.Single(streaks.Models);
        Assert.Equal(0, streaks.Models[0].CurrentStreak);

        // Assert - Second login doesn't create duplicates (idempotency)
        await _supabaseClient.Auth.SignOut();
        await SupabaseTestHelper.ExecuteWithRetryAsync(async () =>
        {
            await _loginHandler.LoginAsync(testEmail, TestPassword);
        }, _logger);
        
        await Task.Delay(500);

        var profilesAfter = await _supabaseClient
            .From<UserProfile>()
            .Where(p => p.Id == newUserId)
            .Get();

        Assert.Single(profilesAfter.Models);

        var streaksAfter = await _supabaseClient
            .From<UserStreak>()
            .Where(s => s.UserId == newUserId)
            .Get();

        Assert.Single(streaksAfter.Models);

        _logger.LogInformation("✅ User initialization verified for: {Email}", testEmail);
    }

    /// <summary>
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
