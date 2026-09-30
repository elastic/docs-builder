// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Codex;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Codex;

/// <summary>Loads <c>codex.yml</c> into a <see cref="CodexConfiguration"/>, normalizing the site prefix and environment.</summary>
public static class CodexSiteLoader
{
	public static CodexConfiguration Deserialize(string yaml)
	{
		using var input = new StringReader(yaml);
		var dto = ConfigurationFileProvider.Deserializer.Deserialize<CodexConfigurationDto>(input) ?? new CodexConfigurationDto();
		return new CodexConfiguration
		{
			SitePrefix = NormalizeSitePrefix(dto.SitePrefix),
			Environment = NormalizeEnvironment(dto.Environment),
			Title = dto.Title,
			Groups =
			[
				.. dto.Groups.Select(g => new CodexGroupDefinition { Id = g.Id, Name = g.Name, Description = g.Description, Icon = g.Icon })
			],
			CanonicalBaseUrl = dto.CanonicalBaseUrl
		};
	}

	public static CodexConfiguration Load(IFileInfo file) => Deserialize(file.OpenText().ReadToEnd());

	public static CodexConfiguration Load(IFileSystem fileSystem, string path)
	{
		var file = fileSystem.FileInfo.New(path);
		if (!file.Exists)
			throw new FileNotFoundException($"Codex configuration file not found: {path}", path);

		return Load(file);
	}

	// Empty or "/" means root (no prefix); otherwise it starts with '/' and does not end with one.
	private static string NormalizeSitePrefix(string? raw)
	{
		var sitePrefix = raw?.Trim() ?? "";
		if (string.IsNullOrEmpty(sitePrefix) || sitePrefix == "/")
			return "";
		if (!sitePrefix.StartsWith('/'))
			sitePrefix = "/" + sitePrefix;
		return sitePrefix.TrimEnd('/');
	}

	// Trim and lowercase so it maps cleanly to the lowercase Elasticsearch index namespace
	// (e.g. "INTERNAL" / " internal " -> "internal").
	private static string? NormalizeEnvironment(string? raw)
	{
		var environment = raw?.Trim().ToLowerInvariant();
		return string.IsNullOrEmpty(environment) ? null : environment;
	}
}

[YamlSerializable]
internal sealed class CodexConfigurationDto
{
	[YamlMember(Alias = "site_prefix")]
	public string SitePrefix { get; set; } = "/";

	[YamlMember(Alias = "environment")]
	public string? Environment { get; set; }

	[YamlMember(Alias = "title")]
	public string Title { get; set; } = "Elastic Internal Docs";

	[YamlMember(Alias = "groups")]
	public List<CodexGroupDefinitionDto> Groups { get; set; } = [];

	[YamlMember(Alias = "canonical_base_url")]
	public string? CanonicalBaseUrl { get; set; }
}

[YamlSerializable]
internal sealed class CodexGroupDefinitionDto
{
	[YamlMember(Alias = "id")]
	public string Id { get; set; } = string.Empty;

	[YamlMember(Alias = "name")]
	public string Name { get; set; } = string.Empty;

	[YamlMember(Alias = "description")]
	public string? Description { get; set; }

	[YamlMember(Alias = "icon")]
	public string? Icon { get; set; }
}
