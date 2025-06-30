// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Documentation.Configuration.Versions;

public static class VersionsConfigurationExtensions
{
	private static readonly string ResourceName = "Elastic.Documentation.Configuration.versions.yml";

	public static IServiceCollection AddVersions(this IServiceCollection services)
	{
		var assembly = typeof(VersionsConfigurationExtensions).Assembly;
		using var stream = assembly.GetManifestResourceStream(ResourceName) ?? throw new FileNotFoundException(ResourceName);
		using var reader = new StreamReader(stream);

		var deserializer = new StaticDeserializerBuilder(new YamlStaticContext())
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.Build();

		var dto = deserializer.Deserialize<VersionsConfigDto>(reader);

		var versions = dto.VersioningSystems.ToDictionary(
			kvp => ToVersioningSystemId(kvp.Key),
			kvp => new VersioningSystem
			{
				Id = ToVersioningSystemId(kvp.Key),
				Base = ToSemVersion(kvp.Value.Base),
				Current = ToSemVersion(kvp.Value.Current)
			});
		var config = new VersionsConfiguration { VersioningSystems = versions };

		_ = services.AddSingleton<IOptions<VersionsConfiguration>>(new OptionsWrapper<VersionsConfiguration>(config));

		return services;
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
	public Dictionary<string, VersioningSystemDto> VersioningSystems { get; set; } = [];
}

internal sealed record VersioningSystemDto
{
	public string Base { get; set; } = string.Empty;
	public string Current { get; set; } = string.Empty;
}
