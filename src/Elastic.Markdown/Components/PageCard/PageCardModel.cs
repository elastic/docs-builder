// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Components.PageCard;

/// <summary>A whole-row link to another page: a title, an optional description and an arrow.</summary>
public sealed record PageCardModel
{
	public required string Title { get; init; }
	public required string Url { get; init; }
	public IHtmlContent? Description { get; init; }

	/// <summary>The htmx preload hint, left out when the link is not boosted.</summary>
	public string? Preload { get; init; }

	/// <summary>Set by listings so script can filter the card by group and title.</summary>
	public string? Group { get; init; }

	public bool InListing { get; init; }

	public bool HasDescription => Description is not null && !string.IsNullOrWhiteSpace(RenderedDescription);

	private string RenderedDescription
	{
		get
		{
			if (Description is null)
				return string.Empty;
			using var writer = new StringWriter();
			Description.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
			return writer.ToString();
		}
	}
}
