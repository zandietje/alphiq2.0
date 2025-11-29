using Alphiq.Domain.ValueObjects;

namespace Alphiq.Infrastructure.BrokerAuth;

/// <summary>
/// Secure storage for broker credentials (encrypted in Supabase).
/// </summary>
public interface IBrokerCredentialStore
{
    /// <summary>
    /// Stores OAuth credentials for an account.
    /// </summary>
    Task StoreCredentialsAsync(
        AccountId accountId,
        string accessToken,
        string refreshToken,
        DateTimeOffset expiresAt,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves stored credentials for an account.
    /// </summary>
    Task<BrokerCredentials?> GetCredentialsAsync(AccountId accountId, CancellationToken ct = default);

    /// <summary>
    /// Removes credentials for an account.
    /// </summary>
    Task RemoveCredentialsAsync(AccountId accountId, CancellationToken ct = default);
}

/// <summary>
/// Broker OAuth credentials.
/// </summary>
public sealed record BrokerCredentials
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool NeedsRefresh => DateTimeOffset.UtcNow >= ExpiresAt.AddMinutes(-5);
}
