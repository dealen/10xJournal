using Microsoft.Extensions.Logging;

namespace _10xJournal.Client.Tests.Infrastructure.TestHelpers;

/// <summary>
/// Helper class for working with Supabase in integration tests.
/// Provides utilities for rate limiting, retries, and database verification.
/// </summary>
public static class SupabaseTestHelper
{
    private static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds(500);
    private const int MaxRetryAttempts = 3;

    /// <summary>
    /// Executes an async action with retry logic for rate limiting.
    /// Uses exponential backoff to avoid overwhelming the test instance.
    /// </summary>
    public static async Task ExecuteWithRetryAsync(
        Func<Task> action,
        ILogger? logger = null)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                await action();
                return;
            }
            catch (Supabase.Gotrue.Exceptions.GotrueException ex) 
                when ((ex.Message.Contains("429") || ex.Message.Contains("over_request_rate_limit")) 
                      && attempt <= MaxRetryAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)) + BaseDelay;
                logger?.LogWarning(
                    "Rate limit hit (attempt {Attempt}/{MaxRetries}). Waiting {Delay}ms before retry. Error: {Error}",
                    attempt,
                    MaxRetryAttempts,
                    delay.TotalMilliseconds,
                    ex.Message);
                await Task.Delay(delay);
            }
        }
    }

    /// <summary>
    /// Executes an async function with retry logic for rate limiting.
    /// Uses exponential backoff to avoid overwhelming the test instance.
    /// </summary>
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> action,
        ILogger? logger = null)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                return await action();
            }
            catch (Supabase.Gotrue.Exceptions.GotrueException ex) 
                when ((ex.Message.Contains("429") || ex.Message.Contains("over_request_rate_limit")) 
                      && attempt <= MaxRetryAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)) + BaseDelay;
                logger?.LogWarning(
                    "Rate limit hit (attempt {Attempt}/{MaxRetries}). Waiting {Delay}ms before retry. Error: {Error}",
                    attempt,
                    MaxRetryAttempts,
                    delay.TotalMilliseconds,
                    ex.Message);
                await Task.Delay(delay);
            }
        }
    }

    /// <summary>
    /// Verifies that required database functions exist.
    /// Throws an exception with helpful error message if functions are missing.
    /// </summary>
    public static async Task VerifyDatabaseFunctionsAsync(
        Supabase.Client supabaseClient,
        ILogger? logger = null)
    {
        var requiredFunctions = new[]
        {
            "initialize_new_user",
            "export_journal_entries",
            "delete_my_account"
        };

        var missingFunctions = new List<string>();

        foreach (var functionName in requiredFunctions)
        {
            try
            {
                // Try to call the function with a test UUID to check if it exists
                // We expect it to fail with auth error, but that's better than "function not found"
                var testId = Guid.Empty;
                
                if (functionName == "initialize_new_user")
                {
                    // This one takes a parameter
                    var parameters = new Dictionary<string, object>
                    {
                        { "p_user_id", testId }
                    };
                    await supabaseClient.Rpc(functionName, parameters);
                }
                else
                {
                    // These take no parameters
                    await supabaseClient.Rpc(functionName, null);
                }
            }
            catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
            {
                // Check if the error is about the function not existing
                if (ex.Message.Contains("PGRST202") || 
                    ex.Message.Contains("Could not find the function") ||
                    ex.Message.Contains("no matches were found in the schema cache"))
                {
                    missingFunctions.Add(functionName);
                    logger?.LogError("Database function '{FunctionName}' not found: {Error}", functionName, ex.Message);
                }
                // Other errors (like auth errors) are OK - function exists
            }
            catch (Exception ex)
            {
                // Other exceptions might mean the function exists but we're not authenticated
                logger?.LogDebug("Function '{FunctionName}' check returned: {Error}", functionName, ex.Message);
            }
        }

        if (missingFunctions.Any())
        {
            var errorMessage = $@"
❌ MISSING DATABASE FUNCTIONS ❌

The following required database functions are missing from the test database:
{string.Join(Environment.NewLine, missingFunctions.Select(f => $"  - {f}"))}

This likely means your test database is missing migrations.

To fix this:
1. Ensure all migrations in /supabase/migrations have been applied to your test database
2. Check that the test database URL in appsettings.test.json is correct
3. Run migrations on your test Supabase instance

Required migrations:
  - 20251020110000_create_initialize_new_user_function.sql
  - 20251020140000_fix_initialize_new_user_ambiguity.sql
  - 20251019100000_create_export_journal_entries_function.sql
  - 20251019110000_create_delete_my_account_function.sql
";
            throw new InvalidOperationException(errorMessage);
        }

        logger?.LogInformation("✅ All required database functions verified");
    }

    /// <summary>
    /// Checks if an exception is a rate limit error.
    /// </summary>
    public static bool IsRateLimitError(Exception ex)
    {
        return ex switch
        {
            Supabase.Gotrue.Exceptions.GotrueException gotrueEx => 
                gotrueEx.Message.Contains("429") || 
                gotrueEx.Message.Contains("over_request_rate_limit"),
            _ => false
        };
    }

    /// <summary>
    /// Checks if a test should be skipped due to configuration issues.
    /// Returns true if the test should be skipped, false otherwise.
    /// </summary>
    public static bool ShouldSkipTest(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();
        return message.Contains("not configured") || 
               message.Contains("test-instance-url") ||
               message.Contains("test-key");
    }

    /// <summary>
    /// Creates a test-friendly logger for diagnostic output.
    /// </summary>
    public static ILogger<T> CreateTestLogger<T>()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
        });
        return loggerFactory.CreateLogger<T>();
    }

    /// <summary>
    /// Adds a small delay to help avoid rate limiting in sequential tests.
    /// </summary>
    public static async Task DelayForRateLimitAsync()
    {
        await Task.Delay(BaseDelay);
    }
}
