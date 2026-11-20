// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Converters;

/// <summary>
/// The keys of a mapping that its reader does not read. A loader that walks a mapping node by node already holds the written keys;
/// this is the one place that turns them into the <c>UnknownKeys</c> list a model keeps (see <c>Constraints-Conventions.md</c>).
/// </summary>
internal static class UnknownYamlKeys
{
	/// <summary>The <paramref name="written"/> keys that are not in <paramref name="known"/>, in the order written.</summary>
	public static IReadOnlyList<string> Of(IEnumerable<string> written, params string[] known) =>
		[.. written.Where(key => !known.Contains(key, StringComparer.Ordinal))];
}
