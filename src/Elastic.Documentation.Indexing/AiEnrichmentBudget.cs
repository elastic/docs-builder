// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Indexing;

/// <summary>
/// A doc-count and wall-clock budget for a single AI enrichment run.
/// </summary>
/// <param name="MaxDocs">Maximum documents to enrich this run. <see langword="null"/> or <see langword="0"/> falls back to <see cref="AiEnrichmentDefaults.MaxEnrichmentsPerRun"/>; consumers that support an explicit "no cap" should pass <see langword="null"/> and treat <c>0</c> specially before constructing the budget.</param>
/// <param name="MaxTime">Optional wall-clock deadline. Must be at least <see cref="AiEnrichmentDefaults.MinWallClock"/> — validate with <see cref="TryValidateMaxTime"/> before constructing.</param>
public sealed record AiEnrichmentBudget(int? MaxDocs, TimeSpan? MaxTime)
{
	/// <summary>Default budget: <see cref="AiEnrichmentDefaults.MaxEnrichmentsPerRun"/> documents, no time limit.</summary>
	public static AiEnrichmentBudget Default { get; } = new(AiEnrichmentDefaults.MaxEnrichmentsPerRun, null);

	/// <summary><see cref="MaxDocs"/>, or <see cref="AiEnrichmentDefaults.MaxEnrichmentsPerRun"/> when unset.</summary>
	public int EffectiveMaxDocs => MaxDocs is > 0 ? MaxDocs.Value : AiEnrichmentDefaults.MaxEnrichmentsPerRun;

	/// <summary>
	/// Validates a candidate <c>--max-*-time</c> value. Enforces a minimum of <see cref="AiEnrichmentDefaults.MinWallClock"/>
	/// to avoid deadlines so short the enrichment orchestrator cannot make progress.
	/// </summary>
	/// <param name="maxTime">The candidate wall-clock limit, or <see langword="null"/> for no limit.</param>
	/// <param name="error">Set to a user-facing error message when validation fails; otherwise <see langword="null"/>.</param>
	/// <returns><see langword="true"/> when <paramref name="maxTime"/> is <see langword="null"/> or within bounds.</returns>
	public static bool TryValidateMaxTime(TimeSpan? maxTime, out string? error)
	{
		if (maxTime is { } wall && wall < AiEnrichmentDefaults.MinWallClock)
		{
			error = "must be at least 1m (for example 1m, 90m, 2h) when specified.";
			return false;
		}
		error = null;
		return true;
	}
}
