// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer;
using Elastic.ApiExplorer.Schema;

namespace Elastic.ApiExplorer.Landing;

/// <summary>Display form of a tag's external documentation link.</summary>
public record TagExternalDocsDisplay(string Url, bool IsElasticDocs, string LinkText);

public class TagLandingViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiTag Tag { get; init; }

	/// <summary>Flattened overview table rows; built before the slice renders.</summary>
	public required IReadOnlyList<ApiOverviewRow> OverviewRows { get; init; }

	public TagExternalDocsDisplay? ExternalDocsDisplay =>
		Tag.ExternalDocs is null
			? null
			: new TagExternalDocsDisplay(
				Tag.ExternalDocs.Url,
				ApiPropertyTreeBuilder.IsElasticDocsUrl(Tag.ExternalDocs.Url),
				string.IsNullOrWhiteSpace(Tag.ExternalDocs.Description) ? "Documentation" : Tag.ExternalDocs.Description);

	/// <inheritdoc />
	protected override string? LayoutPageTitle => Tag.DisplayName;
}
