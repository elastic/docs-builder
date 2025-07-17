// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Documentation.Extensions;
using Elastic.Markdown.Myst.Directives.Image;

namespace Elastic.Markdown.Myst.Directives.Image;

public class ImageCarouselViewModel : DirectiveViewModel
{
	public required List<ImageViewModel> Images { get; init; }
	public string? FixedHeight { get; init; }
}
