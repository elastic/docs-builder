// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// The single source of URL path segments (monikers) for API explorer pages.
/// </summary>
public static partial class ApiUrlBuilder
{
	public static string ApiRoot(string? urlPathPrefix) => $"{urlPathPrefix?.TrimEnd('/')}/api";

	public static string ProductRoot(string? urlPathPrefix, string apiUrlSuffix) => $"{ApiRoot(urlPathPrefix)}/doc/{apiUrlSuffix}";

	/// <summary>
	/// URL path suffix for one API product version: <c>{key}</c> for <c>main</c>,
	/// <c>{key}/v{N}</c> for released numeric majors from the version index.
	/// </summary>
	public static string ProductSuffix(string apiKey, string versionMoniker) =>
		versionMoniker == "main" ? apiKey : $"{apiKey}/v{versionMoniker}";

	/// <summary>
	/// Deterministic URL leaf for an operation page under <c>.../operation/</c>: lowercase
	/// <c>operation-{id}</c> when an operation id is present, otherwise derived from the route.
	/// </summary>
	public static string OperationMoniker(string? operationId, string route)
	{
		var id = !string.IsNullOrWhiteSpace(operationId)
			? operationId
			: route.Replace("}", "").Replace("{", "").Replace('/', '-').Trim('-');

		return $"operation-{id.ToLowerInvariant()}";
	}

	/// <summary>Deterministic URL segment for a schema type page under <c>.../types/</c>.</summary>
	public static string SchemaMoniker(string schemaId) => schemaId.Replace('.', '-').ToLowerInvariant();

	/// <summary>Deterministic URL leaf for <c>.../group/{segment}</c> from the canonical tag name.</summary>
	public static string TagMoniker(string? tagName)
	{
		if (string.IsNullOrWhiteSpace(tagName))
			return "endpoint-unknown";

		var s = tagName.Trim();
		s = string.Join(" ", s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
		s = ParentheticalSuffixPattern().Replace(s, "-$1");
		s = s.Replace("{", string.Empty, StringComparison.Ordinal);
		s = s.Replace("}", string.Empty, StringComparison.Ordinal);
		s = s.Replace("/", "-", StringComparison.Ordinal);
		s = s.Replace(" ", "-", StringComparison.Ordinal);
		s = s.ToLowerInvariant();
		if (string.IsNullOrEmpty(s))
			return "endpoint-unknown";

		return $"endpoint-{s}";
	}

	[GeneratedRegex(@"\s*\(([^)]+)\)")]
	private static partial Regex ParentheticalSuffixPattern();
}
