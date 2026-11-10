// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation.Search.Contract;

namespace Elastic.Documentation.Api;

/// <summary>The <c>api_key</c> and <c>api_tree</c> query parameters that name the API page a navigation search comes from.</summary>
internal static partial class ApiPageParameters
{
	[GeneratedRegex(@"^[a-z0-9-]+\z", RegexOptions.CultureInvariant)]
	private static partial Regex ApiKeyPattern();

	[GeneratedRegex(@"^(main|[0-9]{1,3})\z", RegexOptions.CultureInvariant)]
	private static partial Regex ApiTreePattern();

	/// <summary>
	/// Both absent is no API page, and a key alone is a page outside any version tree. A tree without its key, or a
	/// malformed value, is rejected rather than ignored, so it neither returns a silently unscoped result nor becomes a
	/// cache variant of one.
	/// </summary>
	public static bool TryParse(string? apiKey, string? apiTree, out ApiPageContext? page)
	{
		page = null;
		if (apiKey is null && apiTree is null)
			return true;
		if (apiKey is null || !ApiKeyPattern().IsMatch(apiKey) || (apiTree is not null && !ApiTreePattern().IsMatch(apiTree)))
			return false;

		page = new ApiPageContext(apiKey, apiTree);
		return true;
	}
}
