// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Documentation.Extensions;

namespace Elastic.Markdown.Myst.Directives.Image;

public class ImageViewModel : DirectiveViewModel
{
	public required string? Label { get; init; }
	public required string? Align { get; init; }
	public required string Alt { get; init; }
	public required string? Title { get; init; }
	public required string? Height { get; init; }
	public required string? Scale { get; init; }
	public required string? Target { get; init; }
	public required string? Width { get; init; }
	public required string? ImageUrl { get; init; }

	private string? _uniqueImageId;

	public string UniqueImageId =>
		_uniqueImageId ??= string.IsNullOrEmpty(ImageUrl)
			? Guid.NewGuid().ToString("N")[..8] // fallback to a random ID if ImageUrl is null or empty
			: ShortId.Create(ImageUrl);
	public required string? Screenshot { get; init; }

	public string Style
	{
		get
		{
			var sb = new StringBuilder();
			if (Height != null)
				_ = sb.Append($"height: {Height};");
			if (Width != null)
				_ = sb.Append($"width: {Width};");
			return sb.ToString();
		}
	}
}
