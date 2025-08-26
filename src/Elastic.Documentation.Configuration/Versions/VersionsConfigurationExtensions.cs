// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Documentation.Configuration.Versions;

public static class VersionsConfigurationExtensions
{
	public static VersionsConfiguration CreateVersionConfiguration(this ConfigurationFileProvider provider)
	{
		var path = provider.VersionFile;

		var deserializer = new StaticDeserializerBuilder(new YamlStaticContext())
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.Build();

		var dto = deserializer.Deserialize<VersionsConfigDto>(path.OpenText());

		var versions = dto.VersioningSystems.ToDictionary(
			kvp => ToVersioningSystemId(kvp.Key),
			kvp => new VersioningSystem
			{
				Id = ToVersioningSystemId(kvp.Key),
				Base = ToSemVersion(kvp.Value.Base),
				Current = ToSemVersion(kvp.Value.Current)
			});
		var products = dto.Products.ToDictionary(
			kvp => kvp.Key,
			kvp => new Product
			{
				Id = kvp.Key,
				DisplayName = kvp.Value.Display,
				VersionSystem = ToVersioningSystemId(kvp.Value.VersionScheme)
			});
		var config = new VersionsConfiguration { Products = products, VersioningSystems = versions };
		return config;
	}

	private static VersioningSystemId ToVersioningSystemId(string id)
	{
		if (!VersioningSystemIdExtensions.TryParse(id, out var versioningSystemId, true, true))
			throw new InvalidOperationException($"Could not parse versioning system id {id}");
		return versioningSystemId;
	}

	private static SemVersion ToSemVersion(string semVer)
	{
		var fullVersion = semVer.Split('.').Length switch
		{
			0 => semVer + "0.0.0",
			1 => semVer + ".0.0",
			2 => semVer + ".0",
			_ => semVer
		};
		if (!SemVersion.TryParse(fullVersion, out var version))
			throw new InvalidOperationException($"Could not parse version {semVer}");
		return version;
	}
}

// Private DTOs for deserialization. These match the YAML structure directly.

internal sealed record VersionsConfigDto
{
	public Dictionary<string, ProductDto> Products { get; set; } = [];
	public Dictionary<string, VersioningSystemDto> VersioningSystems { get; set; } = [];
}

internal sealed record ProductDto
{
	public string Display { get; set; } = string.Empty;
	[YamlMember(Alias = "version_system")]
	public string VersionScheme { get; set; } = string.Empty;
}


internal sealed record VersioningSystemDto
{
	public string Base { get; set; } = string.Empty;
	public string Current { get; set; } = string.Empty;
}
