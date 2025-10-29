// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;

namespace Elastic.Documentation.Configuration.Synonyms;

public record SynonymsConfiguration
{
	public required IReadOnlyCollection<string> Synonyms { get; init; }
}

internal sealed record SynonymsConfigDto
{
	public List<List<string>> Synonyms { get; set; } = [];
}

public static class SynonymsConfigurationExtensions
{
	public static SynonymsConfiguration CreateSynonymsConfiguration(this ConfigurationFileProvider provider)
	{
		var synonymsFile = provider.SynonymsFile;

		if (!synonymsFile.Exists)
			return new SynonymsConfiguration { Synonyms = [] };

		var synonymsDto = ConfigurationFileProvider.Deserializer.Deserialize<SynonymsConfigDto>(synonymsFile.OpenText());
		var flattenedSynonyms = synonymsDto.Synonyms.Select(sl => string.Join(',', sl)).ToImmutableArray();
		return new SynonymsConfiguration { Synonyms = flattenedSynonyms };
	}
}
