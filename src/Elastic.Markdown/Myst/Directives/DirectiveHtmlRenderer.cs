// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst.InlineParsers.Substitution;
using Elastic.Markdown.Myst.Settings;
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
			case StepperBlock stepperBlock:
				WriteStepperBlock(renderer, stepperBlock);
				return;
			case StepBlock stepBlock:
				WriteStepBlock(renderer, stepBlock);
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

		var slice = Image.Create(new ImageViewModel
		{
			DirectiveBlock = block,
			Label = block.Label,
			Align = block.Align,
			Alt = block.Alt ?? string.Empty,
			Title = block.Title,
			Height = block.Height,
			Scale = block.Scale,
			Target = block.Target,
			Width = block.Width,
			Screenshot = block.Screenshot,
			ImageUrl = imageUrl,
		});
		RenderRazorSlice(slice, renderer);
	}

	private static void WriteStepperBlock(HtmlRenderer renderer, StepperBlock block)
	{
		var slice = Stepper.Create(new StepperViewModel { DirectiveBlock = block });
		RenderRazorSlice(slice, renderer);
	}

	private static void WriteStepBlock(HtmlRenderer renderer, StepBlock block)
	{
		var slice = Step.Create(new StepViewModel
		{
			DirectiveBlock = block,
			Title = block.Title,
			Anchor = block.Anchor
		});
		RenderRazorSlice(slice, renderer);
	}

	private static void WriteFigure(HtmlRenderer renderer, ImageBlock block)
	{
		var imageUrl = block.ImageUrl != null &&
					   (block.ImageUrl.StartsWith("/_static") || block.ImageUrl.StartsWith("_static"))
			? $"{block.Build.UrlPathPrefix}/{block.ImageUrl.TrimStart('/')}"
			: block.ImageUrl;
		var slice = Figure.Create(new ImageViewModel
		{
			DirectiveBlock = block,
			Label = block.Label,
			Align = block.Align,
			Alt = block.Alt ?? string.Empty,
			Title = block.Title,
			Height = block.Height,
			Scale = block.Scale,
			Target = block.Target,
			Width = block.Width,
			Screenshot = block.Screenshot,
			ImageUrl = imageUrl,
		});
		RenderRazorSlice(slice, renderer);
	}

	private static void WriteChildren(HtmlRenderer renderer, DirectiveBlock directiveBlock) =>
		renderer.WriteChildren(directiveBlock);

	private static void WriteVersion(HtmlRenderer renderer, VersionBlock block)
	{
		var slice = Slices.Directives.Version.Create(new VersionViewModel
		{
			DirectiveBlock = block,
			Directive = block.Directive,
			Title = block.Title,
			VersionClass = block.Class
		});
		RenderRazorSlice(slice, renderer);
	}

	private static void WriteAdmonition(HtmlRenderer renderer, AdmonitionBlock block)
	{
		var slice = Admonition.Create(new AdmonitionViewModel
		{
			DirectiveBlock = block,
			Directive = block.Admonition,
			CrossReferenceName = block.CrossReferenceName,
			Classes = block.Classes,
			Title = block.Title,
			Open = block.DropdownOpen.GetValueOrDefault() ? "open" : null
		});
		RenderRazorSlice(slice, renderer);
	}

	private static void WriteDropdown(HtmlRenderer renderer, DropdownBlock block)
	{
		var slice = Dropdown.Create(new AdmonitionViewModel
		{
			DirectiveBlock = block,
			Directive = block.Admonition,
			CrossReferenceName = block.CrossReferenceName,
			Classes = block.Classes,
			Title = block.Title,
			Open = block.DropdownOpen.GetValueOrDefault() ? "open" : null
		});
		RenderRazorSlice(slice, renderer);
	}

	private static void WriteTabSet(HtmlRenderer renderer, TabSetBlock block)
	{
		var slice = TabSet.Create(new TabSetViewModel { DirectiveBlock = block });
		RenderRazorSlice(slice, renderer);
	}

	private static void WriteTabItem(HtmlRenderer renderer, TabItemBlock block)
	{
		var slice = TabItem.Create(new TabItemViewModel
		{
			DirectiveBlock = block,
			Index = block.Index,
			Title = block.Title,
			TabSetIndex = block.TabSetIndex,
			SyncKey = block.SyncKey,
			TabSetGroupKey = block.TabSetGroupKey
		});
		RenderRazorSlice(slice, renderer);
	}

	private static void WriteMermaid(HtmlRenderer renderer, MermaidBlock block)
	{
		var slice = Mermaid.Create(new MermaidViewModel { DirectiveBlock = block });
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
				ApiCallHeader = null,
				RawIncludedFileContents = content
			});
			RenderRazorSlice(slice, renderer);
		}
	}

	private static void WriteIncludeBlock(HtmlRenderer renderer, IncludeBlock block)
	{
		if (!block.Found || block.IncludePath is null)
			return;

		var snippet = block.Build.ReadFileSystem.FileInfo.New(block.IncludePath);

		var parentPath = block.Context.MarkdownParentPath ?? block.Context.MarkdownSourcePath;
		var document = MarkdownParser.ParseSnippetAsync(block.Build, block.Context, snippet, parentPath, block.Context.YamlFrontMatter, default)
			.GetAwaiter().GetResult();

		var html = document.ToHtml(MarkdownParser.Pipeline);
		_ = renderer.Write(html);
	}

	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	private static void WriteSettingsBlock(HtmlRenderer renderer, SettingsBlock block)
	{
		if (!block.Found || block.IncludePath is null)
			return;

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
				var document = MarkdownParser.ParseMarkdownStringAsync(block.Build, block.Context, s, block.IncludeFrom, block.Context.YamlFrontMatter, MarkdownParser.Pipeline);
				var html = document.ToHtml(MarkdownParser.Pipeline);
				return html;
			}
		});
		var html = slice.RenderAsync().GetAwaiter().GetResult();
		_ = renderer.Write(html);
	}

	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	private static void RenderRazorSlice<T>(RazorSlice<T> slice, HtmlRenderer renderer) => slice.RenderAsync(renderer.Writer).GetAwaiter().GetResult();

	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	private static void RenderRazorSliceRawContent<T>(RazorSlice<T> slice, HtmlRenderer renderer, DirectiveBlock obj)
		where T : DirectiveViewModel
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
