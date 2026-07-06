// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Schemas;
using Elastic.Documentation.Navigation;

namespace Elastic.ApiExplorer.Landing;

public enum OverviewRowKind
{
	ClassificationHeading,
	TagHeading,
	Endpoint,
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
	public IReadOnlyCollection<OperationNavigationItem> Operations { get; init; } = [];

	/// <summary>The full schema id shown next to schema rows.</summary>
	public string? SchemaId { get; init; }
}

/// <summary>Flattens the navigation tree into overview rows so the landing views only iterate and print.</summary>
public static class ApiOverviewBuilder
{
	/// <summary>Rows for the product landing page: the full navigation tree.</summary>
	public static IReadOnlyList<ApiOverviewRow> Build(INavigationItem root)
	{
		var rows = new List<ApiOverviewRow>();
		AddProductRows(root, rows);
		return rows;
	}

	/// <summary>Rows for a tag landing page: endpoints and operations only.</summary>
	public static IReadOnlyList<ApiOverviewRow> BuildTagChildren(INavigationItem tagItem)
	{
		var rows = new List<ApiOverviewRow>();
		AddTagRows(tagItem, rows);
		return rows;
	}

	private static void AddProductRows(INavigationItem item, List<ApiOverviewRow> rows)
	{
		if (item is not INodeNavigationItem<INavigationModel, INavigationItem> node)
			return;

		foreach (var navigationItem in node.NavigationItems)
		{
			switch (navigationItem)
			{
				case ClassificationNavigationItem classification:
					rows.Add(new ApiOverviewRow { Kind = OverviewRowKind.ClassificationHeading, Title = classification.NavigationTitle });
					AddProductRows(classification, rows);
					break;
				case TagNavigationItem tag:
					rows.Add(new ApiOverviewRow { Kind = OverviewRowKind.TagHeading, Title = tag.NavigationTitle, Url = tag.Url });
					AddProductRows(tag, rows);
					break;
				case EndpointNavigationItem endpoint:
					AddEndpointRow(endpoint, rows, AddProductRows);
					break;
				case OperationNavigationItem operation:
					rows.Add(new ApiOverviewRow { Kind = OverviewRowKind.Operation, Title = operation.NavigationTitle, Operations = [operation] });
					break;
				case SchemaCategoryNavigationItem schemaCategory:
					rows.Add(new ApiOverviewRow { Kind = OverviewRowKind.SchemaCategoryHeading, Title = schemaCategory.NavigationTitle });
					AddProductRows(schemaCategory, rows);
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
					rows.Add(new ApiOverviewRow { Kind = OverviewRowKind.MarkdownPage, Title = markdownPage.NavigationTitle, Url = markdownPage.Url });
					break;
				default:
					throw new InvalidOperationException($"Unexpected type: {navigationItem.GetType().FullName}");
			}
		}
	}

	private static void AddTagRows(INavigationItem item, List<ApiOverviewRow> rows)
	{
		if (item is not INodeNavigationItem<INavigationModel, INavigationItem> node)
			return;

		foreach (var navigationItem in node.NavigationItems)
		{
			switch (navigationItem)
			{
				case EndpointNavigationItem endpoint:
					AddEndpointRow(endpoint, rows, AddTagRows);
					break;
				case OperationNavigationItem operation:
					rows.Add(new ApiOverviewRow { Kind = OverviewRowKind.Operation, Title = operation.NavigationTitle, Operations = [operation] });
					break;
				default:
					throw new InvalidOperationException($"Unexpected type on tag landing: {navigationItem.GetType().FullName}");
			}
		}
	}

	private static void AddEndpointRow(EndpointNavigationItem endpoint, List<ApiOverviewRow> rows, Action<INavigationItem, List<ApiOverviewRow>> recurse)
	{
		var endpointOperations = endpoint is { NavigationItems.Count: > 0 } && endpoint.NavigationItems.All(n => n.Hidden)
			? endpoint.NavigationItems
			: [];
		if (endpointOperations.Count > 0)
			rows.Add(new ApiOverviewRow { Kind = OverviewRowKind.Endpoint, Title = endpoint.NavigationTitle, Operations = endpointOperations });
		else
			recurse(endpoint, rows);
	}
}
