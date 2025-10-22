// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Synonyms;

public record SynonymsConfiguration
{
	public required IReadOnlyCollection<string> Synonyms { get; init; }
}

internal sealed record SynonymsConfigDto
{
	public List<string> Synonyms { get; set; } = [];
}

public static class SynonymsConfigurationExtensions
{
	public static SynonymsConfiguration CreateSynonymsConfiguration(this ConfigurationFileProvider provider)
	{
		var synonymsFile = provider.SynonymsFile;
		var synonymsDto = ConfigurationFileProvider.Deserializer.Deserialize<SynonymsConfigDto>(synonymsFile.OpenText());
		return new SynonymsConfiguration { Synonyms = synonymsDto.Synonyms };
	}
}
