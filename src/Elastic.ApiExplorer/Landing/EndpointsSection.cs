// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation;

namespace Elastic.ApiExplorer.Landing;

/// <summary>The Endpoints heading that the product landing page and a group page put above their list, and the anchor it owns.</summary>
public static class EndpointsSection
{
	public const string Anchor = "endpoints";

	/// <summary>The anchors these pages own: a description heading or a supplemental section never takes one.</summary>
	public static readonly IReadOnlySet<string> ReservedAnchors = FrozenSet.ToFrozenSet([Anchor], StringComparer.Ordinal);

	/// <summary>On this page: the headings down to level 3, then Endpoints when the page lists any.</summary>
	public static IReadOnlyList<TocItem> Toc(IEnumerable<TocItem> headings, bool hasEndpoints) =>
		[
			.. headings.Where(static heading => heading.Level <= 3),
			.. hasEndpoints ? [new TocItem("Endpoints", Anchor, 2)] : Array.Empty<TocItem>()
		];
}
