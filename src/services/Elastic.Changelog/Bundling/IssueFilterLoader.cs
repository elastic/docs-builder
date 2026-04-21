// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Service for loading issue filter values from files or command line.
/// </summary>
public class IssueFilterLoader(IFileSystem fileSystem)
{
	/// <summary>
	/// Loads issue filter values from the provided input.
	/// Values can be file paths, URLs, short format (owner/repo#number), or issue numbers.
	/// </summary>
	public async Task<IssueFilterResult> LoadIssuesAsync(
		IDiagnosticsCollector collector,
		string[]? issues,
		string? owner,
		string? repo,
		Cancel ctx)
	{
		var (isValid, matches) = await FilterLoaderUtilities.LoadValuesAsync(
			fileSystem, collector, issues, owner, repo,
			exampleUrlSegment: "issues/123",
			numericValidationMessage: "When --issues contains issue numbers (not URLs or owner/repo#number format), both --owner and --repo must be provided",
			ctx
		);
		return new IssueFilterResult { IsValid = isValid, IssuesToMatch = matches };
	}
}

/// <summary>
/// Result of loading issue filter values.
/// </summary>
public record IssueFilterResult
{
	public required bool IsValid { get; init; }
	public required HashSet<string> IssuesToMatch { get; init; }
}
