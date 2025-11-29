namespace Alphiq.Domain.ValueObjects;

/// <summary>
/// Strongly-typed account identifier.
/// </summary>
public readonly record struct AccountId(long Value)
{
    public static implicit operator long(AccountId id) => id.Value;
    public static implicit operator AccountId(long value) => new(value);
    public override string ToString() => Value.ToString();
}
