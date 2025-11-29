using Alphiq.Domain.ValueObjects;

namespace Alphiq.Infrastructure.BrokerAuth;

/// <summary>
/// Provides access tokens for broker API authentication.
/// CTrader adapters MUST use this interface - they must NOT implement OAuth themselves.
/// </summary>
public interface IBrokerTokenProvider
{
    /// <summary>
    /// Gets a valid access token for the specified account.
    /// Handles token refresh automatically if needed.
    /// </summary>
    Task<string> GetAccessTokenAsync(AccountId accountId, CancellationToken ct = default);

    /// <summary>
    /// Checks if credentials exist for the account.
    /// </summary>
    Task<bool> HasCredentialsAsync(AccountId accountId, CancellationToken ct = default);
}
