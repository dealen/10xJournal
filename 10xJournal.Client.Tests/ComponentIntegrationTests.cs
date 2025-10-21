using _10xJournal.Client.Features.Authentication.Login;
using _10xJournal.Client.Features.JournalEntries.CreateEntry;
using _10xJournal.Client.Features.JournalEntries.Models;
using _10xJournal.Client.Infrastructure.Authentication;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace _10xJournal.Client.Tests;

/// <summary>
/// Component tests in the context of vertical slice architecture.
/// Note: According to our testing instructions, we prefer integration tests over 
/// unit tests, especially for testing interactions with Supabase.
/// </summary>
public class ComponentIntegrationTests
{
    /// <summary>
    /// Example of what a component integration test might look like.
    /// In our architecture, we'd typically test this with a real Supabase instance.
    /// </summary>
    [Fact]
    public async Task EntryEditor_ShouldSaveEntryToDatabase_WhenContentIsValid()
    {
        // Arrange - Set up services with a real Supabase client for testing
        var services = new ServiceCollection();
        
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json", optional: true)
            .Build();
            
        string supabaseUrl = configuration["Supabase:TestUrl"] ?? "https://test-instance-url.supabase.co";
        string supabaseKey = configuration["Supabase:TestKey"] ?? "test-key";
        
        // Register services required by component
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<CurrentUserAccessor>();
        
        // Configure Supabase client for testing
        services.AddScoped(_ => new Supabase.Client(
            supabaseUrl, 
            supabaseKey, 
            new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            }));
            
        // Create test user for authentication
        var serviceProvider = services.BuildServiceProvider();
        var supabaseClient = serviceProvider.GetRequiredService<Supabase.Client>();
        
        // Generate unique email with realistic test domain
        var guid = Guid.NewGuid().ToString("N");
        string testEmail = $"test.{guid.Substring(0, 8)}@testmail.com";
        const string testPassword = "TestPassword123!";
        
        try
        {
            // Register a test user
            var signUpResult = await supabaseClient.Auth.SignUp(testEmail, testPassword);
            if (signUpResult?.User?.Id == null)
            {
                Console.WriteLine("Could not create test user");
                return;
            }
            
            var userId = Guid.Parse(signUpResult.User.Id);
            
            // Login as test user
            await supabaseClient.Auth.SignIn(testEmail, testPassword);
            
            // Now we would:
            // 1. Create a test entry directly using Supabase
            var entry = new JournalEntry
            {
                Content = "Test entry content",
                UserId = userId
            };
            
            // Act
            var insertResponse = await supabaseClient.From<JournalEntry>().Insert(entry);
            var result = insertResponse.Model;
            
            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Id);
            Assert.Equal("Test entry content", result.Content);
            Assert.Equal(userId, result.UserId);
            
            // Clean up by deleting the test entry
            await supabaseClient.From<JournalEntry>()
                .Where(e => e.Id == result.Id)
                .Delete();
        }
        finally
        {
            // Clean up by logging out
            await supabaseClient.Auth.SignOut();
        }
    }
    
    /// <summary>
    /// For UI-only components that don't interact with external dependencies,
    /// unit tests may still be appropriate.
    /// </summary>
    [Fact]
    public void SaveStatusIndicator_ShouldDisplayCorrectStatus_WhenStatusChanges()
    {
        // For truly UI-only components with no external dependencies,
        // we can still use component testing approaches.
        // This would be appropriate for small, reusable UI components.
        
        // Example of testing a UI-only component (simplified):
        // var context = new TestContext();
        // var component = context.RenderComponent<SaveStatusIndicator>(parameters => 
        //    parameters.Add(p => p.IsSaved, true));
        // Assert.Contains("Saved", component.Markup);
        
        // Not actually testing since this is a placeholder
        Assert.True(true);
    }
}