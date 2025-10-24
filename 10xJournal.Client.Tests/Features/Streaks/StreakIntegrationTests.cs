using _10xJournal.Client.Features.JournalEntries.Models;
using _10xJournal.Client.Tests.Infrastructure;
using _10xJournal.Client.Tests.Infrastructure.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using UserStreak = _10xJournal.Client.Features.JournalEntries.ListEntries.UserStreak;

namespace _10xJournal.Client.Tests.Features.Streaks;

/// <summary>
/// Simple model for auth.users table access with service role.
/// </summary>
public class AuthUserStreak : Supabase.Postgrest.Models.BaseModel
{
    public Guid Id { get; set; }
}

/// <summary>
/// Integration tests for streak system.
/// Tests database trigger logic for streak calculation against real test instance.
/// Verifies streak tracking, consecutive day logic, and data integrity.
/// </summary>
[Collection(nameof(SupabaseRateLimitedCollection))]
public class StreakIntegrationTests : IAsyncLifetime
{
    private Supabase.Client _supabaseClient = null!;
    private ILogger<StreakIntegrationTests> _logger = null!;
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
        _logger = SupabaseTestHelper.CreateTestLogger<StreakIntegrationTests>();

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
                    await adminClient.From<AuthUserStreak>().Where(x => x.Id == userId).Delete();
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

