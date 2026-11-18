// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Codex;
using Elastic.Documentation.Diagnostics;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// Loads <c>codex.yml</c> into a <see cref="CodexConfiguration"/>, normalizing the site prefix and environment. A codex that is published is loaded
/// as the next stage, a <see cref="PublishedCodexConfiguration"/>, which carries the rules publishing adds.
/// </summary>
public static class CodexSiteLoader
{
	internal static readonly Loader<CodexConfigurationDto, CodexConfiguration> Read = Loader.For(
		parse: ParseOrExplain,
		map: (dto, _) => ToRecord(dto)
	);

	internal static readonly Loader<CodexConfigurationDto, PublishedCodexConfiguration> Publish = Loader.For(
		parse: ParseOrExplain,
		map: (dto, _) => new PublishedCodexConfiguration(ToRecord(dto))
	);

	/// <summary>Loads and checks the configuration text of <paramref name="file"/>: an error finding says why there is no configuration.</summary>
	public static LoadResult<CodexConfiguration> Load(string text, string file) => Read.Load(text, file);

	/// <summary>Loads and checks the configuration text of <paramref name="file"/> for publishing: it must also name its environment.</summary>
	public static LoadResult<PublishedCodexConfiguration> LoadForPublish(string text, string file) => Publish.Load(text, file);

	/// <summary>
	/// The configuration of <paramref name="file"/>. A file that cannot be read or parsed fails with the exception itself, as it always has:
	/// the findings of the model's rules are not reported here.
	/// </summary>
	public static CodexConfiguration Load(IFileInfo file)
	{
		var text = file.OpenText().ReadToEnd();
		if (Read.Load(text, file.FullName) is Loaded<CodexConfiguration> loaded)
			return loaded.Value;

		// the loader reports a parse failure as a finding; reading again raises the exception itself
		_ = ReadDto(text, file.FullName);
		throw new InvalidOperationException($"Could not load {file.Name}");
	}

	internal static CodexConfiguration Deserialize(string yaml, string? sourceName = null) => ToRecord(ReadDto(yaml, sourceName));

	private static CodexConfigurationDto ReadDto(string text, string? file) =>
		ConfigurationFileProvider.Deserialize<CodexConfigurationDto>(new StringReader(text), file);

	// An unparsable text keeps the message that names the file.
	private static CodexConfigurationDto ParseOrExplain(string text, string file)
	{
		try
		{
			return ReadDto(text, file);
		}
		catch (Exception e) when (e is not (OutOfMemoryException or OperationCanceledException))
		{
			throw new InvalidOperationException($"Failed to read codex configuration '{file}': {e.Message}", e);
		}
	}

	private static CodexConfiguration ToRecord(CodexConfigurationDto dto) =>
		new()
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

public sealed class CodexConfigurationDto
{
	[YamlPropertyName("site_prefix")]
	public string SitePrefix { get; set; } = "/";

	[YamlPropertyName("environment")]
	public string? Environment { get; set; }

	[YamlPropertyName("title")]
	public string Title { get; set; } = "Elastic Internal Docs";

	[YamlPropertyName("groups")]
	public List<CodexGroupDefinitionDto> Groups { get; set; } = [];

	[YamlPropertyName("canonical_base_url")]
	public string? CanonicalBaseUrl { get; set; }
}

public sealed class CodexGroupDefinitionDto
{
	[YamlPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[YamlPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[YamlPropertyName("description")]
	public string? Description { get; set; }

	[YamlPropertyName("icon")]
	public string? Icon { get; set; }
}
