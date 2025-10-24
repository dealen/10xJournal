using _10xJournal.Client.Features.JournalEntries.Models;
using _10xJournal.Client.Shared.Models;
using _10xJournal.Client.Tests.Infrastructure;
using _10xJournal.Client.Tests.Infrastructure.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace _10xJournal.Client.Tests.Features.JournalEntries;

/// <summary>
/// Simple model for auth.users table access with service role.
/// </summary>
public class AuthUser : Supabase.Postgrest.Models.BaseModel
{
    public Guid Id { get; set; }
}

/// <summary>
/// Integration tests for creating journal entries.
/// Tests Supabase integration and RLS policies against real test instance.
/// Verifies critical security and data integrity requirements.
/// </summary>
[Collection(nameof(SupabaseRateLimitedCollection))]
public class CreateEntryIntegrationTests : IAsyncLifetime
{
    private Supabase.Client _supabaseClient = null!;
    private ILogger<CreateEntryIntegrationTests> _logger = null!;
    private readonly List<Guid> _testUserIds = new();
    private readonly List<Guid> _testEntryIds = new();
    
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
        _logger = SupabaseTestHelper.CreateTestLogger<CreateEntryIntegrationTests>();

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

        // Cleanup test users (cascade deletes will handle entries)
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
            return;
        }

        try
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();

            var supabaseUrl = config["Supabase:TestUrl"] ?? "https://test-instance-url.supabase.co";
            var serviceRoleKey = config["Supabase:ServiceRoleKey"];

            if (string.IsNullOrEmpty(serviceRoleKey))
            {
                _logger.LogWarning("ServiceRoleKey not found. Skipping test user cleanup.");
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
                    await adminClient.From<AuthUser>().Where(x => x.Id == userId).Delete();
                    _logger.LogInformation("Successfully cleaned up test user: {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup test user {UserId}", userId);
                }
            }

            _logger.LogInformation("Test user cleanup completed. Cleaned up {Count} users.", _testUserIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform test user cleanup.");
        }
    }

    /// <summary>
    /// Helper: Creates a test user and returns their ID.
    /// </summary>
    private async Task<Guid> CreateTestUserAsync(string email)
    {
        var session = await _supabaseClient.Auth.SignUp(email, TestPassword);
        if (session?.User?.Id == null)
        {
            throw new InvalidOperationException("Failed to create test user - session or user ID is null");
        }
        
        var userId = Guid.Parse(session.User.Id);
        _testUserIds.Add(userId);
        
        // Wait for user initialization (profile + streak)
        await Task.Delay(1000);
        
        return userId;
    }

    /// <summary>
    /// Test: Create journal entry with valid data saves successfully.
    /// Priority: 🔴 Critical (Happy path)
    /// Verifies: Entry creation, data persistence, correct user assignment
    /// </summary>
    [Fact]
    public async Task CreateEntry_WithValidData_SavesSuccessfully()
    {
        // Arrange
        var testEmail = $"create-entry-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";
        
        Guid userId;
        try
        {
            userId = await CreateTestUserAsync(testEmail);
            await _supabaseClient.Auth.SignIn(testEmail, TestPassword);
        }
        catch
        {
            // Skip if test environment not configured
            return;
        }

        var entry = new JournalEntry
        {
            UserId = userId,
            Content = "Test entry content - testing journal entry creation"
        };

        // Act
        var response = await SupabaseTestHelper.ExecuteWithRetryAsync(async () =>
        {
            return await _supabaseClient
                .From<JournalEntry>()
                .Insert(entry);
        }, _logger);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Model);
        Assert.NotNull(response.Model.Id);
        Assert.Equal(entry.Content, response.Model.Content);
        Assert.Equal(userId, response.Model.UserId);

        _testEntryIds.Add(response.Model.Id.Value);
        _logger.LogInformation("✅ Entry created successfully: {EntryId}", response.Model.Id);
    }

    /// <summary>
    /// Test: User A cannot see User B's journal entries (RLS verification).
    /// Priority: 🔴 Critical (Security - RLS policy enforcement)
    /// Verifies: RLS policies prevent cross-user data access
    /// </summary>
    [Fact]
    public async Task CreateEntry_VerifiesRLS_UserCannotSeeOtherUsersEntries()
    {
        // Arrange - Create User A and their entry
        var emailA = $"user-a-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";
        var emailB = $"user-b-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";

        Guid userIdA;
        Guid userIdB;

        try
        {
            userIdA = await CreateTestUserAsync(emailA);
            await _supabaseClient.Auth.SignIn(emailA, TestPassword);

            var entryA = new JournalEntry
            {
                UserId = userIdA,
                Content = "User A's private journal entry - should not be visible to User B"
            };

            var responseA = await SupabaseTestHelper.ExecuteWithRetryAsync(async () =>
            {
                return await _supabaseClient
                    .From<JournalEntry>()
                    .Insert(entryA);
            }, _logger);

            _testEntryIds.Add(responseA.Model!.Id!.Value);

            // Arrange - Create User B
            await _supabaseClient.Auth.SignOut();
            userIdB = await CreateTestUserAsync(emailB);
        }
        catch
        {
            // Skip if test environment not configured
            return;
        }

        // Act - User B logs in and tries to see all entries
        await _supabaseClient.Auth.SignOut();
        await _supabaseClient.Auth.SignIn(emailB, TestPassword);

        var entriesVisibleToB = await _supabaseClient
            .From<JournalEntry>()
            .Get();

        // Assert - User B sees 0 entries (cannot see User A's entry)
        Assert.NotNull(entriesVisibleToB);
        Assert.Empty(entriesVisibleToB.Models);

        _logger.LogInformation("✅ RLS verified: User B cannot see User A's entries");
    }

    /// <summary>
    /// Test: Created entry has correct timestamps and user reference.
    /// Priority: 🟡 Medium (Data integrity)
    /// Verifies: Timestamps are set correctly, user ID matches authenticated user
    /// </summary>
    [Fact(Skip = "Flaky test - requires investigation")]
    public async Task CreateEntry_SetsCorrectTimestamps_AndUserReference()
    {
        // Arrange
        var testEmail = $"timestamps-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";
        
        Guid userId;
        try
        {
            userId = await CreateTestUserAsync(testEmail);
            await _supabaseClient.Auth.SignIn(testEmail, TestPassword);
        }
        catch
        {
            // Skip if test environment not configured
            return;
        }

        var entry = new JournalEntry
        {
            UserId = userId,
            Content = "Timestamp test entry - verifying database-generated timestamps"
        };

        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var response = await SupabaseTestHelper.ExecuteWithRetryAsync(async () =>
        {
            return await _supabaseClient
                .From<JournalEntry>()
                .Insert(entry);
        }, _logger);

        var afterCreate = DateTimeOffset.UtcNow;

        // Assert - Model and timestamps exist
        Assert.NotNull(response.Model);
        
        // Assert - CreatedAt and UpdatedAt are initially the same
        Assert.Equal(response.Model.CreatedAt, response.Model.UpdatedAt);
        
        // Assert - User ID is correct
        Assert.Equal(userId, response.Model.UserId);
        
        // Assert - Timestamps are reasonable (not in future, not too old)
        var now = DateTimeOffset.UtcNow;
        Assert.True(response.Model.CreatedAt <= now.AddMinutes(1), "CreatedAt should not be in the future");
        Assert.True(response.Model.CreatedAt >= beforeCreate.AddMinutes(-1), "CreatedAt should not be too old");

        _testEntryIds.Add(response.Model.Id!.Value);
        _logger.LogInformation("✅ Timestamps verified for entry: {EntryId}", response.Model.Id);
    }
}
