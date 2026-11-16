// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Generator.Equals;

namespace Elastic.Documentation.Catalog;

/// <summary>
/// Global catalog of named elastic.co learning destinations. Pages opt in with
/// the <c>{related-learning}</c> directive by catalog ID.
/// </summary>
/// <remarks>
/// Constructed with <c>RelatedLearningLoader.Load</c>.
/// </remarks>
[Equatable]
public sealed partial record RelatedLearningCatalog
{
	public static RelatedLearningCatalog Empty { get; } = new() { Links = FrozenDictionary<string, RelatedLearningLink>.Empty };

	/// <summary>Catalog entries keyed by ID.</summary>
	[UnorderedEquality]
	public required IReadOnlyDictionary<string, RelatedLearningLink> Links { get; init; }

	public bool TryGet(string id, [NotNullWhen(true)] out RelatedLearningLink? link) => Links.TryGetValue(id, out link);
}

/// <summary>A single named learning destination from <c>related-learning.yml</c>.</summary>
/// <remarks>
/// Constructed through <c>RelatedLearningCatalog</c>.
/// </remarks>
public sealed record RelatedLearningLink
{
	public required string Id { get; init; }
	public required string Title { get; init; }
	public required string Url { get; init; }
}
