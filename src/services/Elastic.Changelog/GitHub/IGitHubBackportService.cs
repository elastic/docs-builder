// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.GitHub;

/// <summary>Finds the pull request each backport was made from.</summary>
public interface IGitHubBackportService
{
	/// <summary>
	/// Resolves the original pull request of each backport. The result has an entry for every requested number:
	/// the originals, or <c>null</c> when none could be found. Returns <c>null</c> after emitting an error when
	/// GitHub cannot be queried (missing credentials, API failures).
	/// </summary>
	Task<IReadOnlyDictionary<int, BackportRoots?>?> ResolveRootsAsync(
		IDiagnosticsCollector collector,
		string owner,
		string repo,
		IReadOnlyCollection<int> backportNumbers,
		Cancel ctx
	);
}
