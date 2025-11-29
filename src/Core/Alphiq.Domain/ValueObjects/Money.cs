namespace Alphiq.Domain.ValueObjects;

/// <summary>
/// Represents a monetary amount with currency.
/// </summary>
public readonly record struct Money(decimal Amount, string Currency = "USD")
{
    public static Money Zero(string currency = "USD") => new(0, currency);

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Cannot add {a.Currency} and {b.Currency}");
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Cannot subtract {a.Currency} and {b.Currency}");
        return new Money(a.Amount - b.Amount, a.Currency);
    }

    public override string ToString() => $"{Amount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)} {Currency}";
}
