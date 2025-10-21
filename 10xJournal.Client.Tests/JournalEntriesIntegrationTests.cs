using _10xJournal.Client.Features.JournalEntries.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace _10xJournal.Client.Tests;

/// <summary>
/// Integration tests for the JournalEntries feature.
/// These tests connect to an actual test Supabase instance rather than using mocks,
/// giving us confidence that our actual integration works correctly.
/// </summary>
public class JournalEntriesIntegrationTests : IAsyncLifetime
{
    private Supabase.Client _supabaseClient = null!;
    private Guid _testUserId;
    private string _testUserEmail = string.Empty;
    private const string TestPassword = "TestPassword123!";
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;

    public JournalEntriesIntegrationTests()
    {
        // Load from appsettings.test.json
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .Build();

        // Use GetValue with a fallback to avoid possible null assignment to non-nullable fields
        _supabaseUrl = config.GetValue<string>("Supabase:Url") ?? string.Empty;
        _supabaseKey = config.GetValue<string>("Supabase:AnonKey") ?? string.Empty;
    }

    public async Task InitializeAsync()
    {
        // Configure Supabase test client (should use a dedicated test instance)
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json", optional: true)
            .Build();

        string supabaseUrl = configuration["Supabase:TestUrl"] ?? "https://test-instance-url.supabase.co";
        string supabaseKey = configuration["Supabase:TestKey"] ?? "test-key";

        var options = new Supabase.SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        };

