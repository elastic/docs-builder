// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Portal;

/// <summary>
/// Configuration for a documentation portal that composes multiple isolated documentation sets.
/// </summary>
public record PortalConfiguration
{
	/// <summary>
	/// The URL prefix for all portal URLs (e.g., "/internal-docs").
	/// All documentation sets will be served under this prefix.
	/// </summary>
	[YamlMember(Alias = "site_prefix")]
	public string SitePrefix { get; init; } = "/docs";

	/// <summary>
	/// The title displayed on the portal index page.
	/// </summary>
	[YamlMember(Alias = "title")]
	public string Title { get; init; } = "Documentation Portal";

	/// <summary>
	/// The list of documentation sets to include in the portal.
	/// </summary>
	[YamlMember(Alias = "documentation_sets")]
	public IReadOnlyList<PortalDocumentationSetReference> DocumentationSets { get; init; } = [];

	/// <summary>
	/// Deserializes a portal configuration from YAML content.
	/// </summary>
	public static PortalConfiguration Deserialize(string yaml)
	{
		var input = new StringReader(yaml);
		var config = ConfigurationFileProvider.Deserializer.Deserialize<PortalConfiguration>(input);
		return NormalizeConfiguration(config);
	}

	/// <summary>
	/// Loads a portal configuration from a file.
	/// </summary>
	public static PortalConfiguration Load(IFileInfo file)
	{
		var yaml = file.OpenText().ReadToEnd();
		return Deserialize(yaml);
	}

	/// <summary>
	/// Loads a portal configuration from a file path.
	/// </summary>
	public static PortalConfiguration Load(IFileSystem fileSystem, string path)
	{
		var file = fileSystem.FileInfo.New(path);
		if (!file.Exists)
			throw new FileNotFoundException($"Portal configuration file not found: {path}", path);

		return Load(file);
	}

	private static PortalConfiguration NormalizeConfiguration(PortalConfiguration config)
	{
		// Normalize site prefix to ensure it starts with / and doesn't end with /
		var sitePrefix = config.SitePrefix.Trim();
		if (!sitePrefix.StartsWith('/'))
			sitePrefix = "/" + sitePrefix;
		sitePrefix = sitePrefix.TrimEnd('/');

		return config with { SitePrefix = sitePrefix };
	}

	/// <summary>
	/// Gets all unique categories defined in the documentation sets.
	/// </summary>
	[YamlIgnore]
	public IReadOnlyList<string> Categories =>
		DocumentationSets
			.Where(ds => !string.IsNullOrEmpty(ds.Category))
			.Select(ds => ds.Category!)
			.Distinct()
			.OrderBy(c => c)
			.ToList();

	/// <summary>
	/// Gets documentation sets grouped by category.
	/// Documentation sets without a category are grouped under null.
	/// </summary>
	[YamlIgnore]
	public ILookup<string?, PortalDocumentationSetReference> DocumentationSetsByCategory =>
		DocumentationSets.ToLookup(ds => ds.Category);
}
