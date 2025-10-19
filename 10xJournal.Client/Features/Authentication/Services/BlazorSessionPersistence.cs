using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using System.Text.Json;
using Microsoft.JSInterop;

namespace _10xJournal.Client.Features.Authentication.Services;

/// <summary>
/// Handles session persistence for Supabase authentication in Blazor WebAssembly.
/// Stores and retrieves session data from browser localStorage.
/// </summary>
public class BlazorSessionPersistence : IGotrueSessionPersistence<Session>
{
    private const string SESSION_KEY = "supabase.auth.token";
    private readonly IJSRuntime _jsRuntime;

    public BlazorSessionPersistence(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Saves the session to browser localStorage.
    /// </summary>
    public void SaveSession(Session session)
    {
        try
        {
            var json = JsonSerializer.Serialize(session);
            // Note: This is a synchronous call wrapped in a void async method
            _ = _jsRuntime.InvokeVoidAsync("localStorage.setItem", SESSION_KEY, json);
        }
        catch (Exception ex)
        {
            // Session save failed - log but don't throw
            Console.WriteLine($"Failed to save session: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads the session from browser localStorage.
    /// </summary>
    public Session? LoadSession()
    {
        try
        {
            // This is problematic in Blazor WASM - LoadSession is called synchronously
            // but we need async to access localStorage
            // We'll return null here and rely on InitializeAsync to load the session
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load session: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Loads the session asynchronously from browser localStorage.
    /// This is the proper way to load sessions in Blazor WASM.
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

            return JsonSerializer.Deserialize<Session>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load session: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Destroys the session by removing it from browser localStorage.
    /// </summary>
    public void DestroySession()
    {
        try
        {
            _ = _jsRuntime.InvokeVoidAsync("localStorage.removeItem", SESSION_KEY);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to destroy session: {ex.Message}");
        }
    }
}
