// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Myst.Settings;
using Markdig.Syntax;
using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Slices.Directives;

public abstract class DirectiveViewModel
{
	public required ContainerBlock DirectiveBlock { get; set; }
	public HtmlString RenderBlock()
	{
		var subscription = DocumentationObjectPoolProvider.HtmlRendererPool.Get();
		subscription.HtmlRenderer.WriteChildren(DirectiveBlock);

		var result = subscription.RentedStringBuilder?.ToString();
		DocumentationObjectPoolProvider.HtmlRendererPool.Return(subscription);
		return new HtmlString(result);
	}
}

public class AdmonitionViewModel : DirectiveViewModel
{
	public required string Title { get; init; }
	public required string Directive { get; init; }
	public required string? CrossReferenceName { get; init; }
	public required string? Classes { get; init; }
	public required string? Open { get; init; }
}

public class CodeViewModel
{
	public required string? ApiCallHeader { get; init; }
	public required string? Caption { get; init; }
	public required string Language { get; init; }
	public required string? CrossReferenceName { get; init; }
	public string? RawIncludedFileContents { get; init; }
	public EnhancedCodeBlock? EnhancedCodeBlock { get; set; }

	public HtmlString RenderBlock()
	{
		if (!string.IsNullOrWhiteSpace(RawIncludedFileContents))
			return new HtmlString(RawIncludedFileContents);
		if (EnhancedCodeBlock == null)
			return HtmlString.Empty;

		var subscription = DocumentationObjectPoolProvider.HtmlRendererPool.Get();
		EnhancedCodeBlockHtmlRenderer.RenderCodeBlockLines(subscription.HtmlRenderer, EnhancedCodeBlock);
		var result = subscription.RentedStringBuilder?.ToString();
		DocumentationObjectPoolProvider.HtmlRendererPool.Return(subscription);
		return new HtmlString(result);
	}
}

public class VersionViewModel : DirectiveViewModel
{
	public required string Directive { get; init; }
	public required string VersionClass { get; init; }
	public required string Title { get; init; }
}

public class TabSetViewModel : DirectiveViewModel;

public class TabItemViewModel : DirectiveViewModel
{
	public required int Index { get; init; }
	public required int TabSetIndex { get; init; }
	public required string Title { get; init; }
	public required string? SyncKey { get; init; }
	public required string? TabSetGroupKey { get; init; }
}
public class IncludeViewModel : DirectiveViewModel
{
	public required string Html { get; init; }
}

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


public class SettingsViewModel
{
	public required YamlSettings SettingsCollection { get; init; }

	public required Func<string, string> RenderMarkdown { get; init; }
}

public class MermaidViewModel : DirectiveViewModel;

public class StepperViewModel : DirectiveViewModel;

public class StepViewModel : DirectiveViewModel
{
	public required string Title { get; init; }
	public required string Anchor { get; init; }
}
