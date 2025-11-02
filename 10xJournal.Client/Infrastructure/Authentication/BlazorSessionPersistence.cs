using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using System.Text.Json;
using Microsoft.JSInterop;

namespace _10xJournal.Client.Infrastructure.Authentication;

/// <summary>
/// Handles session persistence for Supabase authentication in Blazor WebAssembly.
/// Stores and retrieves session data from browser localStorage.
/// </summary>
public class BlazorSessionPersistence : IGotrueSessionPersistence<Session>
{
    private const string SESSION_KEY = "supabase.auth.token";
    private readonly IJSRuntime _jsRuntime;
    private Session? _cachedSession;

    public BlazorSessionPersistence(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Saves the session to browser localStorage.
    /// Note: This is a synchronous interface method, but we immediately start the async save.
    /// The session is also cached in memory for immediate retrieval.
    /// We use ConfigureAwait(false).GetAwaiter().GetResult() to ensure the save completes
    /// even on mobile browsers before proceeding.
    /// </summary>
    public void SaveSession(Session session)
    {
        try
        {
            // Cache the session in memory for immediate access
            _cachedSession = session;
            
            var json = JsonSerializer.Serialize(session);
            
            // CRITICAL: Use synchronous wait to ensure localStorage save completes
            // This is especially important on mobile browsers where fire-and-forget
            // operations may not complete if the page navigates away quickly
            try
            {
                _jsRuntime.InvokeVoidAsync("localStorage.setItem", SESSION_KEY, json)
                    .AsTask()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                // Log but don't throw - we have the session cached in memory
                Console.Error.WriteLine($"[BlazorSessionPersistence] Failed to save session to localStorage: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[BlazorSessionPersistence] Failed to save session: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads the session from browser localStorage.
    /// Returns cached session if available, otherwise returns null.
    /// The Supabase client will call InitializeAsync which uses LoadSessionAsync.
    /// </summary>
    public Session? LoadSession()
    {
        // Return cached session if available
        // The actual loading from localStorage happens in InitializeAsync
        return _cachedSession;
    }

    /// <summary>
    /// Loads the session asynchronously from browser localStorage.
    /// This is called by Supabase.InitializeAsync() to restore the session on app startup.
    /// </summary>
    public async Task<Session?> LoadSessionAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", SESSION_KEY);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var session = JsonSerializer.Deserialize<Session>(json);
            
            // Cache the loaded session for synchronous access
            _cachedSession = session;
            
            return session;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load session: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Destroys the session by removing it from browser localStorage and memory cache.
    /// Uses synchronous wait to ensure cleanup completes on mobile browsers.
    /// </summary>
    public void DestroySession()
    {
        try
        {
            // Clear the memory cache
            _cachedSession = null;
            
            // Ensure localStorage removal completes (important for mobile)
            try
            {
                _jsRuntime.InvokeVoidAsync("localStorage.removeItem", SESSION_KEY)
                    .AsTask()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[BlazorSessionPersistence] Failed to destroy session in localStorage: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[BlazorSessionPersistence] Failed to destroy session: {ex.Message}");
        }
    }
}