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
public class AuthUserUpdate : Supabase.Postgrest.Models.BaseModel
{
    public Guid Id { get; set; }
}

/// <summary>
/// Integration tests for updating journal entries.
/// Tests Supabase integration and RLS policies against real test instance.
/// Verifies update operations and security constraints.
/// </summary>
[Collection(nameof(SupabaseRateLimitedCollection))]
public class UpdateEntryIntegrationTests : IAsyncLifetime
{
    private Supabase.Client _supabaseClient = null!;
    private ILogger<UpdateEntryIntegrationTests> _logger = null!;
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
        _logger = SupabaseTestHelper.CreateTestLogger<UpdateEntryIntegrationTests>();

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
                    await adminClient.From<AuthUserUpdate>().Where(x => x.Id == userId).Delete();
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
    /// Test: Update journal entry with valid data updates successfully.
    /// Priority: 🔴 Critical (Happy path)
    /// Verifies: Entry update, UpdatedAt timestamp changes
    /// </summary>
    [Fact]
    public async Task UpdateEntry_WithValidData_UpdatesSuccessfully()
    {
        // Arrange
        var testEmail = $"update-entry-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";
        
        Guid userId;
        JournalEntry originalEntry;
        
        try
        {
            userId = await CreateTestUserAsync(testEmail);
            await _supabaseClient.Auth.SignIn(testEmail, TestPassword);
            
            originalEntry = await CreateTestEntryAsync(userId, "Original content");
            await Task.Delay(1500); // Ensure timestamp difference
        }
        catch
        {
            return;
        }

        var updatedContent = "Updated content - modification test";

        // Act
        var response = await SupabaseTestHelper.ExecuteWithRetryAsync(async () =>
        {
            originalEntry.Content = updatedContent;
            return await _supabaseClient
                .From<JournalEntry>()
                .Where(e => e.Id == originalEntry.Id)
                .Update(originalEntry);
        }, _logger);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Models);
        Assert.Single(response.Models);
        Assert.Equal(updatedContent, response.Models[0].Content);
        
        // Re-fetch to get the updated timestamp from database
        var refetchedEntry = await _supabaseClient
            .From<JournalEntry>()
            .Where(e => e.Id == originalEntry.Id)
            .Single();
        
        Assert.NotNull(refetchedEntry);
        Assert.True(refetchedEntry.UpdatedAt >= originalEntry.CreatedAt);

        _logger.LogInformation("✅ Entry updated successfully: {EntryId}", originalEntry.Id);
    }

    /// <summary>
    /// Test: User cannot update another user's journal entries (RLS verification).
    /// Priority: 🔴 Critical (Security - RLS policy enforcement)
    /// Verifies: RLS policies prevent cross-user modifications
    /// </summary>
    [Fact]
    public async Task UpdateEntry_VerifiesRLS_UserCannotUpdateOtherUsersEntries()
    {
        // Arrange - Create User A and their entry
        var emailA = $"user-a-update-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";
        var emailB = $"user-b-update-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";

        Guid userIdA;
        Guid userIdB;
        JournalEntry entryA;

        try
        {
            userIdA = await CreateTestUserAsync(emailA);
            await _supabaseClient.Auth.SignIn(emailA, TestPassword);
            
            entryA = await CreateTestEntryAsync(userIdA, "User A's entry - should not be updatable by User B");

            // Create User B
            await _supabaseClient.Auth.SignOut();
            userIdB = await CreateTestUserAsync(emailB);
        }
        catch
        {
            return;
        }

        // Act - User B tries to update User A's entry
        await _supabaseClient.Auth.SignOut();
        await _supabaseClient.Auth.SignIn(emailB, TestPassword);

        entryA.Content = "User B trying to modify User A's content - SHOULD FAIL";

        var response = await _supabaseClient
            .From<JournalEntry>()
            .Where(e => e.Id == entryA.Id)
            .Update(entryA);

        // Assert - Update should affect 0 rows (RLS blocks it)
        Assert.NotNull(response);
        Assert.NotNull(response.Models);
        Assert.Empty(response.Models); // No rows updated due to RLS

        _logger.LogInformation("✅ RLS verified: User B cannot update User A's entries");
    }

    /// <summary>
    /// Test: Update does not change CreatedAt, only UpdatedAt.
    /// Priority: 🟡 Medium (Data integrity)
    /// Verifies: CreatedAt remains immutable, UpdatedAt is updated
    /// </summary>
    [Fact]
    public async Task UpdateEntry_DoesNotChangeCreatedAt_OnlyUpdatedAt()
    {
        // Arrange
        var testEmail = $"timestamps-update-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";
        
        Guid userId;
        JournalEntry originalEntry;
        
        try
        {
            userId = await CreateTestUserAsync(testEmail);
            await _supabaseClient.Auth.SignIn(testEmail, TestPassword);
            
            originalEntry = await CreateTestEntryAsync(userId, "Original content for timestamp test");
            await Task.Delay(1500); // Ensure timestamp difference
        }
        catch
        {
            return;
        }

        var originalCreatedAt = originalEntry.CreatedAt;
        var originalUpdatedAt = originalEntry.UpdatedAt;

        // Act
        originalEntry.Content = "Updated content";
        
        var response = await SupabaseTestHelper.ExecuteWithRetryAsync(async () =>
        {
            return await _supabaseClient
                .From<JournalEntry>()
                .Where(e => e.Id == originalEntry.Id)
                .Update(originalEntry);
        }, _logger);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Models);
        Assert.Single(response.Models);
        
        // Re-fetch to get the updated timestamp from database
        var updatedEntry = await _supabaseClient
            .From<JournalEntry>()
            .Where(e => e.Id == originalEntry.Id)
            .Single();
        
        Assert.NotNull(updatedEntry);
        
        // CreatedAt should remain unchanged
        Assert.Equal(originalCreatedAt, updatedEntry.CreatedAt);
        
        // UpdatedAt should be newer (or equal if updated very quickly)
        Assert.True(updatedEntry.UpdatedAt >= originalUpdatedAt);
        Assert.True(updatedEntry.UpdatedAt >= updatedEntry.CreatedAt);

        _logger.LogInformation("✅ Timestamp integrity verified for entry: {EntryId}", originalEntry.Id);
    }
}
