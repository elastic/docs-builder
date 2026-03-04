// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Service for loading PR filter values from files or command line.
/// </summary>
public class PrFilterLoader(IFileSystem fileSystem)
{
	/// <summary>
	/// Loads PR filter values from the provided input.
	/// Values can be file paths, URLs, short PR format (owner/repo#number), or PR numbers.
	/// </summary>
	public async Task<PrFilterResult> LoadPrsAsync(
		IDiagnosticsCollector collector,
		string[]? prs,
		string? owner,
		string? repo,
		Cancel ctx)
	{
		var (isValid, matches) = await FilterLoaderUtilities.LoadValuesAsync(
			fileSystem, collector, prs, owner, repo,
			exampleUrlSegment: "pull/123",
			numericValidationMessage: "When --prs contains PR numbers (not URLs or owner/repo#number format), both --owner and --repo must be provided",
			ctx
		);
		return new PrFilterResult { IsValid = isValid, PrsToMatch = matches };
	}
}

/// <summary>
/// Result of loading PR filter values.
/// </summary>
public record PrFilterResult
{
	public required bool IsValid { get; init; }
	public required HashSet<string> PrsToMatch { get; init; }
}
