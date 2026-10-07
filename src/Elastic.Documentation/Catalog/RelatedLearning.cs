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
/// <para><b>Built by:</b> <c>RelatedLearningLoader.Load</c> in <c>Elastic.Documentation.Configuration</c>, reading <c>related-learning.yml</c> (<c>RelatedLearningCatalog.Empty</c> is the empty catalog).</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>Links</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="RelatedLearningConstraints"/>) is where a missing member is reported, and the loader's mapping is what normally sets them.</para>
/// <para><b>In a test:</b> build it with an object initializer (<c>new RelatedLearningCatalog { Links = ... }</c>) or <c>DomainGen.Of&lt;RelatedLearningCatalog&gt;()</c> (tests), and <c>with</c> for variations.</para>
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
/// <para><b>Built by:</b> <c>RelatedLearningLoader.Load</c> in <c>Elastic.Documentation.Configuration</c>, reading <c>related-learning.yml</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>Id</c>, <c>Title</c>, <c>Url</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="RelatedLearningConstraints"/>) reports it: <see cref="RelatedLearningConstraints.LinkHasTitle"/> and <see cref="RelatedLearningConstraints.LinkHasUrl"/> report a missing title or url.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new RelatedLearningLink { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
public sealed record RelatedLearningLink
{
	public string Id { get; internal init; } = default!;
	public string Title { get; internal init; } = default!;
	public string Url { get; internal init; } = default!;
}
