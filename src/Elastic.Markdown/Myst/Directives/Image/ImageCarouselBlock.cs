// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.Image;

public class ImageCarouselBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public List<ImageBlock> Images { get; } = [];
	public string? Id { get; set; }
	public string? MaxHeight { get; set; }

	public override string Directive => "carousel";

	public override void FinalizeAndValidate(ParserContext context)
	{
		// Call the DirectiveBlock's FinalizeAndValidate
		// for setup common to all the directive blocks
		base.FinalizeAndValidate(context);

		// Parse options
		Id = Prop("id");
		MaxHeight = Prop("max-height");

		// Validate max-height option
		if (!string.IsNullOrEmpty(MaxHeight))
		{
			var validHeights = new[] { "none", "small", "medium" };
			if (!validHeights.Contains(MaxHeight.ToLower()))
				this.EmitWarning($"Invalid max-height value '{MaxHeight}'. Valid options are: none, small, medium. Defaulting to 'none'.");
		}

		// Process child image blocks directly
		foreach (var block in this)
		{
			if (block is ImageBlock imageBlock)
			{
				Images.Add(imageBlock);
			}
		}

		if (Images.Count == 0)
		{
			this.EmitError("carousel directive requires nested image directives");
		}
	}
}
