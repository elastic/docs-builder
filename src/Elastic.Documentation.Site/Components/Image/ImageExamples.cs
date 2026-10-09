// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Documentation.Site.Components.Image;

public static class ImageExamples
{
	private const string Pixel =
		"data:image/svg+xml;utf8,%3Csvg xmlns='http://www.w3.org/2000/svg' width='640' height='320'%3E%3Crect width='640' height='320' fill='%23999'/%3E%3C/svg%3E";

	private static ImageModel Sample(string id, string alt) => new() { Id = id, Src = Pixel, Alt = alt };

	public static ComponentSection Images { get; } = new(
		"Image",
		"Images open full size in a native dialog. Without script the link opens the image itself. Figures add a caption and an anchor; carousels scroll and snap.",
		"image",
		[
			ComponentExample.Of("Image", _Image.Create, Sample("example-image", "A placeholder image")),
			ComponentExample.Of(
				"Screenshot",
				_Image.Create,
				Sample("example-screenshot", "A placeholder screenshot") with { Screenshot = true }
			),
			ComponentExample.Of(
				"Figure",
				_Figure.Create,
				new FigureModel
				{
					Image = Sample("example-figure", "A placeholder figure"),
					Label = "example-figure-anchor",
					Caption = new HtmlString("A figure with a caption")
				}
			),
			ComponentExample.Of(
				"Carousel",
				_ImageCarousel.Create,
				new ImageCarouselModel
				{
					Images =
					[
						Sample("example-carousel-1", "First slide"),
						Sample("example-carousel-2", "Second slide"),
						Sample("example-carousel-3", "Third slide")
					],
					MaxHeight = "350px"
				}
			)
		]
	);
}
