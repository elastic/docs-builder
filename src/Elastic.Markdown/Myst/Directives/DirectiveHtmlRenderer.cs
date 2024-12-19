// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Myst.FrontMatter;
using Elastic.Markdown.Myst.Settings;
using Elastic.Markdown.Myst.Substitution;
using Elastic.Markdown.Slices.Directives;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using RazorSlices;
using YamlDotNet.Core;

namespace Elastic.Markdown.Myst.Directives;

/// <summary>
/// An HTML renderer for a <see cref="DirectiveBlock"/>.
/// </summary>
/// <seealso cref="HtmlObjectRenderer{CustomContainer}" />
public class DirectiveHtmlRenderer : HtmlObjectRenderer<DirectiveBlock>
{
	protected override void Write(HtmlRenderer renderer, DirectiveBlock directiveBlock)
	{
		renderer.EnsureLine();

		switch (directiveBlock)
		{
			case MermaidBlock mermaidBlock:
				WriteMermaid(renderer, mermaidBlock);
				return;
			case AppliesBlock appliesBlock:
				WriteApplies(renderer, appliesBlock);
				return;
			case FigureBlock imageBlock:
				WriteFigure(renderer, imageBlock);
				return;
			case ImageBlock imageBlock:
				WriteImage(renderer, imageBlock);
				return;
			case DropdownBlock dropdownBlock:
				WriteDropdown(renderer, dropdownBlock);
				return;
			case AdmonitionBlock admonitionBlock:
				WriteAdmonition(renderer, admonitionBlock);
				return;
			case VersionBlock versionBlock:
				WriteVersion(renderer, versionBlock);
				return;
			case TabSetBlock tabSet:
				WriteTabSet(renderer, tabSet);
				return;
			case TabItemBlock tabItem:
				WriteTabItem(renderer, tabItem);
				return;
			case LiteralIncludeBlock literalIncludeBlock:
				WriteLiteralIncludeBlock(renderer, literalIncludeBlock);
				return;
			case IncludeBlock includeBlock:
				if (includeBlock.Literal)
					WriteLiteralIncludeBlock(renderer, includeBlock);
				else
					WriteIncludeBlock(renderer, includeBlock);
				return;
			case SettingsBlock settingsBlock:
				WriteSettingsBlock(renderer, settingsBlock);
				return;
			default:
				// if (!string.IsNullOrEmpty(directiveBlock.Info) && !directiveBlock.Info.StartsWith('{'))
				// 	WriteCode(renderer, directiveBlock);
				// else if (!string.IsNullOrEmpty(directiveBlock.Info))
				// 	WriteAdmonition(renderer, directiveBlock);
				// else
				WriteChildren(renderer, directiveBlock);
				break;
		}
	}

	private void WriteImage(HtmlRenderer renderer, ImageBlock block)
	{
		var imageUrl =
			block.ImageUrl != null &&
			(block.ImageUrl.StartsWith("/_static") || block.ImageUrl.StartsWith("_static"))
				? $"{block.Build.UrlPathPrefix}/{block.ImageUrl.TrimStart('/')}"
				: block.ImageUrl;
		var slice = Image.Create(new ImageViewModel
		{
			Label = block.Label,
			Align = block.Align,
			Alt = block.Alt,
			Height = block.Height,
			Scale = block.Scale,
			Target = block.Target,
			Width = block.Width,
			ImageUrl = imageUrl,
		});
		RenderRazorSlice(slice, renderer, block);
	}

	private void WriteFigure(HtmlRenderer renderer, ImageBlock block)
	{
		var imageUrl = block.ImageUrl != null &&
		               (block.ImageUrl.StartsWith("/_static") || block.ImageUrl.StartsWith("_static"))
			? $"{block.Build.UrlPathPrefix}/{block.ImageUrl.TrimStart('/')}"
			: block.ImageUrl;
		var slice = Slices.Directives.Figure.Create(new ImageViewModel
		{
			Label = block.Label,
			Align = block.Align,
			Alt = block.Alt,
			Height = block.Height,
			Scale = block.Scale,
			Target = block.Target,
			Width = block.Width,
			ImageUrl = imageUrl,
		});
		RenderRazorSlice(slice, renderer, block);
	}

