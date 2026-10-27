// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Structural;
using Elastic.ApiExplorer.Types;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.ApplicabilityBadges;

namespace Elastic.ApiExplorer.Landing;

public enum OverviewRowKind
{
	ClassificationHeading,
	TagHeading,
	Operation,
	SchemaCategoryHeading,
	Schema,
	MarkdownPage
}

/// <summary>One row of the landing/tag-landing overview table, flattened from the navigation tree.</summary>
public record ApiOverviewRow
{
	public required OverviewRowKind Kind { get; init; }
	public required string Title { get; init; }
	public string? Url { get; init; }
	/// <summary>Merged method and route rows of an operation row.</summary>
	public OperationEndpoint? Endpoint { get; init; }

	/// <summary>The lifecycle badge of an operation row (beta, preview, ...) without its popover: the row is a link, and a tap on the badge must follow it.</summary>
	public ApplicableToViewModel? Availability { get; init; }

	public bool Deprecated { get; init; }

	/// <summary>The full schema id shown next to schema rows.</summary>
	public string? SchemaId { get; init; }
}

/// <summary>A heading row and the item rows under it. <see cref="Heading"/> is null for items that come before any heading.</summary>
public record ApiOverviewSection(ApiOverviewRow? Heading, IReadOnlyList<ApiOverviewRow> Items);

/// <summary>The product landing page: topic pages to read, and the sections the grouped index shows.</summary>
public record ApiOverviewIndex(IReadOnlyList<ApiOverviewRow> Topics, IReadOnlyList<ApiOverviewSection> Groups);

/// <summary>Flattens the navigation tree into overview rows so the landing views only iterate and print.</summary>
public static class ApiOverviewBuilder
{
	/// <summary>Rows for the product landing page: the full navigation tree.</summary>
	public static IReadOnlyList<ApiOverviewRow> Build(
		INavigationItem root,
		VersionsConfiguration? versionsConfig,
		bool designSystem = false
	)
	{
		var rows = new List<ApiOverviewRow>();
		AddProductRows(root, rows, versionsConfig, designSystem);
		return rows;
	}

	/// <summary>Groups the flat rows into sections: each heading row starts one, item rows join the current one.</summary>
	public static IReadOnlyList<ApiOverviewSection> Sections(IReadOnlyList<ApiOverviewRow> rows)
	{
		var sections = new List<ApiOverviewSection>();
		ApiOverviewRow? heading = null;
		var items = new List<ApiOverviewRow>();
		foreach (var row in rows)
		{
			if (!IsHeading(row))
			{
				items.Add(row);
				continue;
			}
			Flush();
			heading = row;
		}
		Flush();
		return sections;

		void Flush()
		{
			if (heading is not null || items.Count > 0)
				sections.Add(new ApiOverviewSection(heading, items));
			heading = null;
			items = [];
		}
	}

	/// <summary>
	/// Splits the pages that come before the first group (Authentication, Servers) from the grouped endpoints.
	/// They are topics to read, not endpoints to browse.
	/// </summary>
	public static ApiOverviewIndex SplitTopics(IReadOnlyList<ApiOverviewSection> sections)
	{
		var topics = new List<ApiOverviewRow>();
		var groups = new List<ApiOverviewSection>();
		foreach (var section in sections)
		{
			if (section.Heading is not null)
			{
				groups.Add(section);
				continue;
			}
			topics.AddRange(section.Items.Where(static r => r.Kind == OverviewRowKind.MarkdownPage));
			var rest = section.Items.Where(static r => r.Kind != OverviewRowKind.MarkdownPage).ToList();
			if (rest.Count > 0)
				groups.Add(section with { Items = rest });
		}
		return new ApiOverviewIndex(topics, groups);
	}

	private static bool IsHeading(ApiOverviewRow row) =>
		row.Kind is OverviewRowKind.ClassificationHeading or OverviewRowKind.TagHeading or OverviewRowKind.SchemaCategoryHeading;

