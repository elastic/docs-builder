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
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
/// </remarks>
[Equatable]
public sealed partial record RelatedLearningCatalog
{
	public static RelatedLearningCatalog Empty { get; } = new() { Links = FrozenDictionary<string, RelatedLearningLink>.Empty };

	/// <summary>Catalog entries keyed by ID.</summary>
	[UnorderedEquality]
	public IReadOnlyDictionary<string, RelatedLearningLink> Links { get; internal init; } = default!;

	public bool TryGet(string id, [NotNullWhen(true)] out RelatedLearningLink? link) => Links.TryGetValue(id, out link);
}

/// <summary>A single named learning destination from <c>related-learning.yml</c>.</summary>
/// <remarks>
/// Constructed through <c>RelatedLearningCatalog</c>.
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
/// </remarks>
public sealed record RelatedLearningLink
{
	public string Id { get; internal init; } = default!;
	public string Title { get; internal init; } = default!;
	public string Url { get; internal init; } = default!;
}
