using System;
using System.Threading.Tasks;
using _10xJournal.Client.Features.Authentication.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace _10xJournal.Client.Features.Authentication.Services;

public sealed class CurrentUserAccessor
{
    private readonly Supabase.Client _supabaseClient;
    private readonly ILogger<CurrentUserAccessor> _logger;
    private readonly DevUserOptions _devUser;

    public CurrentUserAccessor(
        Supabase.Client supabaseClient,
        ILogger<CurrentUserAccessor> logger,
        IOptions<DevUserOptions> devUserOptions)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
        _devUser = devUserOptions.Value;
    }

    public Task<Guid?> GetCurrentUserIdAsync()
    {
        var userId = TryResolveFromSupabase();
        if (userId.HasValue)
        {
            return Task.FromResult<Guid?>(userId);
        }

        var devUserId = _devUser.GetParsedUserId();
        if (devUserId.HasValue)
        {
            _logger.LogWarning("Using development override for user {Email}", _devUser.Email ?? "unknown");
            return Task.FromResult<Guid?>(devUserId);
        }

        return Task.FromResult<Guid?>(null);
    }

    private Guid? TryResolveFromSupabase()
    {
        try
        {
            var auth = _supabaseClient.Auth;

            var sessionUserId = auth?.CurrentSession?.User?.Id;
            if (!string.IsNullOrWhiteSpace(sessionUserId) && Guid.TryParse(sessionUserId, out var parsedSessionId))
            {
                return parsedSessionId;
            }

            var currentUserId = auth?.CurrentUser?.Id;
            if (!string.IsNullOrWhiteSpace(currentUserId) && Guid.TryParse(currentUserId, out var parsedCurrentUserId))
            {
                return parsedCurrentUserId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to resolve user id from Supabase Auth context.");
        }

        return null;
    }
}
