using System;

namespace _10xJournal.Client.Features.Authentication.Login;

public sealed class DevUserOptions
{
    public bool Enabled { get; set; }

    public string? UserId { get; set; }

    public string? Email { get; set; }

    public Guid? GetParsedUserId()
    {
        if (!Enabled || string.IsNullOrWhiteSpace(UserId))
        {
            return null;
        }

        return Guid.TryParse(UserId, out var parsed) ? parsed : null;
    }
}