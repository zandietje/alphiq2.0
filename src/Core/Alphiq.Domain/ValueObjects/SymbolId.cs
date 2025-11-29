namespace Alphiq.Domain.ValueObjects;

/// <summary>
/// Strongly-typed symbol identifier.
/// </summary>
public readonly record struct SymbolId(long Value)
{
    public static implicit operator long(SymbolId id) => id.Value;
    public static implicit operator SymbolId(long value) => new(value);
    public override string ToString() => Value.ToString();
}
