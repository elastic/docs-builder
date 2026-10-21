// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Diagnostics;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.RelatedLearning;

internal sealed class RelatedLearningConfigDto
{
	[YamlPropertyName("links")]
	public Dictionary<string, RelatedLearningLinkDto> Links { get; set; } = [];
}

internal sealed class RelatedLearningLinkDto
{
	[YamlPropertyName("title")]
	public string Title { get; set; } = string.Empty;

	[YamlPropertyName("url")]
	public string Url { get; set; } = string.Empty;
}

internal static class RelatedLearningConfigurationExtensions
{
	/// <summary>Parses a <c>related-learning.yml</c> document and throws a <see cref="ConfigurationException"/> listing every <see cref="RelatedLearningConstraints"/> error.</summary>
	public static RelatedLearningCatalog Parse(string yaml, string? sourceName = null) =>
		Result.From(RelatedLearningConstraints.All.Report(ParseUnchecked(yaml, sourceName)), "related-learning.yml").OrThrow();

	/// <summary>Parses a <c>related-learning.yml</c> document without checking it. Throws only on YAML it cannot read.</summary>
	internal static RelatedLearningCatalog ParseUnchecked(string yaml) => ParseUnchecked(yaml, null);

	/// <summary>Parses a <c>related-learning.yml</c> document without checking it, naming <paramref name="sourceName"/> in unknown-key errors.</summary>
	internal static RelatedLearningCatalog ParseUnchecked(string yaml, string? sourceName) =>
		FromDto(ConfigurationFileProvider.Deserialize<RelatedLearningConfigDto>(yaml, sourceName));

	internal static RelatedLearningCatalog FromDto(RelatedLearningConfigDto dto)
	{
		var links = new Dictionary<string, RelatedLearningLink>(dto.Links.Count, StringComparer.Ordinal);
		foreach (var (id, linkDto) in dto.Links)
			links[id] = new RelatedLearningLink { Id = id, Title = linkDto.Title, Url = linkDto.Url };
		return new RelatedLearningCatalog { Links = links.ToFrozenDictionary(StringComparer.Ordinal) };
	}
}
