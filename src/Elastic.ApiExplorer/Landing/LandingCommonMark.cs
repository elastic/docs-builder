// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Operations;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Landing;

internal static class LandingCommonMark
{
	internal const string OtherApisHeading = "Other APIs";

	public static string Catalog(IReadOnlyList<ApiCatalogEntry> entries)
	{
		var markdown = new StringBuilder();
		ApiCommonMark.Heading(markdown, 1, ApiCatalog.PageTitle);
		var groups = ApiCatalogViewModel.Group(entries);
		foreach (var group in groups)
		{
			// The page has no label for the APIs that no group covers. In Markdown they need a heading, or they read
			// as part of the group above them. A lone group needs none.
			var heading = group.Title ?? (groups.Count > 1 ? OtherApisHeading : null);
			if (heading is not null)
				ApiCommonMark.Heading(markdown, 2, heading);
			foreach (var entry in group.Entries)
				WriteCatalogEntry(markdown, entry);
		}
		return markdown.ToString();
	}

	private static void WriteCatalogEntry(StringBuilder markdown, ApiCatalogEntry entry)
	{
		_ = markdown.AppendLine($"- {ApiCommonMark.Link(entry.Title, entry.Url)} (`{entry.Key}`)");
		if (ApiSeoDescription.Excerpt(entry.Description) is { } summary)
			_ = markdown.AppendLine($"  {summary}");
		if (ApiCatalogViewModel.DeploymentsOf(entry) is { Count: > 0 } deployments)
			_ = markdown.AppendLine($"  Deployments: {string.Join(", ", deployments.Select(d => d.Name))}");
		_ = markdown.AppendLine(
			$"  {ApiCommonMark.Link("Markdown", ApiOutputPaths.MarkdownUrl(entry.Url))} · {ApiCommonMark.Link("JSON", ApiOutputPaths.JsonUrl(entry.Url))} · {ApiCommonMark.Link("YAML", ApiOutputPaths.YamlUrl(entry.Url))}"
		);
	}

	public static string Product(OpenApiInfo? info, IReadOnlyList<ApiOverviewRow> rows, string apiBaseUrl)
	{
		var markdown = new StringBuilder();
		ApiCommonMark.Heading(markdown, 1, info?.Title ?? "API Documentation");
		ApiCommonMark.Prepared(markdown, info?.Description, apiBaseUrl);
		if (!string.IsNullOrEmpty(info?.License?.Name))
			ApiCommonMark.Paragraph(markdown, $"License: {info.License.Name}");

		WriteOverview(markdown, rows);
		return markdown.ToString();
	}

	public static string Tag(TagLandingViewModel model)
	{
		var markdown = new StringBuilder();
		var apiBaseUrl = model.CurrentNavigationItem.NavigationRoot.Url;
		ApiCommonMark.Heading(markdown, 1, model.Tag.DisplayName);
		if (!string.Equals(model.Tag.Name, model.Tag.DisplayName, StringComparison.Ordinal))
			ApiCommonMark.Paragraph(markdown, $"`{model.Tag.Name}`");

		ApiCommonMark.Prepared(markdown, model.DescriptionMarkdown, apiBaseUrl);
		foreach (var extra in model.PostSections)
		{
			ApiCommonMark.Heading(markdown, 3, extra.Heading);
			ApiCommonMark.Prepared(markdown, extra.BodyMarkdown, apiBaseUrl);
		}

		if (model.ExternalDocsDisplay is { } docs)
			ApiCommonMark.Paragraph(markdown, ApiCommonMark.Link(docs.LinkText, docs.Url));

		WriteOverview(markdown, model.OverviewRows);
		return markdown.ToString();
	}

	private static void WriteOverview(StringBuilder markdown, IReadOnlyList<ApiOverviewRow> rows)
	{
		foreach (var row in rows)
		{
			switch (row.Kind)
			{
				case OverviewRowKind.ClassificationHeading:
					ApiCommonMark.Heading(markdown, 2, row.Title);
					break;
				case OverviewRowKind.TagHeading:
					ApiCommonMark.Heading(markdown, 3, ApiCommonMark.Link(row.Title, row.Url));
					break;
				case OverviewRowKind.SchemaCategoryHeading:
					ApiCommonMark.Heading(markdown, 3, row.Title);
					break;
				case OverviewRowKind.Schema:
					_ = markdown.AppendLine($"- {ApiCommonMark.Link(row.Title, row.Url)} (`{row.SchemaId}`)");
					break;
				case OverviewRowKind.MarkdownPage:
					_ = markdown.AppendLine($"- {ApiCommonMark.Link(row.Title, row.Url)}");
					break;
				case OverviewRowKind.Operation:
					WriteOperationRow(markdown, row);
					break;
			}
		}
	}

	private static void WriteOperationRow(StringBuilder markdown, ApiOverviewRow row)
	{
		if (row.Endpoint is not { } endpoint || row.Url is null)
		{
			_ = markdown.AppendLine($"- {row.Title}");
			return;
		}

		var paths = string.Join(", ", endpoint.Rows.Select(OperationCommonMark.PathLabel));
		_ = markdown.AppendLine($"- {ApiCommonMark.Link(row.Title, row.Url)}: {paths}");
	}
}
