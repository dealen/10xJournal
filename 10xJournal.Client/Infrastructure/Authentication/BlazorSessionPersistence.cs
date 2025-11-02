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
    /// Note: This is a synchronous interface method, but we fire-and-forget the async save.
    /// The session is cached in memory for immediate retrieval, and the async save to localStorage
    /// happens in the background. This is the correct pattern for Blazor WASM as it doesn't support
    /// synchronous waiting on async JavaScript interop.
    /// </summary>
    public void SaveSession(Session session)
    {
        try
        {
            // Cache the session in memory for immediate access
            _cachedSession = session;
            
            var json = JsonSerializer.Serialize(session);
            
            // Fire-and-forget save to localStorage
            // This is the correct pattern for Blazor WASM - we cannot use .Wait() or .GetAwaiter().GetResult()
            // The in-memory cache ensures immediate availability, and localStorage persistence happens async
            _ = _jsRuntime.InvokeVoidAsync("localStorage.setItem", SESSION_KEY, json);
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
    /// Uses fire-and-forget pattern for localStorage removal (correct for Blazor WASM).
    /// </summary>
    public void DestroySession()
    {
        try
        {
            // Clear the memory cache immediately
            _cachedSession = null;
            
            // Fire-and-forget removal from localStorage
            // This is the correct pattern for Blazor WASM
            _ = _jsRuntime.InvokeVoidAsync("localStorage.removeItem", SESSION_KEY);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[BlazorSessionPersistence] Failed to destroy session: {ex.Message}");
        }
    }
}