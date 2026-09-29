// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Catalog;
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
	/// <summary>Parses and validates a <c>related-learning.yml</c> document. Throws on invalid input; <see cref="Loading.RelatedLearningLoader"/> turns that into a diagnostic.</summary>
	public static RelatedLearningCatalog Parse(string yaml)
	{
		var dto = ConfigurationFileProvider.Deserializer.Deserialize<RelatedLearningConfigDto>(yaml)
			?? throw new InvalidOperationException("related-learning.yml deserialized to null.");
		return FromDto(dto);
	}

	internal static RelatedLearningCatalog FromDto(RelatedLearningConfigDto dto)
	{
		var links = new Dictionary<string, RelatedLearningLink>(dto.Links.Count, StringComparer.Ordinal);
		foreach (var (id, linkDto) in dto.Links)
		{
			if (string.IsNullOrWhiteSpace(linkDto.Title))
				throw new InvalidOperationException($"related-learning.yml link '{id}' is missing required 'title'.");
			if (string.IsNullOrWhiteSpace(linkDto.Url))
				throw new InvalidOperationException($"related-learning.yml link '{id}' is missing required 'url'.");
			if (!IsAbsoluteHttpUrl(linkDto.Url))
			{
				throw new InvalidOperationException(
					$"related-learning.yml link '{id}' has invalid url '{linkDto.Url}'. " +
						"Every url must be an absolute http or https URL."
				);
			}

			links[id] = new RelatedLearningLink { Id = id, Title = linkDto.Title, Url = linkDto.Url };
		}

		return new RelatedLearningCatalog { Links = links.ToFrozenDictionary(StringComparer.Ordinal) };
	}

	private static bool IsAbsoluteHttpUrl(string url) =>
		Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
