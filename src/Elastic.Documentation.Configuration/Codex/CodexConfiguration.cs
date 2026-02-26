// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Codex;

/// <summary>
/// Configuration for a documentation codex that composes multiple isolated documentation sets.
/// </summary>
public record CodexConfiguration
{
	/// <summary>
	/// The URL prefix for all codex URLs (e.g., "/internal-docs").
	/// All documentation sets will be served under this prefix.
	/// Set to "/" or empty for no prefix (site hosted at root, e.g., codex.elastic.dev).
	/// </summary>
	[YamlMember(Alias = "site_prefix")]
	public string SitePrefix { get; set; } = "/";

	/// <summary>
	/// The environment name for this codex (e.g., "engineering", "security").
	/// Used as part of the Elasticsearch index namespace.
	/// </summary>
	[YamlMember(Alias = "environment")]
	public string? Environment { get; set; }

	/// <summary>
	/// The title displayed on the codex index page.
	/// </summary>
	[YamlMember(Alias = "title")]
	public string Title { get; set; } = "Documentation Codex";

	/// <summary>
	/// Predefined groups with id, name, description, and icon. Documentation sets reference groups by id.
	/// </summary>
	[YamlMember(Alias = "groups")]
	public IReadOnlyList<CodexGroupDefinition> Groups { get; set; } = [];

	/// <summary>
	/// The base URL for canonical links and frontmatter URLs (e.g., "https://codex.elastic.dev").
	/// Used by the LLM markdown exporter, canonical link tags, and report-issue links.
	/// </summary>
	[YamlMember(Alias = "canonical_base_url")]
	public string? CanonicalBaseUrl { get; set; }

	/// <summary>
	/// Deserializes a codex configuration from YAML content.
	/// </summary>
	public static CodexConfiguration Deserialize(string yaml)
	{
		using var input = new StringReader(yaml);
		var config = ConfigurationFileProvider.Deserializer.Deserialize<CodexConfiguration>(input);
		return NormalizeConfiguration(config);
	}

	/// <summary>
	/// Loads a codex configuration from a file.
	/// </summary>
	public static CodexConfiguration Load(IFileInfo file)
	{
		var yaml = file.OpenText().ReadToEnd();
		return Deserialize(yaml);
	}

	/// <summary>
	/// Loads a codex configuration from a file path.
	/// </summary>
	public static CodexConfiguration Load(IFileSystem fileSystem, string path)
	{
		var file = fileSystem.FileInfo.New(path);
		if (!file.Exists)
			throw new FileNotFoundException($"Codex configuration file not found: {path}", path);

		return Load(file);
	}

	private static CodexConfiguration NormalizeConfiguration(CodexConfiguration config)
	{
		// Normalize site prefix: empty or "/" means root (no prefix)
		var sitePrefix = config.SitePrefix?.Trim() ?? "";
		if (string.IsNullOrEmpty(sitePrefix) || sitePrefix == "/")
		{
			// Root prefix - no path segment
			sitePrefix = "";
		}
		else
		{
			// Ensure it starts with / and doesn't end with /
			if (!sitePrefix.StartsWith('/'))
				sitePrefix = "/" + sitePrefix;
			sitePrefix = sitePrefix.TrimEnd('/');
		}

		return config with { SitePrefix = sitePrefix };
	}
}
