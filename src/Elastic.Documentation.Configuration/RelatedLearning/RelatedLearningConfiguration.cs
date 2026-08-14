// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using Elastic.Documentation.Links;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.RelatedLearning;

/// <summary>
/// Global catalog of elastic.co learning links and the docs pages that should show them.
/// </summary>
public record RelatedLearningConfiguration
{
	public static RelatedLearningConfiguration Empty { get; } = new() { Links = [] };

	/// <summary>Catalog entries in file order.</summary>
	public required IReadOnlyList<RelatedLearningLink> Links { get; init; }

	/// <summary>
	/// Returns catalog links whose <see cref="RelatedLearningLink.Pages"/> include
	/// <c>{repositoryName}://{relativePath}</c>, preserving catalog file order.
	/// </summary>
	public IReadOnlyList<RelatedLearningLink> GetLinksForPage(string repositoryName, string relativePath)
	{
		if (Links.Count == 0)
			return [];

		var crossLink = $"{repositoryName}://{relativePath.Replace('\\', '/')}";
		return Links.Where(l => l.Pages.Contains(crossLink)).ToArray();
	}
}

/// <summary>A single named learning destination from <c>related-learning.yml</c>.</summary>
public record RelatedLearningLink
{
	public required string Id { get; init; }
	public required string Title { get; init; }
	public required string Url { get; init; }

	/// <summary>Qualified page cross-links (<c>{repo}://path.md</c>) that show this link.</summary>
	public IReadOnlyList<string> Pages { get; init; } = [];
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

	[YamlMember(Alias = "pages")]
	public List<string> Pages { get; set; } = [];
}

public static class RelatedLearningConfigurationExtensions
{
	public static RelatedLearningConfiguration CreateRelatedLearningConfiguration(this ConfigurationFileProvider provider)
	{
		var file = provider.RelatedLearningFile;
		if (!file.Exists)
			return RelatedLearningConfiguration.Empty;

		using var reader = file.OpenText();
		return Parse(reader.ReadToEnd());
	}

	/// <summary>Parses and validates a <c>related-learning.yml</c> document.</summary>
	public static RelatedLearningConfiguration Parse(string yaml)
	{
		var dto = ConfigurationFileProvider.Deserializer.Deserialize<RelatedLearningConfigDto>(yaml)
			?? throw new InvalidOperationException("related-learning.yml deserialized to null.");
		return FromDto(dto);
	}

	/// <summary>Parses and validates a catalog DTO. Used by tests and the file loader.</summary>
	internal static RelatedLearningConfiguration FromDto(RelatedLearningConfigDto dto)
	{
		var links = new List<RelatedLearningLink>(dto.Links.Count);
		foreach (var (id, linkDto) in dto.Links)
		{
			if (string.IsNullOrWhiteSpace(linkDto.Title))
				throw new InvalidOperationException($"related-learning.yml link '{id}' is missing required 'title'.");
			if (string.IsNullOrWhiteSpace(linkDto.Url))
				throw new InvalidOperationException($"related-learning.yml link '{id}' is missing required 'url'.");

			var pages = new List<string>(linkDto.Pages.Count);
			foreach (var page in linkDto.Pages)
			{
				if (!IsQualifiedPageCrossLink(page))
				{
					throw new InvalidOperationException(
						$"related-learning.yml link '{id}' has unqualified page '{page}'. " +
						"Every pages entry must be a cross-link of the form '{{repo}}://path.md'.");
				}
				pages.Add(page.Replace('\\', '/'));
			}

			links.Add(new RelatedLearningLink
			{
				Id = id,
				Title = linkDto.Title,
				Url = linkDto.Url,
				Pages = pages.ToImmutableArray()
			});
		}

		return new RelatedLearningConfiguration { Links = links.ToImmutableArray() };
	}

	/// <summary>
	/// A qualified page cross-link is <c>{repository}://{relativePath}</c> — same form as TOC/cross-links.
	/// </summary>
	internal static bool IsQualifiedPageCrossLink(string page)
	{
		if (!CrossLinkValidator.IsValidCrossLink(page, out _))
			return false;
		var separator = page.IndexOf("://", StringComparison.Ordinal);
		if (separator <= 0)
			return false;
		var path = page.AsSpan(separator + 3).Trim();
		return !path.IsEmpty;
	}
}
