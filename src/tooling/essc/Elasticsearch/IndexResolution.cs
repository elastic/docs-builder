// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using Elastic.Transport;

namespace Elastic.SiteSearch.Cli.Elasticsearch;

/// <summary>
/// The outcome of an index bootstrap decision for one write target (primary or secondary):
/// whether a new backing index was created (vs. reusing an existing one), and the resolved
/// concrete index name currently behind the write alias.
/// </summary>
/// <param name="WriteAlias">The write alias/data stream name.</param>
/// <param name="TargetIndex">The concrete backing index resolved via <see cref="IndexResolution.ResolveConcreteIndexAsync"/>, or <c>null</c> if unresolved.</param>
/// <param name="RolledOver">Whether bootstrap created a new backing index (<c>true</c>) or reused an existing one (<c>false</c>).</param>
public sealed record IndexBootstrapInfo(string WriteAlias, string? TargetIndex, bool RolledOver);

/// <summary>
/// Resolves the concrete backing index behind a write alias. The ingest orchestrator only
/// exposes the write alias, not the dated backing index it actually bootstrapped — this fills
/// that gap using the standard <c>GET {alias}/_alias</c> API so sync commands can log exactly
/// which index they're writing into.
/// </summary>
internal static class IndexResolution
{
	/// <summary>
	/// Returns the concrete index name(s) currently behind <paramref name="writeAlias"/>, or
	/// <c>null</c> if the alias doesn't resolve (unexpected once bootstrap has completed).
	/// </summary>
	public static async Task<string?> ResolveConcreteIndexAsync(ITransport transport, string writeAlias, CancellationToken ct)
	{
		var response = await transport.GetAsync<StringResponse>($"{writeAlias}/_alias", cancellationToken: ct);
		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			return null;

		var root = JsonNode.Parse(response.Body ?? "{}")?.AsObject();
		if (root is null || root.Count == 0)
			return null;

		return string.Join(", ", root.Select(kvp => kvp.Key));
	}
}
