// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.GitHub;

/// <summary>Reads the pull requests of a range, for the entries that are synthesized from them.</summary>
public interface IPullRequestSummaryProvider
{
	/// <summary>
	/// The summary of each requested pull request, or <c>null</c> for one that could not be read. Returns <c>null</c> after
	/// emitting an error when GitHub cannot be queried at all (no credentials, API failures).
	/// </summary>
	Task<IReadOnlyDictionary<int, PullRequestSummary?>?> GetAsync(
		IDiagnosticsCollector collector,
		string owner,
		string repo,
		IReadOnlyCollection<int> numbers,
		Cancel ctx
	);
}
