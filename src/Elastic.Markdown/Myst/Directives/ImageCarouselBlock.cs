// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Markdown.Myst.Directives;

public class ImageCarouselBlock : DirectiveBlock
{
	private readonly DirectiveBlockParser _parser;

	// Inner class for YAML deserialization
	private sealed class CarouselImage
	{
		[YamlMember(Alias = "url")]
		public string? Url { get; set; }

		[YamlMember(Alias = "alt")]
		public string? Alt { get; set; }

		[YamlMember(Alias = "title")]
		public string? Title { get; set; }

		[YamlMember(Alias = "height")]
		public string? Height { get; set; }

		[YamlMember(Alias = "width")]
		public string? Width { get; set; }
	}

	public List<ImageBlock> Images { get; } = [];
	public string? Id { get; set; }
	public bool? ShowControls { get; set; }
	public bool? ShowIndicators { get; set; }

#pragma warning disable IDE0290 // Use primary constructor
	public ImageCarouselBlock(DirectiveBlockParser parser, ParserContext context)
		: base(parser, context) => _parser = parser;
#pragma warning restore IDE0290 // Use primary constructor

	public override string Directive => "carousel";

	public override void FinalizeAndValidate(ParserContext context)
	{
		// Parse options
		Id = Prop("id");
		ShowControls = TryPropBool("controls");
		ShowIndicators = TryPropBool("indicators");

		// Check for child image blocks first (nested directive approach)
		var childImageBlocks = new List<ImageBlock>();

		// Log all children for debugging
		var childCount = 0;
		Console.WriteLine($"DEBUG Carousel {Id}: Examining all child blocks");
		foreach (var block in this)
		{
			childCount++;
			Console.WriteLine($"DEBUG Carousel {Id}: Child {childCount} is of type {block.GetType().Name}");

			if (block is ImageBlock imageBlock)
			{
				childImageBlocks.Add(imageBlock);
				Console.WriteLine($"DEBUG Carousel {Id}: - Added ImageBlock with URL: {imageBlock.Arguments}");
			}
		}

		Console.WriteLine($"DEBUG Carousel {Id}: Total children count: {childCount}");
		Console.WriteLine($"DEBUG Carousel {Id}: Found {childImageBlocks.Count} child image blocks");

		if (childImageBlocks.Count > 0)
		{
			// Process child image blocks
			foreach (var imageBlock in childImageBlocks)
			{
				Console.WriteLine($"DEBUG Carousel {Id}: Processing ImageBlock with URL: {imageBlock.Arguments}");
				// Process the ImageBlock
				imageBlock.FinalizeAndValidate(context);

				// Add to Images list so they're available for rendering
				Images.Add(imageBlock);
			}

			// Remove the blocks from the parent since they've been processed
			// This prevents them from being rendered twice
			foreach (var block in childImageBlocks)
			{
				_ = this.Remove(block);  // Use discard operator to ignore the return value
			}

			return; // Exit early as we've processed nested directives
		}

		// Parse images array as fallback for backward compatibility
		var imagesYaml = Prop("images");
		Console.WriteLine($"DEBUG Carousel {Id}: Found images array: {imagesYaml}");
		if (string.IsNullOrEmpty(imagesYaml))
		{
			this.EmitError("carousel directive requires either nested image directives or an :images: property");
			return;
		}

		try
		{
			// Create a deserializer to process the YAML using the static builder
			var deserializer = new StaticDeserializerBuilder(new DocsBuilderYamlStaticContext())
				.WithNamingConvention(HyphenatedNamingConvention.Instance)
				.Build();

			// Parse YAML images array
			var carouselImages = deserializer.Deserialize<List<CarouselImage>>(imagesYaml);

			// Create ImageBlock instances from the parsed YAML
			foreach (var img in carouselImages)
			{
				if (string.IsNullOrEmpty(img.Url))
				{
					this.EmitError("Each image in a carousel must have a URL");
					continue;
				}

				// Create a new ImageBlock for each entry
				var imageBlock = new ImageBlock(_parser, context)
				{
					Arguments = img.Url
				};

				// Set properties if provided
				if (!string.IsNullOrEmpty(img.Alt))
					imageBlock.AddProperty("alt", img.Alt);

				if (!string.IsNullOrEmpty(img.Title))
					imageBlock.AddProperty("title", img.Title);

				if (!string.IsNullOrEmpty(img.Height))
					imageBlock.AddProperty("height", img.Height);

				if (!string.IsNullOrEmpty(img.Width))
					imageBlock.AddProperty("width", img.Width);

				// Process the ImageBlock
				imageBlock.FinalizeAndValidate(context);

				// Add to our carousel's images list
				Images.Add(imageBlock);
			}
		}
		catch (Exception ex)
		{
			this.EmitError($"Failed to parse images: {ex.Message}");
		}

		// Validate we have at least one image
		if (Images.Count == 0)
		{
			this.EmitError("carousel directive requires at least one image");
		}
	}

	private int? TryPropInt(string name)
	{
		var value = Prop(name);
		if (string.IsNullOrEmpty(value))
			return null;
		return int.TryParse(value, out var result) ? result : null;
	}
}
