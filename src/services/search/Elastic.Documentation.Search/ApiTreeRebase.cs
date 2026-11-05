// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract;

namespace Elastic.Documentation.Search;

/// <summary>
/// One document serves every tree that renders its operation alike, and its path points into the first of them. A
/// reader in another of those trees gets the same page under that tree's root.
/// </summary>
public sealed record ApiTreeRebase(string From, string To)
{
	/// <summary>The root swap that points a hit into the reader's tree, or <c>null</c> when its URLs already do or it belongs to another API.</summary>
	public static ApiTreeRebase? For(ApiOperationMetadata? api, ApiTreeScope? scope)
	{
		if (scope is null || api is not { Trees: [var first, ..] } || api.Key != scope.Key)
			return null;

		var target = api.Trees.FirstOrDefault(t => t.Tree == scope.Tree);
		return target is null || target.Root == first.Root ? null : new ApiTreeRebase(first.Root, target.Root);
	}

	/// <summary>Swaps <see cref="From"/> for <see cref="To"/> when <paramref name="url"/> lies under it.</summary>
	public string Apply(string url) =>
		url.StartsWith(From, StringComparison.Ordinal) && (url.Length == From.Length || url[From.Length] is '/' or '#' or '?')
			? To + url[From.Length..]
			: url;
}
