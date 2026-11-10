// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Operations;

/// <summary>
/// The order operations are listed in: by route, then by method in resource lifecycle order (GET, POST, PUT, PATCH,
/// DELETE, HEAD, OPTIONS, TRACE, CONNECT). Unlike <see cref="OperationEndpoint"/>'s dominant method, this ranks for
/// scanning a list, not for choosing which method a page leads with.
/// </summary>
public static class ApiOperationOrder
{
	private static readonly string[] Lifecycle = ["get", "post", "put", "patch", "delete", "head", "options", "trace", "connect"];

	/// <summary>Orders <paramref name="items"/> by the route and method of the endpoint row each one shows first.</summary>
	public static IOrderedEnumerable<T> InListOrder<T>(this IEnumerable<T> items, Func<T, EndpointRow> row) =>
		items.OrderBy(i => row(i).Route, StringComparer.Ordinal).ThenBy(i => MethodRank(row(i).Method));

	/// <summary>The method's place in the lifecycle, ignoring case; an unknown method sorts last.</summary>
	public static int MethodRank(string method)
	{
		var index = Array.IndexOf(Lifecycle, method.ToLowerInvariant());
		return index < 0 ? Lifecycle.Length : index;
	}
}
