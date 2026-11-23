// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Operations;

namespace Elastic.ApiExplorer.Landing;

public record ApiGroupRowModel(ApiOverviewRow Row, string HxAttributes);

public record ApiGroupListModel(IReadOnlyList<ApiOverviewRow> Rows, string HxAttributes)
{
	private int Endpoints => Rows.Count(static row => row.Endpoint is not null);

	/// <summary>"14 endpoints", or null when the list has none to count.</summary>
	public string? EndpointsLabel => Endpoints > 0 ? CountLabel.Of(Endpoints, "endpoint") : null;
}

public record ApiIndexGroupModel(ApiOverviewSection Group, string HxAttributes)
{
	/// <summary>The group's heading row; <see cref="ApiIndexBlock"/> only lists sections that have one, with a page to link to.</summary>
	public ApiOverviewRow Heading => Group.Heading!;

	public string EndpointsLabel => CountLabel.Of(Group.Items.Count, "endpoint");
}

/// <summary>The text of a route in an overview row, one piece per segment, so the view only prints them.</summary>
public record GroupRouteModel(EndpointRow Row)
{
	/// <summary>
	/// With exactly one optional segment the route has two variants, with and without it, so that segment sits in brackets.
	/// With several, the variants need not combine freely, and brackets on each would suggest routes the API does not accept.
	/// </summary>
	public IReadOnlyList<string> Parts
	{
		get
		{
			if (Row.Segments.Count == 0)
				return ["/"];
			var bracketed = Row.Segments.Count(static segment => segment.Optional) == 1;
			return [.. Row.Segments.Select(segment => bracketed && segment.Optional ? $"[/{segment.Text}]" : "/" + segment.Text)];
		}
	}
}

internal static class CountLabel
{
	internal static string Of(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";
}
