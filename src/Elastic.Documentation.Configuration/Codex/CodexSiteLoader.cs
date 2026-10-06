// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Codex;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Codex;

/// <summary>
/// Loads <c>codex.yml</c> into a <see cref="CodexConfiguration"/>, normalizing the site prefix and environment, and checks it once:
/// against <see cref="CodexConfigurationConstraints"/>, and for publishing also <see cref="CodexPublishingConstraints"/>.
/// </summary>
public static class CodexSiteLoader
{
	/// <summary>
	/// Loads and checks <paramref name="file"/>. A missing or unreadable file, or input the parser rejects, is an error diagnostic; every
	/// violation of every set rides in the result, and an error means no configuration. Diagnostics are reported against <paramref name="originalPath"/>.
	/// </summary>
	public static Result<CodexConfiguration> Load(IFileInfo file, string originalPath, bool forPublish)
	{
		var read = LoaderPipeline.Run(
			originalPath,
			parse: () =>
			{
				if (!file.Exists)
					throw new FileNotFoundException($"Codex configuration file not found: {originalPath}", originalPath);
				return ReadText(file, originalPath);
			},
			map: yaml => DeserializeOrExplain(yaml, originalPath),
			rules: CodexConfigurationConstraints.All,
			errorsAreFatal: false
		);
		if (read.Value is not { } config)
			return read;

		// the stage's own set (publishing needs an environment) is a second report over the same value: one report per set, none repeated
		var diagnostics = forPublish
			? [.. read.Diagnostics, .. CodexPublishingConstraints.All.Report(config).ToDiagnostics(originalPath)]
			: read.Diagnostics;
		return diagnostics.Any(d => d.Severity == Severity.Error)
			? Result.Failure<CodexConfiguration>(diagnostics)
			: Result.Success(config, diagnostics);
	}

	// An unreadable file (outside a scoped file system) or unparsable text keeps the message that names the file.
	private static string ReadText(IFileInfo file, string originalPath)
	{
		try
		{
			using var reader = file.OpenText();
			return reader.ReadToEnd();
		}
		catch (Exception e) when (e is not OutOfMemoryException)
		{
			throw new InvalidOperationException($"Failed to read codex configuration '{originalPath}': {e.Message}", e);
		}
	}

	private static CodexConfiguration DeserializeOrExplain(string yaml, string originalPath)
	{
		try
		{
			return Deserialize(yaml);
		}
		catch (Exception e) when (e is not OutOfMemoryException)
		{
			throw new InvalidOperationException($"Failed to read codex configuration '{originalPath}': {e.Message}", e);
		}
	}

	internal static CodexConfiguration Deserialize(string yaml)
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
