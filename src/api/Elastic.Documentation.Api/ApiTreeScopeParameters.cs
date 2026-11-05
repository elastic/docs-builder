// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation.Search.Contract;

namespace Elastic.Documentation.Api;

/// <summary>The <c>api_key</c> and <c>api_tree</c> query parameters that put a navigation search in one version tree of an API.</summary>
internal static partial class ApiTreeScopeParameters
{
	[GeneratedRegex(@"^[a-z0-9-]+\z", RegexOptions.CultureInvariant)]
	private static partial Regex ApiKeyPattern();

	[GeneratedRegex(@"^(main|[0-9]{1,3})\z", RegexOptions.CultureInvariant)]
	private static partial Regex ApiTreePattern();

	/// <summary>
	/// Both absent is no scope. One without the other, or a malformed value, is rejected rather than ignored, so it
	/// neither returns a silently unscoped result nor becomes a cache variant of one.
	/// </summary>
	public static bool TryParse(string? apiKey, string? apiTree, out ApiTreeScope? scope)
	{
		scope = null;
		if (apiKey is null && apiTree is null)
			return true;
		if (apiKey is null || apiTree is null || !ApiKeyPattern().IsMatch(apiKey) || !ApiTreePattern().IsMatch(apiTree))
			return false;

		scope = new ApiTreeScope(apiKey, apiTree);
		return true;
	}
}
