// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Diagnostics;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.RelatedLearning;

[YamlSerializable]
internal sealed class RelatedLearningConfigDto
{
	[YamlMember(Alias = "links")]
	public Dictionary<string, RelatedLearningLinkDto> Links { get; set; } = [];
}

[YamlSerializable]
internal sealed class RelatedLearningLinkDto
{
	[YamlMember(Alias = "title")]
	public string Title { get; set; } = string.Empty;

	[YamlMember(Alias = "url")]
	public string Url { get; set; } = string.Empty;
}

internal static class RelatedLearningConfigurationExtensions
{
	/// <summary>Parses a <c>related-learning.yml</c> document and throws a <see cref="ConfigurationException"/> listing every <see cref="RelatedLearningConstraints"/> error.</summary>
	public static RelatedLearningCatalog Parse(string yaml) =>
		Result.From(RelatedLearningConstraints.All.Report(ParseUnchecked(yaml)), "related-learning.yml").OrThrow();

	/// <summary>Parses a <c>related-learning.yml</c> document without checking it. Throws only on YAML it cannot read.</summary>
	public static RelatedLearningCatalog ParseUnchecked(string yaml)
	{
		var dto = ConfigurationFileProvider.Deserializer.Deserialize<RelatedLearningConfigDto>(yaml)
			?? throw new InvalidOperationException("related-learning.yml deserialized to null.");
		return FromDto(dto);
	}

	internal static RelatedLearningCatalog FromDto(RelatedLearningConfigDto dto)
	{
		var links = new Dictionary<string, RelatedLearningLink>(dto.Links.Count, StringComparer.Ordinal);
		foreach (var (id, linkDto) in dto.Links)
			links[id] = new RelatedLearningLink { Id = id, Title = linkDto.Title, Url = linkDto.Url };
		return new RelatedLearningCatalog { Links = links.ToFrozenDictionary(StringComparer.Ordinal) };
	}
}
