// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer;

/// <summary>
/// The single source of URL path segments (monikers) for API explorer pages.
/// </summary>
public static class ApiUrlBuilder
{
	/// <summary>
	/// Deterministic URL segment for an operation page: the operation id when present,
	/// otherwise derived from the route (braces stripped, slashes to dashes).
	/// </summary>
	public static string OperationMoniker(string? operationId, string route) =>
		!string.IsNullOrWhiteSpace(operationId)
			? operationId
			: route.Replace("}", "").Replace("{", "").Replace('/', '-');

	/// <summary>Deterministic URL segment for a schema type page under <c>.../types/</c>.</summary>
	public static string SchemaMoniker(string schemaId) =>
		schemaId.Replace('.', '-').ToLowerInvariant();

	/// <summary>Deterministic single URL segment for <c>.../tags/{segment}/</c> from the canonical tag name.</summary>
	public static string TagMoniker(string? tagName)
	{
		if (string.IsNullOrWhiteSpace(tagName))
			return "unknown";

		var s = tagName.Trim();
		s = string.Join(" ", s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
		s = s.Replace("{", string.Empty, StringComparison.Ordinal);
		s = s.Replace("}", string.Empty, StringComparison.Ordinal);
		s = s.Replace("/", "-", StringComparison.Ordinal);
		s = s.Replace(" ", "-", StringComparison.Ordinal);
		if (string.IsNullOrEmpty(s))
			return "unknown";

		return s;
	}
}
