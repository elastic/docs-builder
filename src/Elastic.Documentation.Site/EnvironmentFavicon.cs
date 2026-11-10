// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;

namespace Elastic.Documentation.Site;

/// <summary>Picks the favicon file: the plain Elastic glyph in production, the glyph plus a coloured label elsewhere.</summary>
public static class EnvironmentFavicon
{
	public static string FileName(BuildType buildType, string? environmentName) => environmentName?.ToLowerInvariant() switch
	{
		"prod" => "favicon.svg",
		"staging" => "favicon-staging.svg",
		"edge" => "favicon-edge.svg",
		"demo" => "favicon-demo.svg",
		"dev" or "local" => "favicon-dev.svg",
		"preview" => "favicon-preview.svg",
		null or "" when buildType == BuildType.Assembler => "favicon.svg",
		null or "" => "favicon-isolated.svg",
		_ => "favicon-other.svg"
	};
}
