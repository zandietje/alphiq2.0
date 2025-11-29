namespace Alphiq.Optimizer.Search;

/// <summary>
/// Search algorithm for parameter optimization.
/// </summary>
public interface ISearchAlgorithm
{
    string Name { get; }

    /// <summary>
    /// Generates the next set of parameters to evaluate.
    /// </summary>
    IReadOnlyList<ParameterSet> GenerateNextBatch(int batchSize);

    /// <summary>
    /// Updates the algorithm with evaluation results.
    /// </summary>
    void ReportResults(IReadOnlyList<EvaluationResult> results);

    /// <summary>
    /// Gets the best parameters found so far.
    /// </summary>
    ParameterSet? GetBestParameters();
}

/// <summary>
/// Set of strategy parameters for evaluation.
/// </summary>
public sealed record ParameterSet
{
    public required string Id { get; init; }
    public required IReadOnlyDictionary<string, object> Parameters { get; init; }
}

/// <summary>
/// Result of evaluating a parameter set.
/// </summary>
public sealed record EvaluationResult
{
    public required string ParameterSetId { get; init; }
    public required double Fitness { get; init; }
    public required decimal ProfitFactor { get; init; }
    public required decimal MaxDrawdownPercent { get; init; }
    public required int TotalTrades { get; init; }
    public required decimal WinRate { get; init; }
}
