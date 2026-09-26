using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace LabWebAppServer.Web.Authentication;

public sealed class ServerTicketStore : ITicketStore
{
    private const int MaximumEntries = 1000;
    private readonly ConcurrentDictionary<string, AuthenticationTicket> tickets = new(StringComparer.Ordinal);

    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        RemoveExpired();
        if (tickets.Count >= MaximumEntries)
        {
            throw new InvalidOperationException("The local session store is full.");
        }

        var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        tickets[key] = ticket;
        return Task.FromResult(key);
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        if (tickets.ContainsKey(key))
        {
            tickets[key] = ticket;
        }

        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        if (!tickets.TryGetValue(key, out var ticket))
        {
            return Task.FromResult<AuthenticationTicket?>(null);
        }

        if (ticket.Properties.ExpiresUtc is { } expiresAt && expiresAt <= DateTimeOffset.UtcNow)
        {
            tickets.TryRemove(key, out _);
            return Task.FromResult<AuthenticationTicket?>(null);
        }

        return Task.FromResult<AuthenticationTicket?>(ticket);
    }

    public Task RemoveAsync(string key)
    {
        tickets.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private void RemoveExpired()
    {
        foreach (var pair in tickets)
        {
            if (pair.Value.Properties.ExpiresUtc is { } expiresAt && expiresAt <= DateTimeOffset.UtcNow)
            {
                tickets.TryRemove(pair.Key, out _);
            }
        }
    }
}