	private void WriteChildren(HtmlRenderer renderer, DirectiveBlock directiveBlock) =>
		renderer.WriteChildren(directiveBlock);

	private void WriteVersion(HtmlRenderer renderer, VersionBlock block)
	{
		var slice = Slices.Directives.Version.Create(new VersionViewModel
		{
			Directive = block.Directive, Title = block.Title, VersionClass = block.Class
		});
		RenderRazorSlice(slice, renderer, block);
	}

	private void WriteAdmonition(HtmlRenderer renderer, AdmonitionBlock block)
	{
		var slice = Admonition.Create(new AdmonitionViewModel
		{
			Directive = block.Admonition,
			CrossReferenceName = block.CrossReferenceName,
			Classes = block.Classes,
			Title = block.Title,
			Open = block.DropdownOpen.GetValueOrDefault() ? "open" : null
		});
		RenderRazorSlice(slice, renderer, block);
	}

	private void WriteDropdown(HtmlRenderer renderer, DropdownBlock block)
	{
		var slice = Dropdown.Create(new AdmonitionViewModel
		{
			Directive = block.Admonition,
			CrossReferenceName = block.CrossReferenceName,
			Classes = block.Classes,
			Title = block.Title,
			Open = block.DropdownOpen.GetValueOrDefault() ? "open" : null
		});
		RenderRazorSlice(slice, renderer, block);
	}

	private void WriteCode(HtmlRenderer renderer, EnhancedCodeBlock block)
	{
		var slice = Code.Create(new CodeViewModel
		{
			CrossReferenceName = string.Empty,// block.CrossReferenceName,
			Language = block.Language,
			Caption = string.Empty
		});
		//RenderRazorSliceRawContent(slice, renderer, block);
	}


	private void WriteTabSet(HtmlRenderer renderer, TabSetBlock block)
	{
		var slice = TabSet.Create(new TabSetViewModel());
		RenderRazorSlice(slice, renderer, block);
	}

	private void WriteMermaid(HtmlRenderer renderer, MermaidBlock block)
	{
		var slice = Mermaid.Create(new MermaidViewModel());
		RenderRazorSliceRawContent(slice, renderer, block);
	}

	private void WriteApplies(HtmlRenderer renderer, AppliesBlock block)
	{
		if (block.Deployment is null || block.Deployment == Deployment.All)
			return;

		var slice = Applies.Create(block.Deployment);
		RenderRazorSliceNoContent(slice, renderer);
	}

	private void WriteTabItem(HtmlRenderer renderer, TabItemBlock block)
	{
		var slice = TabItem.Create(new TabItemViewModel
		{
			Index = block.Index, Title = block.Title, TabSetIndex = block.TabSetIndex
		});
		RenderRazorSlice(slice, renderer, block);
	}

	private void WriteLiteralIncludeBlock(HtmlRenderer renderer, IncludeBlock block)
	{
		if (!block.Found || block.IncludePath is null)
			return;

		var file = block.FileSystem.FileInfo.New(block.IncludePath);
		var content = block.FileSystem.File.ReadAllText(file.FullName);
		if (string.IsNullOrEmpty(block.Language))
			renderer.Write(content);
		else
		{
			var slice = Code.Create(new CodeViewModel
			{
				CrossReferenceName = null, Language = block.Language, Caption = null
			});
			RenderRazorSlice(slice, renderer, content);
		}
	}

	private void WriteIncludeBlock(HtmlRenderer renderer, IncludeBlock block)
	{
		if (!block.Found || block.IncludePath is null)
			return;

		var parser = new MarkdownParser(block.DocumentationSourcePath, block.Build, block.GetDocumentationFile,
			block.Configuration);
		var file = block.FileSystem.FileInfo.New(block.IncludePath);
		var document = parser.ParseAsync(file, block.FrontMatter, default).GetAwaiter().GetResult();
		var html = document.ToHtml(parser.Pipeline);
		renderer.Write(html);
		//var slice = Include.Create(new IncludeViewModel { Html = html });
		//RenderRazorSlice(slice, renderer, block);
	}

