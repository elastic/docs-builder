// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Components.PropertyTree;

/// <summary>The text of a row's fold toggle. api-docs.ts rewrites it from the same count when a reader flips the row.</summary>
public static class PropertyToggle
{
	public static string Label(bool expanded, int count) =>
		$"{(expanded ? "Hide" : "Show")} {count} {(count == 1 ? "property" : "properties")}";
}
