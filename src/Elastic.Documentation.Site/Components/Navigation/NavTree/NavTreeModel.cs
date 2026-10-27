// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.Primitives.Method;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.Documentation.Site.Components.Navigation.NavTree;

public sealed record NavTreeModel
{
	public required IReadOnlyList<NavigationRenderNode> Items { get; init; }

	/// <summary>The tree's content hash. Pages that share a tree share this id.</summary>
	public required string Id { get; init; }

	public static bool TryParseMethod(string? method, out HttpVerb parsed)
	{
		parsed = HttpVerb.Get;
		return method is not null && Enum.TryParse(method, ignoreCase: true, out parsed);
	}
}