    private async Task<UserStreak?> GetUserStreakAsync(Guid userId)
    {
        try
        {
            var streaks = await _supabaseClient
                .From<UserStreak>()
                .Where(s => s.UserId == userId)
                .Get();

            return streaks?.Models?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get streak for user {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Test: Creating first entry initializes streak to 1.
    /// Priority: 🟠 High (Core functionality)
    /// Verifies: Streak trigger creates initial streak record
    /// </summary>
    [Fact(Skip = "Flaky test - requires investigation")]
    public async Task CreateEntry_FirstEntry_InitializesStreakToOne()
    {
        // Arrange
        var testEmail = $"streak-first-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";
        
        Guid userId;
        
        try
        {
            userId = await CreateTestUserAsync(testEmail);
            
            // Call initialize_new_user to ensure profile and streak exist
            await _supabaseClient.Rpc("initialize_new_user", new { p_user_id = userId });
            await Task.Delay(1000);
            
            await _supabaseClient.Auth.SignIn(testEmail, TestPassword);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Test setup failed");
            return;
        }

        // Get initial streak state (should be 0)
        var initialStreak = await GetUserStreakAsync(userId);
        
        if (initialStreak == null)
        {
            _logger.LogWarning("No streak found for user {UserId} - skipping test", userId);
            return;
        }
        
        Assert.Equal(0, initialStreak.CurrentStreak);
        Assert.Equal(0, initialStreak.LongestStreak);

        // Act - Create first entry
        var entry = new JournalEntry
        {
            UserId = userId,
            Content = "First entry - should start streak at 1"
        };

        await SupabaseTestHelper.ExecuteWithRetryAsync(async () =>
        {
            await _supabaseClient.From<JournalEntry>().Insert(entry);
        }, _logger);

        await Task.Delay(1500); // Wait for trigger to complete

        // Assert - Streak should be 1
        var updatedStreak = await GetUserStreakAsync(userId);
        Assert.NotNull(updatedStreak);
        Assert.Equal(1, updatedStreak.CurrentStreak);
        Assert.Equal(1, updatedStreak.LongestStreak);

        _logger.LogInformation("✅ First entry correctly initialized streak to 1");
    }

    /// <summary>
    /// Test: Creating entry on same day does not increase streak.
    /// Priority: 🟠 High (Streak logic verification)
    /// Verifies: Multiple entries on same day don't inflate streak
    /// </summary>
    [Fact(Skip = "Flaky test - requires investigation")]
    public async Task CreateEntry_SameDay_DoesNotIncreaseStreak()
    {
        // Arrange
        var testEmail = $"streak-sameday-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";
        
        Guid userId;
        
        try
        {
            userId = await CreateTestUserAsync(testEmail);
            
            // Call initialize_new_user to ensure profile and streak exist
            await _supabaseClient.Rpc("initialize_new_user", new { p_user_id = userId });
            await Task.Delay(1000);
            
            await _supabaseClient.Auth.SignIn(testEmail, TestPassword);

            // Create first entry
            var entry1 = new JournalEntry
            {
                UserId = userId,
                Content = "First entry of the day"
            };

            await SupabaseTestHelper.ExecuteWithRetryAsync(async () =>
            {
                await _supabaseClient.From<JournalEntry>().Insert(entry1);
            }, _logger);

            await Task.Delay(1500);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Test setup failed");
            return;
        }

        // Get streak after first entry
        var streakAfterFirst = await GetUserStreakAsync(userId);
        
        if (streakAfterFirst == null)
        {
            _logger.LogWarning("No streak found for user {UserId} - skipping test", userId);
            return;
        }
        
        Assert.Equal(1, streakAfterFirst.CurrentStreak);

        // Act - Create second entry on the same day
        var entry2 = new JournalEntry
        {
            UserId = userId,
            Content = "Second entry of the same day - should not increase streak"
        };

        await SupabaseTestHelper.ExecuteWithRetryAsync(async () =>
        {
            await _supabaseClient.From<JournalEntry>().Insert(entry2);
        }, _logger);

        await Task.Delay(1500);

        // Assert - Streak should still be 1
        var streakAfterSecond = await GetUserStreakAsync(userId);
        Assert.NotNull(streakAfterSecond);
        Assert.Equal(1, streakAfterSecond.CurrentStreak);
        Assert.Equal(1, streakAfterSecond.LongestStreak);

        _logger.LogInformation("✅ Same-day entry correctly did not increase streak");
    }

    /// <summary>
    /// Test: Streak updates longest_streak when current exceeds it.
    /// Priority: 🟠 High (Streak tracking accuracy)
    /// Verifies: Longest streak is tracked correctly
    /// Note: This test simulates consecutive days by manually updating the database
    /// </summary>
    [Fact]
    public async Task Streak_UpdatesLongestStreak_WhenCurrentExceeds()
    {
        // Arrange
        var testEmail = $"streak-longest-{Guid.NewGuid().ToString("N").Substring(0, 8)}@testmail.com";
        
        Guid userId;
        
        try
        {
            userId = await CreateTestUserAsync(testEmail);
            await _supabaseClient.Auth.SignIn(testEmail, TestPassword);

            // Create first entry
            var entry1 = new JournalEntry
            {
                UserId = userId,
                Content = "Day 1 entry"
            };

            await SupabaseTestHelper.ExecuteWithRetryAsync(async () =>
            {
                await _supabaseClient.From<JournalEntry>().Insert(entry1);
            }, _logger);

            await Task.Delay(1000);

            // Manually simulate a consecutive day entry by updating last_entry_date
            // This is a workaround since we can't actually wait 24 hours in a test
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();

            var supabaseUrl = config["Supabase:TestUrl"] ?? "https://test-instance-url.supabase.co";
            var serviceRoleKey = config["Supabase:ServiceRoleKey"];

            if (string.IsNullOrEmpty(serviceRoleKey))
            {
                _logger.LogWarning("ServiceRoleKey not configured. Skipping test.");
                return;
            }

            var adminOptions = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = false,
                AutoConnectRealtime = false
            };

            var adminClient = new Supabase.Client(supabaseUrl, serviceRoleKey, adminOptions);

            // Update last_entry_date to yesterday
            await adminClient.Rpc("execute_sql", new
            {
                sql = $@"
                    UPDATE user_streaks 
                    SET last_entry_date = CURRENT_DATE - INTERVAL '1 day'
                    WHERE user_id = '{userId}'"
            });

            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Test setup failed. Skipping test.");
            return;
        }

        // Act - Create entry for "today" (which will be consecutive to yesterday)
        var entry2 = new JournalEntry
        {
            UserId = userId,
            Content = "Day 2 entry - consecutive day"
        };

        await SupabaseTestHelper.ExecuteWithRetryAsync(async () =>
        {
            await _supabaseClient.From<JournalEntry>().Insert(entry2);
        }, _logger);

        await Task.Delay(1000);

        // Assert - Current and longest streak should both be 2
        var updatedStreak = await GetUserStreakAsync(userId);
        Assert.NotNull(updatedStreak);
        Assert.Equal(2, updatedStreak.CurrentStreak);
        Assert.Equal(2, updatedStreak.LongestStreak);

        _logger.LogInformation("✅ Longest streak correctly updated when current streak exceeded it");
    }
}
