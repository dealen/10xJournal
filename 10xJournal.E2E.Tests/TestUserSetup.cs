using System.Text.Json;
using _10xJournal.E2E.Tests.Helpers;

namespace _10xJournal.E2E.Tests;

/// <summary>
/// Standalone program to set up test users for E2E testing.
/// Run this before executing E2E tests to ensure test users exist and are properly configured.
/// 
/// Usage: dotnet run --project 10xJournal.E2E.Tests/10xJournal.E2E.Tests.csproj
///        Or from the E2E.Tests directory: dotnet run
/// 
/// Commands:
///   setup    - Create/recreate the test user
///   verify   - Verify the test user exists and is confirmed
///   cleanup  - Remove all test users
/// </summary>
public class TestUserSetup
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("🚀 10xJournal E2E Test User Setup");
        Console.WriteLine("=================================\n");

        // Load configuration
        var config = LoadConfiguration();
        if (config == null)
        {
            Console.WriteLine("❌ Failed to load configuration from appsettings.test.json");
            Console.WriteLine("   Make sure the file exists and contains valid Supabase settings.");
            return 1;
        }

        if (string.IsNullOrEmpty(config.ServiceRoleKey) || config.ServiceRoleKey == "YOUR_SERVICE_ROLE_KEY_HERE")
        {
            Console.WriteLine("❌ Service Role Key is not configured!");
            Console.WriteLine("\n📋 How to get your Service Role Key:");
            Console.WriteLine("   1. Go to: https://supabase.com/dashboard/project/_/settings/api");
            Console.WriteLine("   2. Copy the 'service_role' key (NOT the anon key)");
            Console.WriteLine("   3. Update appsettings.test.json:");
            Console.WriteLine("      \"ServiceRoleKey\": \"your_service_role_key_here\"");
            Console.WriteLine("\n⚠️  WARNING: Keep the service_role key SECRET! It has admin privileges.");
            Console.WriteLine("   Never commit it to version control!");
            return 1;
        }

        // Determine command
        var command = args.Length > 0 ? args[0].ToLower() : "setup";

        using var manager = new TestUserManager(config.SupabaseUrl, config.ServiceRoleKey);

        try
        {
            switch (command)
            {
                case "setup":
                    return await SetupTestUser(manager, config);

                case "verify":
                    return await VerifyTestUser(manager, config);

                case "cleanup":
                    return await CleanupTestUsers(manager);

                case "help":
                case "--help":
                case "-h":
                    ShowHelp();
                    return 0;

                default:
                    Console.WriteLine($"❌ Unknown command: {command}");
                    Console.WriteLine("   Run with 'help' to see available commands.");
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Unexpected error: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            return 1;
        }
    }

    private static async Task<int> SetupTestUser(TestUserManager manager, TestConfig config)
    {
        Console.WriteLine($"📝 Setting up test user: {config.TestUserEmail}");
        Console.WriteLine();

        var userId = await manager.CreateTestUserAsync(config.TestUserEmail, config.TestUserPassword);

        if (userId == null)
        {
            Console.WriteLine("\n❌ Failed to create test user. Check the error messages above.");
            return 1;
        }

        Console.WriteLine();
        var verified = await manager.VerifyUserExistsAsync(config.TestUserEmail);

        if (verified)
        {
            Console.WriteLine("\n✅ Test user setup complete and verified!");
            Console.WriteLine($"   Email: {config.TestUserEmail}");
            Console.WriteLine($"   Password: {config.TestUserPassword}");
            Console.WriteLine($"   User ID: {userId}");
            Console.WriteLine("\n🎯 You can now run E2E tests!");
            return 0;
        }
        else
        {
            Console.WriteLine("\n⚠️  Test user was created but verification failed.");
            Console.WriteLine("   The user might still work, but please check manually.");
            return 1;
        }
    }

    private static async Task<int> VerifyTestUser(TestUserManager manager, TestConfig config)
    {
        Console.WriteLine($"🔍 Verifying test user: {config.TestUserEmail}");
        Console.WriteLine();

        var verified = await manager.VerifyUserExistsAsync(config.TestUserEmail);

        if (verified)
        {
            Console.WriteLine("\n✅ Test user exists and is ready to use!");
            return 0;
        }
        else
        {
            Console.WriteLine("\n❌ Test user verification failed.");
            Console.WriteLine("   Run 'dotnet run setup' to create the user.");
            return 1;
        }
    }

    private static async Task<int> CleanupTestUsers(TestUserManager manager)
    {
        Console.WriteLine("🧹 Cleaning up all test users...");
        Console.WriteLine();

        var deletedCount = await manager.CleanupAllTestUsersAsync();

        Console.WriteLine($"\n✅ Cleanup complete. Removed {deletedCount} test user(s).");
        return 0;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Available commands:");
        Console.WriteLine("  setup    - Create/recreate the test user (default)");
        Console.WriteLine("  verify   - Verify the test user exists and is confirmed");
        Console.WriteLine("  cleanup  - Remove all test users");
        Console.WriteLine("  help     - Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run                    # Run setup (default)");
        Console.WriteLine("  dotnet run setup              # Create test user");
        Console.WriteLine("  dotnet run verify             # Verify test user exists");
        Console.WriteLine("  dotnet run cleanup            # Remove all test users");
    }

    private static TestConfig? LoadConfiguration()
    {
        try
        {
            var json = File.Exists("appsettings.test.json")
                ? File.ReadAllText("appsettings.test.json")
                : "{}";

            using var doc = JsonDocument.Parse(json);

            string supabaseUrl = "";
            string serviceRoleKey = "";
            string testUserEmail = "";
            string testUserPassword = "";

            if (doc.RootElement.TryGetProperty("Supabase", out var supabaseElement))
            {
                supabaseUrl = supabaseElement.TryGetProperty("Url", out var url) 
                    ? url.GetString() ?? "" 
                    : "";
                serviceRoleKey = supabaseElement.TryGetProperty("ServiceRoleKey", out var key) 
                    ? key.GetString() ?? "" 
                    : "";
            }

            if (doc.RootElement.TryGetProperty("TestUser", out var testUserElement))
            {
                testUserEmail = testUserElement.TryGetProperty("Email", out var email) 
                    ? email.GetString() ?? "" 
                    : "";
                testUserPassword = testUserElement.TryGetProperty("Password", out var password) 
                    ? password.GetString() ?? "" 
                    : "";
            }

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(testUserEmail) || string.IsNullOrEmpty(testUserPassword))
            {
                return null;
            }

            return new TestConfig
            {
                SupabaseUrl = supabaseUrl,
                ServiceRoleKey = serviceRoleKey,
                TestUserEmail = testUserEmail,
                TestUserPassword = testUserPassword
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading configuration: {ex.Message}");
            return null;
        }
    }

    private class TestConfig
    {
        public string SupabaseUrl { get; set; } = "";
        public string ServiceRoleKey { get; set; } = "";
        public string TestUserEmail { get; set; } = "";
        public string TestUserPassword { get; set; } = "";
    }
}
