// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.Markdown.Diagnostics;
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
		_ = renderer.EnsureLine();

		switch (directiveBlock)
		{
			case MermaidBlock mermaidBlock:
				WriteMermaid(renderer, mermaidBlock);
				return;
			case AppliesBlock: //deprecated scheduled for removal
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

	private static void WriteImage(HtmlRenderer renderer, ImageBlock block)
	{
		var imageUrl = block.ImageUrl;
		if (!string.IsNullOrEmpty(block.ImageUrl))
		{
			if (block.ImageUrl.StartsWith('/') || block.ImageUrl.StartsWith("_static"))
				imageUrl = $"{block.Build.UrlPathPrefix}/{block.ImageUrl.TrimStart('/')}";
			else
			{
				// `block.Build.ConfigurationPath.DirectoryName` is the directory of the docset.yml file
				// which is the root of the documentation source
				// e.g. `/User/username/Projects/docs-builder/docs`
				// `block.CurrentFile.DirectoryName` is the directory of the current markdown file where the image is referenced
				// e.g. `/User/username/Projects/docs-builder/docs/page/with/image`
				// `Path.GetRelativePath` will return the relative path to the docset.yml directory
				// e.g. `page/with/image`
				// Hence the full imageUrl will be something like /path-prefix/page/with/image/image.png
				imageUrl = block.Build.UrlPathPrefix + '/' + Path.GetRelativePath(block.Build.ConfigurationPath.DirectoryName!, block.CurrentFile.DirectoryName!) + "/" + block.ImageUrl;
			}
		}
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

	private static void WriteFigure(HtmlRenderer renderer, ImageBlock block)
	{
		var imageUrl = block.ImageUrl != null &&
					   (block.ImageUrl.StartsWith("/_static") || block.ImageUrl.StartsWith("_static"))
			? $"{block.Build.UrlPathPrefix}/{block.ImageUrl.TrimStart('/')}"
			: block.ImageUrl;
		var slice = Figure.Create(new ImageViewModel
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

	private static void WriteChildren(HtmlRenderer renderer, DirectiveBlock directiveBlock) =>
		renderer.WriteChildren(directiveBlock);

	private static void WriteVersion(HtmlRenderer renderer, VersionBlock block)
	{
		var slice = Slices.Directives.Version.Create(new VersionViewModel
		{
			Directive = block.Directive,
			Title = block.Title,
			VersionClass = block.Class
		});
		RenderRazorSlice(slice, renderer, block);
	}

	private static void WriteAdmonition(HtmlRenderer renderer, AdmonitionBlock block)
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

	private static void WriteDropdown(HtmlRenderer renderer, DropdownBlock block)
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

	private static void WriteTabSet(HtmlRenderer renderer, TabSetBlock block)
	{
		var slice = TabSet.Create(new TabSetViewModel());
		RenderRazorSlice(slice, renderer, block);
	}

	private static void WriteTabItem(HtmlRenderer renderer, TabItemBlock block)
	{
		var slice = TabItem.Create(new TabItemViewModel
		{
			Index = block.Index,
			Title = block.Title,
			TabSetIndex = block.TabSetIndex,
			SyncKey = block.SyncKey,
			TabSetGroupKey = block.TabSetGroupKey
		});
		RenderRazorSlice(slice, renderer, block);
	}

	private static void WriteMermaid(HtmlRenderer renderer, MermaidBlock block)
	{
		var slice = Mermaid.Create(new MermaidViewModel());
		RenderRazorSliceRawContent(slice, renderer, block);
	}

	private static void WriteLiteralIncludeBlock(HtmlRenderer renderer, IncludeBlock block)
	{
		if (!block.Found || block.IncludePath is null)
			return;

		var file = block.Build.ReadFileSystem.FileInfo.New(block.IncludePath);
		var content = block.Build.ReadFileSystem.File.ReadAllText(file.FullName);
		if (string.IsNullOrEmpty(block.Language))
			_ = renderer.Write(content);
		else
		{
			var slice = Code.Create(new CodeViewModel
			{
				CrossReferenceName = null,
				Language = block.Language,
				Caption = null,
				ApiCallHeader = null
			});
			RenderRazorSlice(slice, renderer, content);
		}
	}

	private static void WriteIncludeBlock(HtmlRenderer renderer, IncludeBlock block)
	{
		if (!block.Found || block.IncludePath is null)
			return;

		var parser = new MarkdownParser(
			block.Build.DocumentationSourceDirectory,
			block.Build,
			block.Context.GetDocumentationFile,
			block.Build.Configuration,
			block.Context.LinksResolver
		);
		var snippet = block.Build.ReadFileSystem.FileInfo.New(block.IncludePath);
		var parentPath = block.Context.CurrentPath;
		var document = parser.ParseSnippetAsync(snippet, parentPath, block.Context.FrontMatter, default).GetAwaiter().GetResult();
		var html = document.ToHtml(MarkdownParser.Pipeline);
		_ = renderer.Write(html);
	}

	private static void WriteSettingsBlock(HtmlRenderer renderer, SettingsBlock block)
	{
		if (!block.Found || block.IncludePath is null)
			return;

		var parser = new MarkdownParser(
			block.Build.DocumentationSourceDirectory, block.Build, block.Context.GetDocumentationFile, block.Build.Configuration
			, block.Context.LinksResolver
		);

		var file = block.Build.ReadFileSystem.FileInfo.New(block.IncludePath);

		YamlSettings? settings;
		try
		{
			var yaml = file.FileSystem.File.ReadAllText(file.FullName);
			settings = YamlSerialization.Deserialize<YamlSettings>(yaml);
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
				var document = parser.ParseEmbeddedMarkdown(s, block.IncludeFrom, block.Context.FrontMatter);
				var html = document.ToHtml(MarkdownParser.Pipeline);
				return html;
			}
		});
		RenderRazorSliceNoContent(slice, renderer);
	}

	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	private static void RenderRazorSlice<T>(RazorSlice<T> slice, HtmlRenderer renderer, string contents)
	{
		var html = slice.RenderAsync().GetAwaiter().GetResult();
		var blocks = html.Split("[CONTENT]", 2, StringSplitOptions.RemoveEmptyEntries);
		_ = renderer
			.Write(blocks[0])
			.Write(contents)
			.Write(blocks[1]);
	}

	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	private static void RenderRazorSlice<T>(RazorSlice<T> slice, HtmlRenderer renderer, DirectiveBlock obj)
	{
		var html = slice.RenderAsync().GetAwaiter().GetResult();
		var blocks = html.Split("[CONTENT]", 2, StringSplitOptions.RemoveEmptyEntries);
		_ = renderer.Write(blocks[0]);
		renderer.WriteChildren(obj);
		_ = renderer.Write(blocks[1]);
	}

	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	private static void RenderRazorSliceNoContent<T>(RazorSlice<T> slice, HtmlRenderer renderer)
	{
		var html = slice.RenderAsync().GetAwaiter().GetResult();
		_ = renderer.Write(html);
	}

	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	private static void RenderRazorSliceRawContent<T>(RazorSlice<T> slice, HtmlRenderer renderer, DirectiveBlock obj)
	{
		var html = slice.RenderAsync().GetAwaiter().GetResult();
		var blocks = html.Split("[CONTENT]", 2, StringSplitOptions.RemoveEmptyEntries);
		_ = renderer.Write(blocks[0]);
		foreach (var o in obj)
			Render(o);

		_ = renderer.Write(blocks[1]);

		void RenderLeaf(LeafBlock p)
		{
			_ = renderer.WriteLeafRawLines(p, true, false, false);
			renderer.EnableHtmlForInline = false;
			foreach (var oo in p.Inline ?? [])
			{
				if (oo is SubstitutionLeaf sl)
					_ = renderer.Write(sl.Replacement);
				else if (oo is LiteralInline li)
					renderer.Write(li);
				else if (oo is LineBreakInline)
					_ = renderer.WriteLine();
				else if (oo is Role r)
				{
					_ = renderer.Write(new string(r.DelimiterChar, r.DelimiterCount));
					renderer.WriteChildren(r);
				}

				else
					_ = renderer.Write($"(LeafBlock: {oo.GetType().Name}");
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
					_ = renderer.Write(ll.TriviaBefore);
					_ = renderer.Write("-");
					foreach (var lll in ll)
						Render(lll);
				}
				else
					_ = renderer.Write($"(ListBlock: {l.GetType().Name}");
			}
		}

		void Render(Block o)
		{
			if (o is LeafBlock p)
				RenderLeaf(p);
			else if (o is ListBlock l)
				RenderListBlock(l);
			else
				_ = renderer.Write($"(Block: {o.GetType().Name}");
		}
	}
}
