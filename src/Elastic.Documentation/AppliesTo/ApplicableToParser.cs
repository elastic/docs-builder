// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Elastic.Documentation.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Documentation.AppliesTo;

/// <summary>
/// Service for parsing ApplicableTo objects from YAML with memoization for performance.
/// </summary>
public static class ApplicableToParser
{
	private static readonly ConcurrentDictionary<string, ApplicableTo?> ParsedCache = new();
	private static readonly IDeserializer Deserializer = new DeserializerBuilder()
		.IgnoreUnmatchedProperties()
		.WithEnumNamingConvention(HyphenatedNamingConvention.Instance)
		.WithTypeConverter(new ApplicableToYamlConverter())
		.Build();

	/// <summary>
	/// Parses an ApplicableTo object from YAML string with memoization.
	/// This is the simple version that just returns the parsed object without error reporting.
	/// </summary>
	/// <param name="yaml">The YAML string to parse</param>
	/// <returns>The parsed ApplicableTo object, or null if parsing failed</returns>
	public static ApplicableTo? ParseApplicableTo(string yaml)
	{
		// Check cache first
		if (ParsedCache.TryGetValue(yaml, out var cached))
			return cached;

		// Parse and cache the result
		ApplicableTo? parsed = null;
		try
		{
			parsed = Deserializer.Deserialize<ApplicableTo>(yaml);
		}
		catch
		{
			// If parsing fails, cache null to avoid retrying
		}

		// Cache the result (including null for failed parses)
		_ = ParsedCache.TryAdd(yaml, parsed);
		return parsed;
	}

}
