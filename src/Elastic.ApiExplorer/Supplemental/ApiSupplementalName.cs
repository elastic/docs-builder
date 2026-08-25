// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Supplemental;

public enum ApiSupplementalKind
{
	Operation,
	Tag
}

public readonly record struct ApiSupplementalFileName(
	ApiSupplementalKind Kind,
	string Stem,
	int? VersionMajor)
{
	public bool IsVersionSuffixed => VersionMajor is not null;
}

/// <summary>
/// Parses <c>op-*.md</c> / <c>tag-*.md</c> filenames. Operation stems are the spec
/// <c>operationId</c> with no rewriting. Tag stems are <see cref="ApiUrlBuilder.TagSlug"/>.
/// </summary>
public static partial class ApiSupplementalName
{
	public static bool IsConventionFileName(string fileName) => TryParse(fileName, out _);

	public static bool TryParse(string fileName, [NotNullWhen(true)] out ApiSupplementalFileName? parsed)
	{
		parsed = null;
		var match = FileNamePattern().Match(fileName);
		if (!match.Success)
			return false;

		var kind = match.Groups[1].Value.Equals("op", StringComparison.OrdinalIgnoreCase)
			? ApiSupplementalKind.Operation
			: ApiSupplementalKind.Tag;
		var stem = match.Groups[2].Value;
		if (stem.Length == 0)
			return false;

		int? version = null;
		if (match.Groups[3].Success)
		{
			if (!int.TryParse(match.Groups[3].Value, out var major))
				return false;
			version = major;
		}

		parsed = new ApiSupplementalFileName(kind, stem, version);
		return true;
	}

	/// <summary>File stem for a tag, identical to the URL slug without the <c>endpoint-</c> prefix.</summary>
	public static string TagFileStem(string tagName) => ApiUrlBuilder.TagSlug(tagName);

	[GeneratedRegex(@"^(op|tag)-(.+?)(?:\.v(\d+))?\.md$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex FileNamePattern();
}
