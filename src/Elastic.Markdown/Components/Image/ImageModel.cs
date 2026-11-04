// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Components.Image;

/// <summary>An image that opens full size in a lightbox. Without script the link opens the image itself.</summary>
public sealed record ImageModel
{
	public required string Src { get; init; }

	/// <summary>Unique on the page; ties the link to its lightbox dialog.</summary>
	public required string Id { get; init; }

	public string Alt { get; init; } = string.Empty;
	public string? Title { get; init; }
	public string? Style { get; init; }

	/// <summary>Marks a screenshot, which gets a frame.</summary>
	public bool Screenshot { get; init; }

	public string DialogId => $"lightbox-{Id}";

	public string ImageClass => Screenshot ? "image__img screenshot" : "image__img";
}

/// <summary>A numbered figure: the image, a caption and an anchor to link to it.</summary>
public sealed record FigureModel
{
	public required ImageModel Image { get; init; }
	public string? Label { get; init; }
	public required IHtmlContent Caption { get; init; }
}

/// <summary>Images in a row that scrolls and snaps. The previous and next controls need script, so they stay hidden until it runs.</summary>
public sealed record ImageCarouselModel
{
	public required IReadOnlyList<ImageModel> Images { get; init; }

	/// <summary>A CSS length, or <c>null</c> for the natural height of each image.</summary>
	public string? MaxHeight { get; init; }

	public string? StyleAttribute => MaxHeight is { Length: > 0 } height ? $"--carousel-max-height: {height};" : null;
}