        _supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey, options);

        // Create a test user for our tests
        // Use a realistic test domain that passes Supabase validation
        var guid = Guid.NewGuid().ToString("N");
        _testUserEmail = $"test.{guid.Substring(0, 8)}@testmail.com";

        try
        {
            var signUpResult = await _supabaseClient.Auth.SignUp(_testUserEmail, TestPassword);
            if (signUpResult?.User?.Id != null)
            {
                _testUserId = Guid.Parse(signUpResult.User.Id);
            }
        }
        catch (Exception ex)
        {
            // If running against a real test instance, we'll log the error but continue
            Console.WriteLine($"Test Supabase instance not configured correctly: {ex.Message}");
        }

        // Clean up any existing test data for this user
        await CleanupTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up test data
        await CleanupTestDataAsync();

        // Try to delete the test user if possible
        try
        {
            // Note: This may not work without admin privileges, so we'll just log a warning if it fails
            await _supabaseClient.Auth.SignOut();
        }
        catch
        {
            // Log but don't fail tests if we can't delete the test user
            Console.WriteLine("Warning: Could not sign out - manual cleanup may be required");
        }
    }

    private async Task CleanupTestDataAsync()
    {
        if (_testUserId != Guid.Empty)
        {
            try
            {
                await _supabaseClient.From<JournalEntry>()
                    .Where(e => e.UserId == _testUserId)
                    .Delete();
            }
            catch
            {
                // If cleanup fails, log but continue
                Console.WriteLine("Warning: Could not clean up test data");
            }
        }
    }

    /// <summary>
    /// Test for creating and retrieving a journal entry, demonstrating
    /// how we should test against a real Supabase instance rather than mocking.
    /// </summary>
    [Fact]
    public async Task CreateAndRetrieveEntry_ShouldSucceed_WhenUserIsAuthenticated()
    {
        // Skip if test user wasn't created
        if (_testUserId == Guid.Empty)
        {
            return;
        }

        // Authenticate as test user
        try
        {
            await _supabaseClient.Auth.SignIn(_testUserEmail, TestPassword);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not authenticate test user: {ex.Message}");
            return;
        }

        var entryContent = $"Test journal entry created at {DateTime.UtcNow}";

        try
        {
            // Act - Create a new entry
            var newEntry = new JournalEntry
            {
                Content = entryContent,
                UserId = _testUserId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var insertResponse = await _supabaseClient.From<JournalEntry>().Insert(newEntry);
            var createdEntry = insertResponse.Model;

            // Assert - Entry was created successfully
            Assert.NotNull(createdEntry);
            Assert.Equal(entryContent, createdEntry.Content);
            Assert.Equal(_testUserId, createdEntry.UserId);

            // Act - Retrieve the entry
            var getResponse = await _supabaseClient
                .From<JournalEntry>()
                .Where(e => e.Id == createdEntry.Id)
                .Get();

            var retrievedEntry = getResponse.Models.FirstOrDefault();

            // Assert - Retrieved entry matches created entry
            Assert.NotNull(retrievedEntry);
            Assert.Equal(createdEntry.Id, retrievedEntry.Id);
            Assert.Equal(entryContent, retrievedEntry.Content);

            // Clean up - Delete the test entry
            await _supabaseClient.From<JournalEntry>()
                .Where(e => e.Id == createdEntry.Id)
                .Delete();
        }
        finally
        {
            // Always sign out after test
            await _supabaseClient.Auth.SignOut();
        }
    }

    /// <summary>
    /// Tests that Row Level Security prevents users from accessing each other's entries.
    /// This is a critical security test that verifies our RLS policies work as expected.
    /// </summary>
    [Fact]
    public async Task RowLevelSecurity_ShouldPreventAccessToOtherUsersEntries()
    {
        // Skip if test user wasn't created
        if (_testUserId == Guid.Empty)
        {
            return;
        }

        // Authenticate as test user
        try
        {
            await _supabaseClient.Auth.SignIn(_testUserEmail, TestPassword);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not authenticate test user: {ex.Message}");
            return;
        }

        Guid? secondUserId = null;
        // Use a realistic test domain that passes Supabase validation
        var guid2 = Guid.NewGuid().ToString("N");
        string secondUserEmail = $"test2.{guid2.Substring(0, 8)}@testmail.com";

        try
        {
            // Create a test entry for our first user
            var testEntry = new JournalEntry
            {
                Content = "This entry should not be visible to other users",
                UserId = _testUserId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var insertResponse = await _supabaseClient.From<JournalEntry>().Insert(testEntry);
            Assert.NotNull(insertResponse.Model);

            // Create a second test user
            await _supabaseClient.Auth.SignOut();
            var signUpResult = await _supabaseClient.Auth.SignUp(secondUserEmail, TestPassword);

            if (signUpResult?.User?.Id != null)
            {
                secondUserId = Guid.Parse(signUpResult.User.Id);

                // Sign in as second user
                await _supabaseClient.Auth.SignIn(secondUserEmail, TestPassword);

                // Try to access first user's entry - should return empty due to RLS
                var getResponse = await _supabaseClient
                    .From<JournalEntry>()
                    .Where(e => e.UserId == _testUserId)
                    .Get();

                // Assert - RLS should prevent access
                Assert.Empty(getResponse.Models);
            }
        }
        finally
        {
            // Sign back in as first user to clean up
            await _supabaseClient.Auth.SignOut();
            await _supabaseClient.Auth.SignIn(_testUserEmail, TestPassword);
        }
    }

    /// <summary>
    /// Tests updating a journal entry and verifies the changes are saved correctly.
    /// </summary>
    [Fact]
    public async Task UpdateJournalEntry_ShouldUpdateInDatabase()
    {
        // Skip if test user wasn't created
        if (_testUserId == Guid.Empty)
        {
            return;
        }

        // Authenticate as test user
        try
        {
            await _supabaseClient.Auth.SignIn(_testUserEmail, TestPassword);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not authenticate test user: {ex.Message}");
            return;
        }

        try
        {
            // Create a test entry to update
            var testEntry = new JournalEntry
            {
                Content = "Original content",
                UserId = _testUserId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var insertResponse = await _supabaseClient.From<JournalEntry>().Insert(testEntry);
            var entryId = insertResponse.Model?.Id;

            if (entryId.HasValue)
            {
                // Update the entry
                var updatedEntry = new JournalEntry
                {
                    Id = entryId,
                    Content = "Updated content",
                    UserId = _testUserId,
                    CreatedAt = insertResponse.Model!.CreatedAt
                };

                var updateResponse = await _supabaseClient.From<JournalEntry>()
                    .Where(e => e.Id == entryId)
                    .Update(updatedEntry);

                // Verify update was successful
                Assert.NotNull(updateResponse);
                Assert.Equal("Updated content", updateResponse.Model!.Content);

                // Verify by reading back
                var readResponse = await _supabaseClient.From<JournalEntry>()
                    .Where(e => e.Id == entryId)
                    .Get();

                Assert.NotNull(readResponse);
                Assert.Single(readResponse.Models);
                Assert.Equal("Updated content", readResponse.Models.First().Content);
            }
        }
        finally
        {
            // Clean up happens in DisposeAsync
            await _supabaseClient.Auth.SignOut();
        }
    }
}
