namespace Alphiq.Domain.ValueObjects;

/// <summary>
/// Trade quantity in lots.
/// </summary>
public readonly record struct Quantity(double Value)
{
    public static Quantity Zero => new(0);
    public static implicit operator double(Quantity q) => q.Value;
    public static implicit operator Quantity(double value) => new(value);
}
