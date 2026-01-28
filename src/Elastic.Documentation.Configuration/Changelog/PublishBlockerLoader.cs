// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Documentation.Configuration.Changelog;

/// <summary>
/// Lightweight loader for changelog publish blocker configuration.
/// Used by BuildContext to lazily load publish blockers without requiring
/// a dependency on the full Elastic.Changelog assembly.
/// </summary>
public static class PublishBlockerLoader
{
	private static readonly IDeserializer Deserializer =
		new DeserializerBuilder()
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.IgnoreUnmatchedProperties()
			.Build();

	/// <summary>
	/// Loads the publish blocker configuration from a changelog.yml file.
	/// </summary>
	/// <param name="fileSystem">The file system to read from.</param>
	/// <param name="configPath">The path to the changelog.yml configuration file.</param>
	/// <returns>The publish blocker configuration, or null if not found or not configured.</returns>
	public static PublishBlocker? Load(IFileSystem fileSystem, string configPath)
	{
		if (!fileSystem.File.Exists(configPath))
			return null;

		var yamlContent = fileSystem.File.ReadAllText(configPath);
		var yamlConfig = Deserializer.Deserialize<ChangelogConfigYamlMinimal>(yamlContent);
		if (yamlConfig?.Block?.Publish is null)
			return null;

		return ParsePublishBlocker(yamlConfig.Block.Publish);
	}

	/// <summary>
	/// Parses a PublishBlockerYaml into a PublishBlocker domain type.
	/// </summary>
	private static PublishBlocker? ParsePublishBlocker(PublishBlockerYamlMinimal? yaml)
	{
		if (yaml == null)
			return null;

		var types = yaml.Types?.Count > 0 ? yaml.Types.ToList() : null;
		var areas = yaml.Areas?.Count > 0 ? yaml.Areas.ToList() : null;

		if (types == null && areas == null)
			return null;

		return new PublishBlocker
		{
			Types = types,
			Areas = areas
		};
	}

	#region Minimal YAML DTOs

	/// <summary>
	/// Minimal DTO for changelog configuration - only includes block configuration.
	/// </summary>
	private sealed class ChangelogConfigYamlMinimal
	{
		public BlockConfigYamlMinimal? Block { get; set; }
	}

	/// <summary>
	/// Minimal DTO for block configuration.
	/// </summary>
	private sealed class BlockConfigYamlMinimal
	{
		public PublishBlockerYamlMinimal? Publish { get; set; }
	}

	/// <summary>
	/// Minimal DTO for publish blocker configuration.
	/// </summary>
	private sealed class PublishBlockerYamlMinimal
	{
		public List<string>? Types { get; set; }
		public List<string>? Areas { get; set; }
	}

	#endregion
}
