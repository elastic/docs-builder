// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;

namespace Elastic.Documentation.DocumentationSets;

/// <summary>The <c>extensions</c> enabled in <c>docset.yml</c>.</summary>
[Equatable]
public sealed partial record EnabledExtensions
{
	// An extensions key written with no value is null here and fails with a NullReferenceException, as the spread did when the configuration was built.
	public EnabledExtensions(IEnumerable<string> extensions)
	{
		HashSet<string> enabled = [.. extensions];
		Enabled = enabled;
	}

	public bool IsDetectionRulesEnabled => Enabled.Contains("detection-rules");

	[SetEquality]
	public IReadOnlySet<string> Enabled { get; }

	public static EnabledExtensions None { get; } = new([]);
}