	/// <summary>Rows for a tag landing page: endpoints and operations only.</summary>
	public static IReadOnlyList<ApiOverviewRow> BuildTagChildren(
		INavigationItem tagItem,
		VersionsConfiguration? versionsConfig,
		bool designSystem = false
	)
	{
		var rows = new List<ApiOverviewRow>();
		AddTagRows(tagItem, rows, versionsConfig, designSystem);
		return rows;
	}

	private static void AddProductRows(
		INavigationItem item,
		List<ApiOverviewRow> rows,
		VersionsConfiguration? versionsConfig,
		bool designSystem
	)
	{
		if (item is not INodeNavigationItem<INavigationModel, INavigationItem> node)
			return;

		foreach (var navigationItem in node.NavigationItems.Where(static item => !item.Hidden))
		{
			switch (navigationItem)
			{
				case ClassificationNavigationItem classification:
					rows.Add(new ApiOverviewRow { Kind = OverviewRowKind.ClassificationHeading, Title = classification.NavigationTitle });
					AddProductRows(classification, rows, versionsConfig, designSystem);
					break;
				case TagNavigationItem tag:
					rows.Add(new ApiOverviewRow { Kind = OverviewRowKind.TagHeading, Title = tag.NavigationTitle, Url = tag.Url });
					AddProductRows(tag, rows, versionsConfig, designSystem);
					break;
				case OperationNavigationItem operation:
					rows.Add(OperationRow(operation, versionsConfig, designSystem));
					break;
				case SchemaCategoryNavigationItem schemaCategory:
					rows.Add(new ApiOverviewRow { Kind = OverviewRowKind.SchemaCategoryHeading, Title = schemaCategory.NavigationTitle });
					AddProductRows(schemaCategory, rows, versionsConfig, designSystem);
					break;
				case SchemaNavigationItem schemaItem:
					rows.Add(new ApiOverviewRow
					{
						Kind = OverviewRowKind.Schema,
						Title = schemaItem.NavigationTitle,
						Url = schemaItem.Url,
						SchemaId = schemaItem.Model.SchemaId
					});
					break;
				case SimpleMarkdownNavigationItem markdownPage:
					rows.Add(new ApiOverviewRow
					{
						Kind = OverviewRowKind.MarkdownPage,
						Title = markdownPage.NavigationTitle,
						Url = markdownPage.Url
					});
					break;
				case StructuralNavigationItem structuralPage:
					rows.Add(new ApiOverviewRow
					{
						Kind = OverviewRowKind.MarkdownPage,
						Title = structuralPage.NavigationTitle,
						Url = structuralPage.Url
					});
					break;
				case ISidebarSeparatorNavigationItem:
				case IntroHeadingNavigationItem:
					break;
				default:
					throw new InvalidOperationException($"Unexpected type: {navigationItem.GetType().FullName}");
			}
		}
	}

	private static void AddTagRows(
		INavigationItem item,
		List<ApiOverviewRow> rows,
		VersionsConfiguration? versionsConfig,
		bool designSystem
	)
	{
		if (item is not INodeNavigationItem<INavigationModel, INavigationItem> node)
			return;

		foreach (var navigationItem in node.NavigationItems)
		{
			switch (navigationItem)
			{
				case OperationNavigationItem operation:
					rows.Add(OperationRow(operation, versionsConfig, designSystem));
					break;
				case IntroHeadingNavigationItem:
					break;
				default:
					throw new InvalidOperationException($"Unexpected type on tag landing: {navigationItem.GetType().FullName}");
			}
		}
	}

	private static ApiOverviewRow OperationRow(
		OperationNavigationItem operation,
		VersionsConfiguration? versionsConfig,
		bool designSystem
	) =>
		new()
		{
			Kind = OverviewRowKind.Operation,
			Title = operation.NavigationTitle,
			Endpoint = OperationEndpoint.Build(operation.Model, operation.Model.Operation.Description, operation.Siblings),
			Url = operation.Url,
			Availability = AvailabilityBadge.FromOperation(
				operation.Model.Operation,
				versionsConfig,
				showTooltip: false,
				designSystem: designSystem
			),
			Deprecated = operation.Model.Operation.Deprecated
		};
}
