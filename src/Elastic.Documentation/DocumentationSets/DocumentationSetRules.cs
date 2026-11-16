// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.DocumentationSets;

/// <summary>The parsing the loader and <see cref="DocumentationSetConstraints"/> share; the rules themselves are declared there, over the model.</summary>
public static class DocumentationSetRules
{
	/// <summary><c>registry</c>: empty, <c>public</c> and anything unrecognized mean the public link index.</summary>
	public static DocSetRegistry ParseRegistry(string? raw) =>
		!string.IsNullOrWhiteSpace(raw) && DocSetRegistryExtensions.TryParse(raw.Trim(), out var parsed, true)
			? parsed
			: DocSetRegistry.Public;
}