	private void WriteSettingsBlock(HtmlRenderer renderer, SettingsBlock block)
	{
		if (!block.Found || block.IncludePath is null)
			return;

		var parser = new MarkdownParser(block.DocumentationSourcePath, block.Build, block.GetDocumentationFile, block.Configuration);

		var file = block.FileSystem.FileInfo.New(block.IncludePath);

		SettingsCollection? settings;
		try
		{
			var yaml = file.FileSystem.File.ReadAllText(file.FullName);
			settings = YamlSerialization.Deserialize<SettingsCollection>(yaml);
		}
		catch (YamlException e)
		{
			block.EmitError("Can not be parsed as a valid settings file", e.InnerException ?? e);
			return;
		}
		catch (Exception e)
		{
			block.EmitError("Can not be parsed as a valid settings file", e);
			return;
		}

		var slice = Slices.Directives.Settings.Create(new SettingsViewModel
		{
			SettingsCollection = settings,
			RenderMarkdown = s =>
			{
				var document = parser.Parse(s, block.IncludeFrom, block.FrontMatter);
				var html = document.ToHtml(parser.Pipeline);
				return html;
			}
		});
		RenderRazorSliceNoContent(slice, renderer);
	}

	private static void RenderRazorSlice<T>(RazorSlice<T> slice, HtmlRenderer renderer, string contents)
	{
		var html = slice.RenderAsync().GetAwaiter().GetResult();
		var blocks = html.Split("[CONTENT]", 2, StringSplitOptions.RemoveEmptyEntries);
		renderer.Write(blocks[0]);
		renderer.Write(contents);
		renderer.Write(blocks[1]);
	}

	private static void RenderRazorSlice<T>(RazorSlice<T> slice, HtmlRenderer renderer, DirectiveBlock obj)
	{
		var html = slice.RenderAsync().GetAwaiter().GetResult();
		var blocks = html.Split("[CONTENT]", 2, StringSplitOptions.RemoveEmptyEntries);
		renderer.Write(blocks[0]);
		renderer.WriteChildren(obj);
		renderer.Write(blocks[1]);
	}

	private static void RenderRazorSliceNoContent<T>(RazorSlice<T> slice, HtmlRenderer renderer)
	{
		var html = slice.RenderAsync().GetAwaiter().GetResult();
		renderer.Write(html);
	}

	private static void RenderRazorSliceRawContent<T>(RazorSlice<T> slice, HtmlRenderer renderer, DirectiveBlock obj)
	{
		var html = slice.RenderAsync().GetAwaiter().GetResult();
		var blocks = html.Split("[CONTENT]", 2, StringSplitOptions.RemoveEmptyEntries);
		renderer.Write(blocks[0]);
		foreach (var o in obj)
			Render(o);

		renderer.Write(blocks[1]);

		void RenderLeaf(LeafBlock p)
		{
			renderer.WriteLeafRawLines(p, true, false, false);
			renderer.EnableHtmlForInline = false;
			foreach (var oo in p.Inline ?? [])
			{
				if (oo is SubstitutionLeaf sl)
					renderer.Write(sl.Replacement);
				else if (oo is LiteralInline li)
					renderer.Write(li);
				else if (oo is LineBreakInline)
					renderer.WriteLine();
				else if (oo is Role r)
				{
					renderer.Write(new string(r.DelimiterChar, r.DelimiterCount));
					renderer.WriteChildren(r);
				}

				else
					renderer.Write($"(LeafBlock: {oo.GetType().Name}");
			}

			renderer.EnableHtmlForInline = true;
		}

		void RenderListBlock(ListBlock l)
		{
			foreach (var bb in l)
			{
				if (bb is LeafBlock lbi)
					RenderLeaf(lbi);
				else if (bb is ListItemBlock ll)
				{
					renderer.Write(ll.TriviaBefore);
					renderer.Write("-");
					foreach (var lll in ll)
						Render(lll);
				}
				else
					renderer.Write($"(ListBlock: {l.GetType().Name}");
			}
		}

		void Render(Block o)
		{
			if (o is LeafBlock p)
				RenderLeaf(p);
			else if (o is ListBlock l)
				RenderListBlock(l);
			else
				renderer.Write($"(Block: {o.GetType().Name}");
		}
	}
}
