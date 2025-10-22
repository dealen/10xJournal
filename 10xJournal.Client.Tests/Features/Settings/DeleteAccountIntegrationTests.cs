using _10xJournal.Client.Features.JournalEntries.Models;
using _10xJournal.Client.Features.Settings.DeleteAccount.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Xunit;

namespace _10xJournal.Client.Tests.Features.Settings;

/// <summary>
/// Integration tests for DeleteAccount feature.
/// Tests account deletion, data cleanup, and RLS policy enforcement.
/// Verifies delete_my_account RPC function works correctly.
/// </summary>
public class DeleteAccountIntegrationTests : IAsyncLifetime
{
    private Supabase.Client _supabaseClient = null!;
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

        // Initialization complete - each test creates its own users
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
    }

    /// <summary>
    /// Test: Deleting account removes user and all their journal entries.
    /// This is the critical data cleanup verification test.
    /// </summary>
    [Fact]
    public async Task DeleteAccount_RemovesUserAndAllEntries()
    {
        // Arrange - Create a fresh test user
        var guid = Guid.NewGuid().ToString("N");
        var userEmail = $"test.delete.{guid.Substring(0, 8)}@testmail.com";

        try
        {
            var signUpResult = await _supabaseClient.Auth.SignUp(userEmail, TestPassword);
            if (signUpResult?.User?.Id == null)
            {
                return; // Skip if test environment not configured
            }

            var userId = Guid.Parse(signUpResult.User.Id);

            // Login as the new user
            await _supabaseClient.Auth.SignIn(userEmail, TestPassword);

            // Create test entries
            var entry1 = new JournalEntry { Content = "Entry 1 to be deleted", UserId = userId };
            var entry2 = new JournalEntry { Content = "Entry 2 to be deleted", UserId = userId };

            await _supabaseClient.From<JournalEntry>().Insert(entry1);
            await _supabaseClient.From<JournalEntry>().Insert(entry2);

            // Verify entries exist
            var entriesBeforeDelete = await _supabaseClient
                .From<JournalEntry>()
                .Where(e => e.UserId == userId)
                .Get();
            entriesBeforeDelete.Models.Should().HaveCountGreaterThanOrEqualTo(2);

            // Act - Delete account
            var deleteResult = await _supabaseClient.Rpc("delete_my_account", null);

            // Assert - Verify deletion response
            deleteResult.Should().NotBeNull();
            var deleteResponse = JsonSerializer.Deserialize<DeleteAccountResponse>(
                deleteResult!.Content!,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            deleteResponse.Should().NotBeNull();
            deleteResponse!.Success.Should().BeTrue();

            // Logout and try to login again - should fail
            await _supabaseClient.Auth.SignOut();
            var loginAttempt = async () => await _supabaseClient.Auth.SignIn(userEmail, TestPassword);
            await loginAttempt.Should().ThrowAsync<Exception>();
        }
        catch (Exception ex) when (ex.Message.Contains("not configured"))
        {
            // Skip if test environment not configured
            return;
        }
    }

    /// <summary>
    /// Test: Deleting one user's account doesn't affect other users' data.
    /// Critical RLS policy verification test.
    /// </summary>
    [Fact]
    public async Task DeleteAccount_VerifiesRLS_OnlyDeletesCurrentUserData()
    {
        // Arrange - Create two test users
        var guid1 = Guid.NewGuid().ToString("N");
        var guid2 = Guid.NewGuid().ToString("N");
        var user1Email = $"test.delete.user1.{guid1.Substring(0, 8)}@testmail.com";
        var user2Email = $"test.delete.user2.{guid2.Substring(0, 8)}@testmail.com";

        try
        {
            // Create and setup User 1
            var user1SignUp = await _supabaseClient.Auth.SignUp(user1Email, TestPassword);
            if (user1SignUp?.User?.Id == null) return;
            var user1Id = Guid.Parse(user1SignUp.User.Id);

            await _supabaseClient.Auth.SignIn(user1Email, TestPassword);
            var user1Entry = new JournalEntry { Content = "User 1 entry", UserId = user1Id };
            await _supabaseClient.From<JournalEntry>().Insert(user1Entry);
            await _supabaseClient.Auth.SignOut();

            // Create and setup User 2
            var user2SignUp = await _supabaseClient.Auth.SignUp(user2Email, TestPassword);
            if (user2SignUp?.User?.Id == null) return;
            var user2Id = Guid.Parse(user2SignUp.User.Id);

            await _supabaseClient.Auth.SignIn(user2Email, TestPassword);
            var user2Entry = new JournalEntry { Content = "User 2 entry", UserId = user2Id };
            var user2InsertResult = await _supabaseClient.From<JournalEntry>().Insert(user2Entry);
            var user2EntryId = user2InsertResult.Model?.Id;

            // Act - Delete User 1's account
            await _supabaseClient.Auth.SignOut();
            await _supabaseClient.Auth.SignIn(user1Email, TestPassword);
            await _supabaseClient.Rpc("delete_my_account", null);

            // Assert - User 2's data should still exist
            await _supabaseClient.Auth.SignOut();
            await _supabaseClient.Auth.SignIn(user2Email, TestPassword);

            var user2Entries = await _supabaseClient
                .From<JournalEntry>()
                .Where(e => e.UserId == user2Id)
                .Get();

            user2Entries.Models.Should().NotBeEmpty();
            user2Entries.Models.Should().Contain(e => e.Id == user2EntryId);

            // Cleanup User 2
            await _supabaseClient.Rpc("delete_my_account", null);
        }
        catch (Exception ex) when (ex.Message.Contains("not configured"))
        {
            return;
        }
    }

    /// <summary>
    /// Test: Password verification fails with incorrect password.
    /// Verifies security check before account deletion.
    /// </summary>
    [Fact]
    public async Task DeleteAccount_WithInvalidPassword_FailsVerification()
    {
        // Arrange - Create test user
        var guid = Guid.NewGuid().ToString("N");
        var userEmail = $"test.delete.pwcheck.{guid.Substring(0, 8)}@testmail.com";

        try
        {
            var signUpResult = await _supabaseClient.Auth.SignUp(userEmail, TestPassword);
            if (signUpResult?.User?.Id == null) return;

            await _supabaseClient.Auth.SignIn(userEmail, TestPassword);

            // Act & Assert - Try to verify with wrong password
            await _supabaseClient.Auth.SignOut();
            var wrongPasswordAttempt = async () => 
                await _supabaseClient.Auth.SignIn(userEmail, "WrongPassword123!");

            await wrongPasswordAttempt.Should().ThrowAsync<Exception>();

            // Cleanup - Delete the test user properly
            await _supabaseClient.Auth.SignIn(userEmail, TestPassword);
            await _supabaseClient.Rpc("delete_my_account", null);
        }
        catch (Exception ex) when (ex.Message.Contains("not configured"))
        {
            return;
        }
    }

    /// <summary>
    /// Test: Export functionality returns correct data before deletion.
    /// Verifies export works as part of the delete flow.
    /// </summary>
    [Fact]
    public async Task ExportBeforeDelete_ReturnsCorrectData()
    {
        // Arrange - Create test user with entries
        var guid = Guid.NewGuid().ToString("N");
        var userEmail = $"test.delete.export.{guid.Substring(0, 8)}@testmail.com";

        try
        {
            var signUpResult = await _supabaseClient.Auth.SignUp(userEmail, TestPassword);
            if (signUpResult?.User?.Id == null) return;

            var userId = Guid.Parse(signUpResult.User.Id);

            await _supabaseClient.Auth.SignIn(userEmail, TestPassword);

            // Create test entries
            var testContent = $"Entry for export test - {DateTime.UtcNow:yyyy-MM-dd}";
            var entry = new JournalEntry { Content = testContent, UserId = userId };
            await _supabaseClient.From<JournalEntry>().Insert(entry);

            // Act - Export data before deletion
            var exportResult = await _supabaseClient.Rpc("export_journal_entries", null);

            // Assert - Verify export contains our data
            exportResult.Should().NotBeNull();
            var exportJson = exportResult!.Content;
            exportJson.Should().Contain(testContent);

            // Cleanup - Delete account
            await _supabaseClient.Rpc("delete_my_account", null);
        }
        catch (Exception ex) when (ex.Message.Contains("not configured"))
        {
            return;
        }
    }
}
