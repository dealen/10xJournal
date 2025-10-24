using Microsoft.Playwright;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace _10xJournal.E2E.Tests.Infrastructure;

/// <summary>
/// Base class for all E2E tests with common setup and cleanup.
/// Manages browser lifecycle and automatic test data cleanup.
/// </summary>
public abstract class E2ETestBase : IAsyncLifetime
{
    protected IPlaywright Playwright { get; private set; } = null!;
    protected IBrowser Browser { get; private set; } = null!;
    protected IPage Page { get; private set; } = null!;
    protected IConfiguration Configuration { get; private set; } = null!;
    protected string BaseUrl { get; private set; } = null!;
    protected TestDataCleanupHelper CleanupHelper { get; private set; } = null!;
    protected ILogger Logger { get; private set; } = null!;

    private readonly List<string> _usersToCleanup = new();

    protected E2ETestBase()
    {
        // Load configuration
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json", optional: false)
            .Build();

        BaseUrl = "http://localhost:5212"; // Blazor WASM default port
    }

    /// <summary>
    /// Initializes Playwright, browser, and page before each test.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Initialize Playwright
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        // Launch browser
        Browser = await Playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,  // Set to false for debugging
            SlowMo = 0        // Add delay (ms) for debugging if needed
        });

        // Create new page with proper viewport
        Page = await Browser.NewPageAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        // Set default timeout
        Page.SetDefaultTimeout(30000); // 30 seconds

        // Initialize cleanup helper with service role client
        var serviceRoleClient = CreateServiceRoleSupabaseClient();
        var loggerFactory = LoggerFactory.Create(builder => { });
        Logger = loggerFactory.CreateLogger(GetType());
        
        var url = Configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase URL not configured");
        var serviceKey = Configuration["Supabase:ServiceRoleKey"] ?? throw new InvalidOperationException("Supabase service role key not configured");
        
        CleanupHelper = new TestDataCleanupHelper(serviceRoleClient, loggerFactory.CreateLogger<TestDataCleanupHelper>(), url, serviceKey);
    }

    /// <summary>
    /// Cleans up browser resources and test data after each test.
    /// </summary>
    public async Task DisposeAsync()
    {
        try
        {
            // Clean up all test users created during this test
            foreach (var userEmail in _usersToCleanup)
            {
                await CleanupHelper.CleanupUserByEmailAsync(userEmail);
            }

            // Close browser resources
            if (Page != null) await Page.CloseAsync();
            if (Browser != null) await Browser.CloseAsync();
            Playwright?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during test cleanup");
        }
    }

    /// <summary>
    /// Registers a user for cleanup after the test completes.
    /// Call this immediately after creating a test user.
    /// </summary>
    protected void RegisterUserForCleanup(string userEmail)
    {
        _usersToCleanup.Add(userEmail);
        Logger.LogInformation("Registered user for cleanup: {Email}", userEmail);
    }

    /// <summary>
    /// Creates a Supabase client with service role key for cleanup operations.
    /// This client can bypass RLS to clean up test data.
    /// </summary>
    private Supabase.Client CreateServiceRoleSupabaseClient()
    {
        var url = Configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase URL not configured");
        var serviceKey = Configuration["Supabase:ServiceRoleKey"] ?? throw new InvalidOperationException("Supabase service role key not configured");

        var options = new Supabase.SupabaseOptions
        {
            AutoConnectRealtime = false
        };

        return new Supabase.Client(url, serviceKey, options);
    }

    /// <summary>
    /// Generates a unique test user email.
    /// </summary>
    protected string GenerateTestUserEmail()
    {
        return $"e2e-test-{Guid.NewGuid()}@10xjournal-test.com";
    }

    /// <summary>
    /// Generates a secure test password.
    /// </summary>
    protected string GenerateTestPassword()
    {
        return "SecureTestPassword123!";
    }
}
