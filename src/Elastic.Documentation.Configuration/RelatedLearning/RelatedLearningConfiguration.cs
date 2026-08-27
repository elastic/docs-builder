// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.RelatedLearning;

/// <summary>
/// Global catalog of named elastic.co learning destinations. Pages opt in with
/// the <c>{related-learning}</c> directive by catalog ID.
/// </summary>
public record RelatedLearningConfiguration
{
	public static RelatedLearningConfiguration Empty { get; } = new() { Links = FrozenDictionary<string, RelatedLearningLink>.Empty };

	/// <summary>Catalog entries keyed by ID.</summary>
	public required IReadOnlyDictionary<string, RelatedLearningLink> Links { get; init; }

	public bool TryGet(string id, [NotNullWhen(true)] out RelatedLearningLink? link) => Links.TryGetValue(id, out link);

	/// <summary>Parses and validates a <c>related-learning.yml</c> document.</summary>
	public static RelatedLearningConfiguration Parse(string yaml)
	{
		var dto = ConfigurationFileProvider.Deserializer.Deserialize<RelatedLearningConfigDto>(yaml)
			?? throw new InvalidOperationException("related-learning.yml deserialized to null.");
		return RelatedLearningConfigurationExtensions.FromDto(dto);
	}
}

/// <summary>A single named learning destination from <c>related-learning.yml</c>.</summary>
public record RelatedLearningLink
{
	public required string Id { get; init; }
	public required string Title { get; init; }
	public required string Url { get; init; }
}

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

public static class RelatedLearningConfigurationExtensions
{
	public static RelatedLearningConfiguration CreateRelatedLearningConfiguration(this ConfigurationFileProvider provider)
	{
		using var reader = provider.RelatedLearningFile.OpenText();
		return RelatedLearningConfiguration.Parse(reader.ReadToEnd());
	}

	internal static RelatedLearningConfiguration FromDto(RelatedLearningConfigDto dto)
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

		return new RelatedLearningConfiguration { Links = links.ToFrozenDictionary(StringComparer.Ordinal) };
	}

	private static bool IsAbsoluteHttpUrl(string url) =>
		Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
