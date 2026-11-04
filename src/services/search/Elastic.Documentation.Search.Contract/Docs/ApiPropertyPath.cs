// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract;

public static class ApiPropertyPath
{
	public const string ItemsMarker = "[]";
	public const string MapKeyMarker = "<string>";

	/// <summary>The path a reader types for <paramref name="path"/>: <c>[].retriever.standard</c> is <c>retriever.standard</c>, <c>labels.&lt;string&gt;.value</c> is <c>labels.value</c>.</summary>
	public static string ParentKey(string path) =>
		path.Replace("." + MapKeyMarker, "", StringComparison.Ordinal).Replace(ItemsMarker, "", StringComparison.Ordinal).TrimStart('.');
}
