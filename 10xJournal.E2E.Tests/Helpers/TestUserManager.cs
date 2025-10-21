using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;

namespace _10xJournal.E2E.Tests.Helpers;

/// <summary>
/// Helper class for managing test users in Supabase for E2E testing.
/// Uses Supabase Admin API to create, verify, and clean up test users.
/// </summary>
public class TestUserManager : IDisposable
{
    private readonly string _supabaseUrl;
    private readonly string _serviceRoleKey;
    private readonly HttpClient _httpClient;

    public TestUserManager(string supabaseUrl, string serviceRoleKey)
    {
        _supabaseUrl = supabaseUrl ?? throw new ArgumentNullException(nameof(supabaseUrl));
        _serviceRoleKey = serviceRoleKey ?? throw new ArgumentNullException(nameof(serviceRoleKey));
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("apikey", _serviceRoleKey);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
    }

    /// <summary>
    /// Creates a test user with the specified email and password.
    /// The user will be automatically confirmed and ready to use.
    /// </summary>
    /// <param name="email">The email address for the test user</param>
    /// <param name="password">The password for the test user</param>
    /// <returns>The user ID if successful, null otherwise</returns>
    public async Task<string?> CreateTestUserAsync(string email, string password)
    {
        try
        {
            // First, try to delete the user if it exists
            await DeleteUserByEmailAsync(email);

            // Create the user
            var createUserPayload = new
            {
                email = email,
                password = password,
                email_confirm = true, // Auto-confirm the email
                user_metadata = new
                {
                    test_user = true,
                    created_at = DateTime.UtcNow.ToString("O")
                }
            };

            var json = JsonSerializer.Serialize(createUserPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_supabaseUrl}/auth/v1/admin/users",
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Failed to create user: {response.StatusCode}");
                Console.WriteLine($"Error details: {errorContent}");
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            var userId = doc.RootElement.GetProperty("id").GetString();

            Console.WriteLine($"✅ Successfully created test user: {email}");
            Console.WriteLine($"   User ID: {userId}");

            return userId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception creating test user: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Deletes a user by their email address.
    /// </summary>
    /// <param name="email">The email address of the user to delete</param>
    public async Task<bool> DeleteUserByEmailAsync(string email)
    {
        try
        {
            // First, get the user by email
            var userId = await GetUserIdByEmailAsync(email);
            if (userId == null)
            {
                Console.WriteLine($"ℹ️  User {email} does not exist, skipping deletion.");
                return true;
            }

            // Delete the user
            var response = await _httpClient.DeleteAsync(
                $"{_supabaseUrl}/auth/v1/admin/users/{userId}"
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"⚠️  Failed to delete user {email}: {response.StatusCode}");
                Console.WriteLine($"Error details: {errorContent}");
                return false;
            }

            Console.WriteLine($"🗑️  Successfully deleted test user: {email}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception deleting test user: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets a user ID by their email address.
    /// </summary>
    /// <param name="email">The email address to search for</param>
    /// <returns>The user ID if found, null otherwise</returns>
    public async Task<string?> GetUserIdByEmailAsync(string email)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_supabaseUrl}/auth/v1/admin/users"
            );

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"⚠️  Failed to list users: {response.StatusCode}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("users", out var users))
            {
                return null;
            }

            foreach (var user in users.EnumerateArray())
            {
                if (user.TryGetProperty("email", out var userEmail) &&
                    userEmail.GetString()?.Equals(email, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return user.GetProperty("id").GetString();
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception getting user ID: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Verifies that a user exists and can be authenticated.
    /// </summary>
    /// <param name="email">The email address to verify</param>
    /// <returns>True if the user exists and is confirmed, false otherwise</returns>
    public async Task<bool> VerifyUserExistsAsync(string email)
    {
        try
        {
            var userId = await GetUserIdByEmailAsync(email);
            if (userId == null)
            {
                Console.WriteLine($"❌ User {email} does not exist.");
                return false;
            }

            // Get user details to check if confirmed
            var response = await _httpClient.GetAsync(
                $"{_supabaseUrl}/auth/v1/admin/users/{userId}"
            );

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"⚠️  Failed to get user details: {response.StatusCode}");
                return false;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            var emailConfirmedAt = doc.RootElement.TryGetProperty("email_confirmed_at", out var confirmedAt)
                ? confirmedAt.GetString()
                : null;

            if (string.IsNullOrEmpty(emailConfirmedAt))
            {
                Console.WriteLine($"⚠️  User {email} exists but email is not confirmed.");
                return false;
            }

            Console.WriteLine($"✅ User {email} exists and is confirmed.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception verifying user: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Cleans up all test users (users with test_user metadata).
    /// </summary>
    public async Task<int> CleanupAllTestUsersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_supabaseUrl}/auth/v1/admin/users"
            );

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"⚠️  Failed to list users: {response.StatusCode}");
                return 0;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("users", out var users))
            {
                return 0;
            }

            int deletedCount = 0;
            foreach (var user in users.EnumerateArray())
            {
                if (user.TryGetProperty("user_metadata", out var metadata) &&
                    metadata.TryGetProperty("test_user", out var isTestUser) &&
                    isTestUser.GetBoolean())
                {
                    var userId = user.GetProperty("id").GetString();
                    var email = user.TryGetProperty("email", out var emailProp) 
                        ? emailProp.GetString() 
                        : "unknown";

                    var deleteResponse = await _httpClient.DeleteAsync(
                        $"{_supabaseUrl}/auth/v1/admin/users/{userId}"
                    );

                    if (deleteResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"🗑️  Deleted test user: {email}");
                        deletedCount++;
                    }
                }
            }

            Console.WriteLine($"✅ Cleaned up {deletedCount} test user(s).");
            return deletedCount;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception cleaning up test users: {ex.Message}");
            return 0;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
