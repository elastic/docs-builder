// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Elastic.Documentation.AppliesTo;

namespace Elastic.Markdown.Myst;

/// <summary>
/// Service for parsing ApplicableTo objects from YAML with memoization for performance.
/// </summary>
public static class ApplicableToParser
{
	private static readonly ConcurrentDictionary<string, ApplicableTo?> ParsedCache = new();

	/// <summary>
	/// Parses an ApplicableTo object from YAML string with memoization.
	/// This is the simple version that just returns the parsed object without error reporting.
	/// </summary>
	/// <param name="yaml">The YAML string to parse</param>
	/// <returns>The parsed ApplicableTo object, or null if parsing failed</returns>
	public static ApplicableTo? ParseApplicableTo(string yaml)
	{
		if (ParsedCache.TryGetValue(yaml, out var cached))
			return cached;

		ApplicableTo? parsed = null;
		try
		{
			parsed = YamlSerialization.Deserialize<ApplicableTo>(yaml);
			_ = ParsedCache.TryAdd(yaml, parsed);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Failed to parse ApplicableTo YAML: {yaml}. Error: {ex.Message}");
		}

		return parsed;
	}
}
