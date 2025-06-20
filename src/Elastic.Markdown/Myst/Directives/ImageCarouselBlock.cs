// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst;

namespace Elastic.Markdown.Myst.Directives;

public class ImageCarouselBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public List<ImageBlock> Images { get; } = [];
	public string? Id { get; set; }
	public string? FixedHeight { get; set; }

	public override string Directive => "carousel";

	public override void FinalizeAndValidate(ParserContext context)
	{
		// Parse options
		Id = Prop("id");
		FixedHeight = Prop("fixed-height");

		// Validate fixed-height option
		if (!string.IsNullOrEmpty(FixedHeight))
		{
			var validHeights = new[] { "auto", "small", "medium" };
			if (!validHeights.Contains(FixedHeight.ToLower()))
			{
				this.EmitWarning($"Invalid fixed-height value '{FixedHeight}'. Valid options are: auto, small, medium. Defaulting to 'auto'.");
			}
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
