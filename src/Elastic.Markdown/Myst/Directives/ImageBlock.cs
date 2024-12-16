// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives;

public class ImageBlock(DirectiveBlockParser parser, Dictionary<string, string> properties, ParserContext context)
	: DirectiveBlock(parser, properties, context)
{
	public override string Directive => "image";

	/// <summary>
	/// Alternate text: a short description of the image, displayed by applications that cannot display images,
	/// or spoken by applications for visually impaired users.
	/// </summary>
	public string? Alt { get; set; }

	/// <summary>
	/// The desired height of the image. Used to reserve space or scale the image vertically. When the “scale” option
	/// is also specified, they are combined. For example, a height of 200px and a scale of 50 is equivalent to
	/// a height of 100px with no scale.
	/// </summary>
	public string? Height { get; set; }

	/// <summary>
	/// The width of the image. Used to reserve space or scale the image horizontally. As with “height” above,
	/// when the “scale” option is also specified, they are combined.
	/// </summary>
	public string? Width { get; set; }

	/// <summary>
	/// The uniform scaling factor of the image. The default is “100 %”, i.e. no scaling.
	/// </summary>
	public string? Scale { get; set; }

	/// <summary>
	/// The values “top”, “middle”, and “bottom” control an image’s vertical alignment
	/// The values “left”, “center”, and “right” control an image’s horizontal alignment, allowing the image to float
	/// and have the text flow around it.
	/// </summary>
	public string? Align { get; set; }

	/// <summary>
	/// Makes the image into a hyperlink reference (“clickable”).
	/// </summary>
	public string? Target { get; set; }

	public string? ImageUrl { get; private set; }

	public bool Found { get; private set; }

	public string? Label { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		Label = Prop("label", "name");
		Alt = Prop("alt");
		Align = Prop("align");

		Height = Prop("height", "h");
		Width = Prop("width", "w");

		Scale = Prop("scale");
		Target = Prop("target");

		ExtractImageUrl(context);

	}

	private void ExtractImageUrl(ParserContext context)
	{
		var imageUrl = Arguments;
		if (string.IsNullOrWhiteSpace(imageUrl))
		{
			this.EmitError($"{Directive} requires an argument.");
			return;
		}

		if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) && uri.Scheme.StartsWith("http"))
		{
			this.EmitWarning($"{Directive} is using an external URI: {uri} ");
			Found = true;
			ImageUrl = imageUrl;
			return;
		}

		var includeFrom = context.Path.Directory!.FullName;
		if (imageUrl.StartsWith('/'))
			includeFrom = context.Parser.SourcePath.FullName;

		ImageUrl = imageUrl;
		var imagePath = Path.Combine(includeFrom, imageUrl.TrimStart('/'));
		if (context.Build.ReadFileSystem.File.Exists(imagePath))
			Found = true;
		else
			this.EmitError($"`{imageUrl}` does not exist. resolved to `{imagePath}");
	}
}


public class FigureBlock(DirectiveBlockParser parser, Dictionary<string, string> properties, ParserContext context)
	: ImageBlock(parser, properties, context);
