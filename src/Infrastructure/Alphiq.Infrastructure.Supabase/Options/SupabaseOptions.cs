namespace Alphiq.Infrastructure.Supabase.Options;

/// <summary>
/// Configuration options for Supabase connection.
/// </summary>
public sealed class SupabaseOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Supabase";

    /// <summary>
    /// Supabase project URL (e.g., "https://xxx.supabase.co").
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Supabase API key (anon or service_role).
    /// </summary>
    public required string ApiKey { get; init; }
}
