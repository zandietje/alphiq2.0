namespace Alphiq.Domain.ValueObjects;

/// <summary>
/// Bar timeframe (e.g., M1, M5, H1, D1).
/// </summary>
public readonly record struct Timeframe
{
    public string Code { get; }
    public TimeSpan Duration { get; }

    private Timeframe(string code, TimeSpan duration)
    {
        Code = code;
        Duration = duration;
    }

    public static Timeframe M1 => new("M1", TimeSpan.FromMinutes(1));
    public static Timeframe M5 => new("M5", TimeSpan.FromMinutes(5));
    public static Timeframe M15 => new("M15", TimeSpan.FromMinutes(15));
    public static Timeframe M30 => new("M30", TimeSpan.FromMinutes(30));
    public static Timeframe H1 => new("H1", TimeSpan.FromHours(1));
    public static Timeframe H4 => new("H4", TimeSpan.FromHours(4));
    public static Timeframe D1 => new("D1", TimeSpan.FromDays(1));
    public static Timeframe W1 => new("W1", TimeSpan.FromDays(7));

    public static Timeframe Parse(string code) => code.ToUpperInvariant() switch
    {
        "M1" => M1,
        "M5" => M5,
        "M15" => M15,
        "M30" => M30,
        "H1" => H1,
        "H4" => H4,
        "D1" => D1,
        "W1" => W1,
        _ => throw new ArgumentException($"Unknown timeframe: {code}")
    };

    public override string ToString() => Code;
}
