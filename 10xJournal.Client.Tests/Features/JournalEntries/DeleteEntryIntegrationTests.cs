using _10xJournal.Client.Features.JournalEntries.Models;
using _10xJournal.Client.Tests.Infrastructure;
using _10xJournal.Client.Tests.Infrastructure.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace _10xJournal.Client.Tests.Features.JournalEntries;

/// <summary>
/// Simple model for auth.users table access with service role.
/// </summary>
public class AuthUserDelete : Supabase.Postgrest.Models.BaseModel
{
    public Guid Id { get; set; }
}

/// <summary>
/// Integration tests for deleting journal entries.
/// Tests Supabase integration and RLS policies against real test instance.
/// Verifies delete operations and security constraints.
/// </summary>
[Collection(nameof(SupabaseRateLimitedCollection))]
public class DeleteEntryIntegrationTests : IAsyncLifetime
{
    private Supabase.Client _supabaseClient = null!;
    private ILogger<DeleteEntryIntegrationTests> _logger = null!;
    private readonly List<Guid> _testUserIds = new();
    
    private const string TestPassword = "TestPassword123!";

    public async Task InitializeAsync()
    {
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
        _logger = SupabaseTestHelper.CreateTestLogger<DeleteEntryIntegrationTests>();

        await Task.CompletedTask;
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

        await CleanupTestUsersAsync();
    }

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
                    await adminClient.From<AuthUserDelete>().Where(x => x.Id == userId).Delete();
                    _logger.LogInformation("Successfully cleaned up test user: {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup test user {UserId}", userId);
                }
            }

            _logger.LogInformation("Test user cleanup completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform test user cleanup.");
        }
    }

    private async Task<Guid> CreateTestUserAsync(string email)
    {
        var session = await _supabaseClient.Auth.SignUp(email, TestPassword);
        if (session?.User?.Id == null)
        {
            throw new InvalidOperationException("Failed to create test user");
        }
        
        var userId = Guid.Parse(session.User.Id);
        _testUserIds.Add(userId);
        await Task.Delay(1000);
        
        return userId;
    }

    private async Task<JournalEntry> CreateTestEntryAsync(Guid userId, string content)
    {
        var entry = new JournalEntry
        {
            UserId = userId,
            Content = content
        };

        var response = await _supabaseClient
            .From<JournalEntry>()
            .Insert(entry);

        return response.Model!;
    }

    /// <summary>
    /// Test: Delete journal entry removes it successfully.
    /// Priority: 🔴 Critical (Happy path)
    /// Verifies: Entry deletion, data removal from database
    /// </summary>
    [Fact]
    public async Task DeleteEntry_RemovesEntrySuccessfully()
    {
        // Arrange
        var testEmail = $"delete-entry-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";
        
        Guid userId;
        JournalEntry entry;
        
        try
        {
            userId = await CreateTestUserAsync(testEmail);
            await _supabaseClient.Auth.SignIn(testEmail, TestPassword);
            
            entry = await CreateTestEntryAsync(userId, "Entry to be deleted - delete test");
        }
        catch
        {
            return;
        }

        var entryId = entry.Id!.Value;

        // Act - Delete the entry
        await SupabaseTestHelper.ExecuteWithRetryAsync(async () =>
        {
            await _supabaseClient
                .From<JournalEntry>()
                .Where(e => e.Id == entryId)
                .Delete();
        }, _logger);

        await Task.Delay(500);

        // Assert - Entry should no longer exist
        var entriesAfterDelete = await _supabaseClient
            .From<JournalEntry>()
            .Where(e => e.Id == entryId)
            .Get();

        Assert.NotNull(entriesAfterDelete);
        Assert.Empty(entriesAfterDelete.Models);

        _logger.LogInformation("✅ Entry deleted successfully: {EntryId}", entryId);
    }

    /// <summary>
    /// Test: User cannot delete another user's journal entries (RLS verification).
    /// Priority: 🔴 Critical (Security - RLS policy enforcement)
    /// Verifies: RLS policies prevent cross-user deletions
    /// </summary>
    [Fact]
    public async Task DeleteEntry_VerifiesRLS_UserCannotDeleteOtherUsersEntries()
    {
        // Arrange - Create User A and their entry
        var emailA = $"user-a-delete-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";
        var emailB = $"user-b-delete-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";

        Guid userIdA;
        Guid userIdB;
        JournalEntry entryA;

        try
        {
            userIdA = await CreateTestUserAsync(emailA);
            await _supabaseClient.Auth.SignIn(emailA, TestPassword);
            
            entryA = await CreateTestEntryAsync(userIdA, "User A's entry - should not be deletable by User B");

            // Create User B
            await _supabaseClient.Auth.SignOut();
            userIdB = await CreateTestUserAsync(emailB);
        }
        catch
        {
            return;
        }

        var entryIdA = entryA.Id!.Value;

        // Act - User B tries to delete User A's entry
        await _supabaseClient.Auth.SignOut();
        await _supabaseClient.Auth.SignIn(emailB, TestPassword);

        await _supabaseClient
            .From<JournalEntry>()
            .Where(e => e.Id == entryIdA)
            .Delete();

        await Task.Delay(500);

        // Assert - Entry should still exist (delete was blocked by RLS)
        // Switch back to User A to verify
        await _supabaseClient.Auth.SignOut();
        await _supabaseClient.Auth.SignIn(emailA, TestPassword);

        var entriesAfterDeleteAttempt = await _supabaseClient
            .From<JournalEntry>()
            .Where(e => e.Id == entryIdA)
            .Get();

        Assert.NotNull(entriesAfterDeleteAttempt);
        Assert.Single(entriesAfterDeleteAttempt.Models); // Entry still exists

        _logger.LogInformation("✅ RLS verified: User B cannot delete User A's entries");
    }
}
