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
		null or "" when buildType != BuildType.Isolated => "favicon.svg",
		null or "" => "favicon-isolated.svg",
		_ => "favicon-other.svg"
	};

	/// <summary>
	/// The self-hosted social card: "Codex" for Codex, "Documentation" for assembler builds with the environment label outside
	/// production. Isolated builds have none and keep the elastic.co image.
	/// </summary>
	public static string? OgImageFileName(BuildType buildType, string? environmentName) => buildType switch
	{
		BuildType.Codex => "og-codex.png",
		BuildType.Assembler => environmentName?.ToLowerInvariant() switch
		{
			null or "" or "prod" => "og-default.png",
			"staging" => "og-staging.png",
			"edge" => "og-edge.png",
			"demo" => "og-demo.png",
			"dev" or "local" => "og-dev.png",
			"preview" => "og-preview.png",
			_ => "og-other.png"
		},
		_ => null
	};
}
