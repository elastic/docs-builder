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
	public static string Catalog(IReadOnlyList<ApiCatalogEntry> entries)
	{
		var markdown = new StringBuilder();
		ApiCommonMark.Heading(markdown, 1, "API Explorer");
		foreach (var entry in entries.OrderBy(e => e.Key))
			_ = markdown.AppendLine($"- {ApiCommonMark.Link(entry.Title, entry.Url)} (`{entry.Key}`)");
		return markdown.ToString();
	}

	public static string Product(OpenApiInfo? info, LandingInfoNote note, IReadOnlyList<ApiOverviewRow> rows, string apiBaseUrl)
	{
		var markdown = new StringBuilder();
		ApiCommonMark.Heading(markdown, 1, info?.Title ?? "API Documentation");
		ApiCommonMark.Prepared(markdown, info?.Description, apiBaseUrl);
		WriteBaseUrls(markdown, note.Servers);
		if (note.LicenseName is not null)
			ApiCommonMark.Paragraph(markdown, $"License: {note.LicenseName}");
		if (note.Version is not null)
			ApiCommonMark.Paragraph(markdown, $"Version: {note.Version}");

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

	private static void WriteBaseUrls(StringBuilder markdown, IReadOnlyList<OpenApiServer> servers)
	{
		if (servers.Count == 0)
			return;

		foreach (var server in servers)
		{
			var line = $"`{server.Url}`";
			if (!string.IsNullOrEmpty(server.Description))
				line += $" ({server.Description})";
			_ = markdown.AppendLine($"- Base URL: {line}");
		}

		_ = markdown.AppendLine();
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
				case OverviewRowKind.Endpoint:
				case OverviewRowKind.Operation:
					WriteOperationRow(markdown, row);
					break;
			}
		}
	}

	private static void WriteOperationRow(StringBuilder markdown, ApiOverviewRow row)
	{
		if (row.Operations.Count == 0)
		{
			_ = markdown.AppendLine($"- {row.Title}");
			return;
		}

		foreach (var operation in row.Operations)
		{
			var method = operation.Model.OperationType.ToString().ToUpperInvariant();
			_ = markdown.AppendLine($"- {row.Title}: {ApiCommonMark.Link($"`{method}` `{operation.Model.Route}`", operation.Url)}");
		}
	}
}
