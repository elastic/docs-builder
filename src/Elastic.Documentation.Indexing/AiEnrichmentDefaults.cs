// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Indexing;

/// <summary>
/// Shared defaults for generative AI enrichment runs, applied by both the docs-builder
/// <c>index</c> command and essc's <c>labs</c>/<c>contentstack</c> sync and <c>ai</c> commands.
/// </summary>
public static class AiEnrichmentDefaults
{
	/// <summary>Documents enriched per run when no explicit cap is specified.</summary>
	public const int MaxEnrichmentsPerRun = 100;

	/// <summary>How long the orchestrator waits for an enrichment batch to complete before retrying.</summary>
	public static readonly TimeSpan CompletionTimeout = TimeSpan.FromMinutes(5);

	/// <summary>Retries allowed per enrichment batch before it counts as failed.</summary>
	public const int CompletionMaxRetries = 2;

	/// <summary>Smallest wall-clock budget accepted for a run; anything shorter is rejected as impractical.</summary>
	public static readonly TimeSpan MinWallClock = TimeSpan.FromMinutes(1);
}
