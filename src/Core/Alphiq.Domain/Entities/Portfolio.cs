using Alphiq.Domain.ValueObjects;

namespace Alphiq.Domain.Entities;

/// <summary>
/// Portfolio/account state.
/// </summary>
public sealed record Portfolio
{
    public required AccountId AccountId { get; init; }
    public required Money Balance { get; init; }
    public required Money Equity { get; init; }
    public required Money Margin { get; init; }
    public required Money FreeMargin { get; init; }
    public required IReadOnlyList<Position> OpenPositions { get; init; }
}
