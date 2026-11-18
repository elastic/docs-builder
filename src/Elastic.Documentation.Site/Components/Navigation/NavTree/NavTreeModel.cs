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

	/// <summary>The methods the badge can show, in the order given; a method with no badge, such as OPTIONS, is skipped.</summary>
	public static IReadOnlyList<HttpVerb> ParseMethods(IEnumerable<string> methods)
	{
		var parsed = new List<HttpVerb>();
		foreach (var method in methods)
		{
			if (Enum.TryParse<HttpVerb>(method, ignoreCase: true, out var verb))
				parsed.Add(verb);
		}
		return parsed;
	}
}
