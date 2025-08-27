// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.InlineParsers;

namespace Elastic.Markdown.Myst.Directives.Image;

public class FigureBlock(DirectiveBlockParser parser, ParserContext context) : ImageBlock(parser, context);

public class ImageBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "image";

	/// <summary>
	/// Alternate text: a short description of the image, displayed by applications that cannot display images,
	/// or spoken by applications for visually impaired users.
	/// </summary>
	public string? Alt { get; set; }

	/// <summary>
	/// Title text: a short description of the image
	/// </summary>
	public string? Title { get; set; }

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
	/// When set, adds a custom screenshot class to the image.
	/// </summary>
	public string? Screenshot { get; set; }

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

	private static readonly HashSet<string> AllowedUriHosts = ["epr.elastic.co"];

	public override void FinalizeAndValidate(ParserContext context)
	{
		// Call the DirectiveBlock's FinalizeAndValidate
		// for setup common to all the directive blocks
		base.FinalizeAndValidate(context);

		Label = Prop("label", "name");
		Alt = Prop("alt")?.ReplaceSubstitutions(context) ?? string.Empty;
		// Use Alt as Title if no explicit Title is provided
		var explicitTitle = Prop("title")?.ReplaceSubstitutions(context);
		Title = string.IsNullOrEmpty(explicitTitle) ? Alt : explicitTitle;

		Align = Prop("align");
		Height = Prop("height", "h");
		Width = Prop("width", "w");

		Scale = Prop("scale");
		Target = Prop("target");

		// Set Screenshot to "screenshot" if the :screenshot: option is present
		Screenshot = Prop("screenshot") != null ? "screenshot" : null;

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
			if (!AllowedUriHosts.Contains(uri.Host))
				this.EmitWarning($"{Directive} is using an external URI: {uri} ");

			Found = true;
			ImageUrl = imageUrl;
			return;
		}

		ImageUrl = DiagnosticLinkInlineParser.UpdateRelativeUrl(context, imageUrl);

		var file = DiagnosticLinkInlineParser.ResolveFile(context, imageUrl);
		if (file.Exists)
			Found = true;
		else
			this.EmitError($"`{imageUrl}` does not exist. resolved to `{file}");

		if (context.DocumentationFileLookup(context.MarkdownSourcePath) is MarkdownFile currentMarkdown)
		{
			if (!file.Directory!.FullName.StartsWith(currentMarkdown.ScopeDirectory.FullName + Path.DirectorySeparatorChar))
				this.EmitWarning($"Image '{imageUrl}' is referenced out of table of contents scope '{currentMarkdown.ScopeDirectory}'.");
		}
	}
}
