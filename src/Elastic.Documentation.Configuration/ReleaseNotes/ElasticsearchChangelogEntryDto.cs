// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// The changelog entry format that elastic/elasticsearch and elastic/elasticsearch-serverless write to
/// <c>docs/changelog/{pr}.yaml</c>: <c>summary</c> and <c>area</c> where docs-builder uses <c>title</c> and
/// <c>areas</c>, ES type names, and optional <c>breaking</c>, <c>deprecation</c> and <c>highlight</c> blocks.
/// <see cref="ReleaseNotesSerialization.NormalizeYaml"/> converts it to <see cref="ChangelogEntryDto"/>.
/// </summary>
public record ElasticsearchChangelogEntryDto
{
	public string? Pr { get; set; }
	public string? Summary { get; set; }
	public string? Area { get; set; }
	public string? Type { get; set; }
	public List<string>? Issues { get; set; }
	public ElasticsearchChangelogNoticeDto? Breaking { get; set; }
	public ElasticsearchChangelogNoticeDto? Deprecation { get; set; }
	public ElasticsearchChangelogHighlightDto? Highlight { get; set; }

	/// <summary>The repository of <see cref="Pr"/> when it is not the repository that holds the entry.</summary>
	[SharpYaml.Serialization.YamlPropertyName("source_repo")]
	public string? SourceRepo { get; set; }
}

/// <summary>The <c>breaking</c> and <c>deprecation</c> blocks of an Elasticsearch changelog entry.</summary>
public record ElasticsearchChangelogNoticeDto
{
	public string? Area { get; set; }
	public string? Title { get; set; }
	public string? Details { get; set; }
	public string? Impact { get; set; }
	public bool? Notable { get; set; }
	[SharpYaml.Serialization.YamlPropertyName("ess_setting_change")]
	public bool? EssSettingChange { get; set; }
}

/// <summary>The <c>highlight</c> block of an Elasticsearch changelog entry.</summary>
public record ElasticsearchChangelogHighlightDto
{
	public string? Title { get; set; }
	public string? Body { get; set; }
	public bool? Notable { get; set; }
}
