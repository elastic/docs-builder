// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.OpenApiIndex;

/// <summary>
/// Builds CloudFront invalidation paths for OpenAPI spec objects and the version index.
/// </summary>
public static class OpenApiInvalidationPaths
{
	/// <summary>
	/// Returns <c>/index.json</c> plus a leading-slash path for each distinct object key.
	/// </summary>
	public static IReadOnlyList<string> Build(IEnumerable<string> objectKeys)
	{
		var paths = new HashSet<string>(StringComparer.Ordinal) { $"/{VersionIndexPublisher.IndexKey}" };
		foreach (var key in objectKeys)
		{
			if (string.IsNullOrWhiteSpace(key))
				continue;

			_ = paths.Add($"/{key.TrimStart('/')}");
		}

		return [.. paths];
	}
}
