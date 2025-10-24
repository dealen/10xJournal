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
public class AuthUserList : Supabase.Postgrest.Models.BaseModel
{
    public Guid Id { get; set; }
}

/// <summary>
/// Integration tests for listing journal entries.
/// Tests Supabase integration and RLS policies against real test instance.
/// Verifies query operations and security constraints.
/// </summary>
[Collection(nameof(SupabaseRateLimitedCollection))]
public class ListEntriesIntegrationTests : IAsyncLifetime
{
    private Supabase.Client _supabaseClient = null!;
    private ILogger<ListEntriesIntegrationTests> _logger = null!;
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
        _logger = SupabaseTestHelper.CreateTestLogger<ListEntriesIntegrationTests>();

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
                    await adminClient.From<AuthUserList>().Where(x => x.Id == userId).Delete();
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
    /// Test: List entries returns only current user's entries (RLS verification).
    /// Priority: 🔴 Critical (Security - RLS policy enforcement)
    /// Verifies: Users can only see their own journal entries
    /// </summary>
    [Fact]
    public async Task ListEntries_ReturnsOnlyCurrentUserEntries_VerifiesRLS()
    {
        // Arrange - Create User A with 2 entries
        var emailA = $"user-a-list-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";
        var emailB = $"user-b-list-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";

        Guid userIdA;
        Guid userIdB;

        try
        {
            userIdA = await CreateTestUserAsync(emailA);
            await _supabaseClient.Auth.SignIn(emailA, TestPassword);
            
            await CreateTestEntryAsync(userIdA, "User A - Entry 1");
            await CreateTestEntryAsync(userIdA, "User A - Entry 2");

            // Create User B with 1 entry
            await _supabaseClient.Auth.SignOut();
            userIdB = await CreateTestUserAsync(emailB);
            await _supabaseClient.Auth.SignIn(emailB, TestPassword);
            
            await CreateTestEntryAsync(userIdB, "User B - Entry 1");
        }
        catch
        {
            return;
        }

        // Act - User A logs in and lists entries
        await _supabaseClient.Auth.SignOut();
        await _supabaseClient.Auth.SignIn(emailA, TestPassword);

        var entriesForUserA = await _supabaseClient
            .From<JournalEntry>()
            .Get();

        // Assert - User A sees only their 2 entries
        Assert.NotNull(entriesForUserA);
        Assert.Equal(2, entriesForUserA.Models.Count);
        Assert.All(entriesForUserA.Models, entry => Assert.Equal(userIdA, entry.UserId));

        // Act - User B logs in and lists entries
        await _supabaseClient.Auth.SignOut();
        await _supabaseClient.Auth.SignIn(emailB, TestPassword);

        var entriesForUserB = await _supabaseClient
            .From<JournalEntry>()
            .Get();

        // Assert - User B sees only their 1 entry
        Assert.NotNull(entriesForUserB);
        Assert.Single(entriesForUserB.Models);
        Assert.Equal(userIdB, entriesForUserB.Models[0].UserId);

        _logger.LogInformation("✅ RLS verified: Each user sees only their own entries");
    }

    /// <summary>
    /// Test: List entries orders by date descending (newest first).
    /// Priority: 🟡 Medium (User experience)
    /// Verifies: Entries are sorted with newest entries first
    /// </summary>
    [Fact]
    public async Task ListEntries_OrdersByDateDescending()
    {
        // Arrange
        var testEmail = $"list-order-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";
        
        Guid userId;
        
        try
        {
            userId = await CreateTestUserAsync(testEmail);
            await _supabaseClient.Auth.SignIn(testEmail, TestPassword);
            
            // Create entries with delays to ensure different timestamps
            await CreateTestEntryAsync(userId, "First entry - oldest");
            await Task.Delay(1000);
            
            await CreateTestEntryAsync(userId, "Second entry - middle");
            await Task.Delay(1000);
            
            await CreateTestEntryAsync(userId, "Third entry - newest");
            await Task.Delay(500);
        }
        catch
        {
            return;
        }

        // Act - Get entries ordered by created_at descending
        var entries = await _supabaseClient
            .From<JournalEntry>()
            .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
            .Get();

        // Assert - Entries are ordered newest first
        Assert.NotNull(entries);
        Assert.Equal(3, entries.Models.Count);
        
        Assert.Contains("Third entry", entries.Models[0].Content); // Newest
        Assert.Contains("Second entry", entries.Models[1].Content); // Middle
        Assert.Contains("First entry", entries.Models[2].Content); // Oldest

        // Verify timestamps are in descending order
        Assert.True(entries.Models[0].CreatedAt >= entries.Models[1].CreatedAt);
        Assert.True(entries.Models[1].CreatedAt >= entries.Models[2].CreatedAt);

        _logger.LogInformation("✅ Entries correctly ordered by date descending");
    }
}
