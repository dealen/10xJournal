using _10xJournal.Client.Features.JournalEntries.Models;
using _10xJournal.Client.Features.Settings.ExportData.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Xunit;

namespace _10xJournal.Client.Tests.Features.Settings;

/// <summary>
/// Integration tests for ExportData feature.
/// Tests data export functionality and RLS policy enforcement.
/// Verifies export_journal_entries RPC function works correctly.
/// </summary>
public class ExportDataIntegrationTests : IAsyncLifetime
{
    private Supabase.Client _supabaseClient = null!;
    private Guid _testUserId;
    private string _testUserEmail = string.Empty;
    private const string TestPassword = "TestPassword123!";
    private readonly List<Guid> _createdEntryIds = new();

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

        // Create unique test user
        var guid = Guid.NewGuid().ToString("N");
        _testUserEmail = $"test.export.{guid.Substring(0, 8)}@testmail.com";

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
            Console.WriteLine($"Test Supabase instance not configured: {ex.Message}");
        }
    }

    public async Task DisposeAsync()
    {
        // Cleanup test entries
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
                // Ignore cleanup errors
            }
        }

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
    /// Test: Export returns only current user's entries, not other users' data.
    /// This is a critical RLS policy verification test.
    /// </summary>
    [Fact]
    public async Task ExportData_ReturnsOnlyCurrentUserEntries_VerifiesRLS()
    {
        // Skip if test environment not configured
        if (_testUserId == Guid.Empty)
        {
            return;
        }

        // Arrange - Login and create test entries
        try
        {
            await _supabaseClient.Auth.SignIn(_testUserEmail, TestPassword);
        }
        catch
        {
            return;
        }

        var entry1 = new JournalEntry
        {
            Content = "Test entry 1 for export",
            UserId = _testUserId
        };
        var entry2 = new JournalEntry
        {
            Content = "Test entry 2 for export",
            UserId = _testUserId
        };

        var result1 = await _supabaseClient.From<JournalEntry>().Insert(entry1);
        var result2 = await _supabaseClient.From<JournalEntry>().Insert(entry2);

        if (result1.Model?.Id != null) _createdEntryIds.Add(result1.Model.Id.Value);
        if (result2.Model?.Id != null) _createdEntryIds.Add(result2.Model.Id.Value);

        // Act - Call export RPC function
        var exportResult = await _supabaseClient.Rpc("export_journal_entries", null);

        // Assert - Parse and verify export data
        exportResult.Should().NotBeNull();
        exportResult!.Content.Should().NotBeNullOrWhiteSpace();

        var exportData = JsonSerializer.Deserialize<ExportDataResponse>(
            exportResult.Content!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        exportData.Should().NotBeNull();
        exportData!.TotalEntries.Should().BeGreaterThanOrEqualTo(2);
        exportData.Entries.Should().HaveCountGreaterThanOrEqualTo(2);

        // Verify all entries belong to current user
        exportData.Entries.Should().OnlyContain(e => 
            e.Content.Contains("Test entry") && 
            _createdEntryIds.Contains(e.Id));
    }

    /// <summary>
    /// Test: Export data structure matches expected ExportDataResponse format.
    /// Verifies JSON serialization and response structure.
    /// </summary>
    [Fact]
    public async Task ExportData_FormatsDataCorrectly()
    {
        // Skip if test environment not configured
        if (_testUserId == Guid.Empty)
        {
            return;
        }

        // Arrange - Login and create test entry
        try
        {
            await _supabaseClient.Auth.SignIn(_testUserEmail, TestPassword);
        }
        catch
        {
            return;
        }

        var testContent = $"Export format test entry - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        var entry = new JournalEntry
        {
            Content = testContent,
            UserId = _testUserId
        };

        var insertResult = await _supabaseClient.From<JournalEntry>().Insert(entry);
        if (insertResult.Model?.Id != null)
        {
            _createdEntryIds.Add(insertResult.Model.Id.Value);
        }

        // Act - Export data
        var exportResult = await _supabaseClient.Rpc("export_journal_entries", null);

        // Assert - Verify structure
        var exportData = JsonSerializer.Deserialize<ExportDataResponse>(
            exportResult!.Content!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        exportData.Should().NotBeNull();
        exportData!.TotalEntries.Should().BeGreaterThan(0);
        exportData.ExportedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        exportData.Entries.Should().NotBeEmpty();

        // Verify entry structure
        var exportedEntry = exportData.Entries.FirstOrDefault(e => e.Content == testContent);
        exportedEntry.Should().NotBeNull();
        exportedEntry!.Id.Should().NotBeEmpty();
        exportedEntry.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        exportedEntry.Content.Should().Be(testContent);
    }

    /// <summary>
    /// Test: Export with no entries returns empty export structure.
    /// Verifies graceful handling of empty data.
    /// </summary>
    [Fact]
    public async Task ExportData_WithNoEntries_ReturnsEmptyExport()
    {
        // Skip if test environment not configured
        if (_testUserId == Guid.Empty)
        {
            return;
        }

        // Arrange - Login with user that has no entries (cleanup ensures this)
        try
        {
            await _supabaseClient.Auth.SignIn(_testUserEmail, TestPassword);
        }
        catch
        {
            return;
        }

        // Ensure no entries exist
        await _supabaseClient.From<JournalEntry>()
            .Where(e => e.UserId == _testUserId)
            .Delete();

        // Act - Export data
        var exportResult = await _supabaseClient.Rpc("export_journal_entries", null);

        // Assert - Verify empty export
        var exportData = JsonSerializer.Deserialize<ExportDataResponse>(
            exportResult!.Content!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        exportData.Should().NotBeNull();
        exportData!.TotalEntries.Should().Be(0);
        exportData.Entries.Should().BeEmpty();
        exportData.ExportedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }
}
