// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Elastic.ApiExplorer.Landing;

/// <summary>The API Explorer index names every API in the singular, as in "Elasticsearch API".</summary>
public static partial class ApiNaming
{
	[GeneratedRegex(@"\bAPIs\b")]
	private static partial Regex PluralApi();

	public static bool UsesPlural(string title) => PluralApi().IsMatch(title);

	public static string Singularize(string title) => PluralApi().Replace(title, "API");
}
