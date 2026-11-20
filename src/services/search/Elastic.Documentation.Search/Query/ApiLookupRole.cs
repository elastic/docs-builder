// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Globalization;

#pragma warning disable IDE0130 // 'Query' subfolder would shadow the ES client's Query type
namespace Elastic.Documentation.Search;

/// <summary>What a word of an API lookup names about an operation.</summary>
public enum ApiLookupRole
{
	Key,
	Id,
	Group,
	Title,
	Method,
	Field,
	Body
}

/// <summary>The role that explained the word at <paramref name="Index"/> best, with the score of its option.</summary>
public sealed record ApiTokenRole(int Index, ApiLookupRole Role, double Score);

public static class ApiLookupRoles
{
	private static readonly FrozenDictionary<ApiLookupRole, string> Names = new Dictionary<ApiLookupRole, string>
	{
		[ApiLookupRole.Key] = "key",
		[ApiLookupRole.Id] = "id",
		[ApiLookupRole.Group] = "group",
		[ApiLookupRole.Title] = "title",
		[ApiLookupRole.Method] = "method",
		[ApiLookupRole.Field] = "field",
		[ApiLookupRole.Body] = "body"
	}.ToFrozenDictionary();

	private static readonly FrozenDictionary<string, ApiLookupRole> ByName = Names.ToFrozenDictionary(
		kv => kv.Value,
		kv => kv.Key,
		StringComparer.Ordinal
	);

	/// <summary>The name of a role option, such as <c>field_1</c>, as Elasticsearch reports it in <c>matched_queries</c>.</summary>
	public static string QueryName(ApiLookupRole role, int index) => $"{Names[role]}_{index.ToString(CultureInfo.InvariantCulture)}";

	public static bool TryParse(string queryName, out ApiLookupRole role, out int index)
	{
		role = default;
		index = -1;
		var separator = queryName.LastIndexOf('_');
		return separator > 0
			&& ByName.TryGetValue(queryName[..separator], out role)
			&& int.TryParse(queryName.AsSpan(separator + 1), NumberStyles.None, CultureInfo.InvariantCulture, out index);
	}

	/// <summary>
	/// The best role of each word, from the named query scores of a hit. A tie goes to the field, so a word that names a
	/// field as well as anything else still lists that field.
	/// </summary>
	public static IReadOnlyList<ApiTokenRole> Resolve(IReadOnlyDictionary<string, double> scoredQueries) =>
		scoredQueries
			.Select(kv => TryParse(kv.Key, out var role, out var index) ? new ApiTokenRole(index, role, kv.Value) : null)
			.OfType<ApiTokenRole>()
			.GroupBy(r => r.Index)
			.Select(token => token.OrderByDescending(r => r.Score).ThenBy(r => r.Role == ApiLookupRole.Field ? 0 : 1).First())
			.OrderBy(r => r.Index)
			.ToArray();
}
