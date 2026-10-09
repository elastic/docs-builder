// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Myst.Components;
using Elastic.Markdown.Myst.Directives.Admonition;
using Elastic.Markdown.Myst.Directives.Button;
using Elastic.Markdown.Myst.Directives.Stepper;
using Elastic.Markdown.Myst.Directives.Tabs;
using Markdig.Renderers;
using Microsoft.AspNetCore.Html;
using RazorSlices;
using Ds = Elastic.Documentation.Site.Components;

namespace Elastic.Markdown.Myst.Directives;

/// <summary>
/// Renders directives with design-system components. Only used when <c>NAVIGATION_PREVIEW</c> is on; with the flag off the
/// legacy views render exactly as before. Each directive keeps its parsing and nested content, and only the markup changes.
/// </summary>
internal static class DesignSystemDirectives
{
	public static bool IsEnabled(DirectiveBlock block) => IsEnabled(block.Build);

	public static bool IsEnabled(BuildContext build) => build.Configuration.Features.NavigationPreviewEnabled;

	/// <returns><c>true</c> when the block was written; <c>false</c> leaves it to the legacy views.</returns>
	public static bool TryWrite(HtmlRenderer renderer, DirectiveBlock block)
	{
		switch (block)
		{
			case DropdownBlock dropdown:
				Write(
					renderer,
					Ds
						.Disclosure
						._Disclosure
						.Create(new Ds.Disclosure.DisclosureModel
						{
							Title = dropdown.Title,
							Body = new BodyViewModel { DirectiveBlock = dropdown }.RenderBlock(),
							Id = dropdown.CrossReferenceName,
							Classes = dropdown.Classes,
							Open = dropdown.DropdownOpen.GetValueOrDefault(),
							AppliesTo = AppliesToBadge(dropdown, showTooltip: false)
						})
				);
				return true;
			case AdmonitionBlock admonition:
				Write(
					renderer,
					Ds
						.Admonition
						._Admonition
						.Create(new Ds.Admonition.AdmonitionModel
						{
							Kind = Ds.Admonition.AdmonitionModel.KindFor(admonition.Admonition),
							Title = admonition.Title,
							Body = new BodyViewModel { DirectiveBlock = admonition }.RenderBlock(),
							Id = admonition.CrossReferenceName,
							Classes = admonition.Classes,
							AppliesTo = AppliesToBadge(admonition, showTooltip: true)
						})
				);
				return true;
			case TabSetBlock tabSet:
				Write(
					renderer,
					Ds
						.Tabs
						._TabSet
						.Create(new Ds.Tabs.TabSetModel { Items = new TabSetViewModel { DirectiveBlock = tabSet }.RenderBlock() })
				);
				return true;
			case TabItemBlock tabItem:
				Write(
					renderer,
					Ds
						.Tabs
						._TabItem
						.Create(new Ds.Tabs.TabItemModel
						{
							Title = tabItem.Title,
							SetIndex = tabItem.TabSetIndex,
							Index = tabItem.Index,
							SyncKey = tabItem.SyncKey,
							SyncGroup = tabItem.TabSetGroupKey,
							Body = new TabItemViewModel
							{
								DirectiveBlock = tabItem,
								Index = tabItem.Index,
								TabSetIndex = tabItem.TabSetIndex,
								Title = tabItem.Title,
								SyncKey = tabItem.SyncKey,
								TabSetGroupKey = tabItem.TabSetGroupKey
							}.RenderBlock()
						})
				);
				return true;
			case StepperBlock stepper:
				Write(
					renderer,
					Ds
						.Stepper
						._Stepper
						.Create(new Ds.Stepper.StepperModel { Steps = new StepperViewModel { DirectiveBlock = stepper }.RenderBlock() })
				);
				return true;
			case StepBlock step:
				WriteStep(renderer, step);
				return true;
			case ButtonGroupBlock group:
				Write(
					renderer,
					Ds
						.ButtonGroup
						._ButtonGroup
						.Create(new Ds.ButtonGroup.ButtonGroupModel
						{
							Align = group.Align,
							Buttons = new ButtonGroupViewModel { DirectiveBlock = group, Align = group.Align }.RenderBlock()
						})
				);
				return true;
			case ButtonBlock button:
				Write(
					renderer,
					Ds
						.ButtonGroup
						._ButtonDirective
						.Create(new Ds.ButtonGroup.ButtonDirectiveModel
						{
							Style = button.Type switch
							{
								"secondary" => Ds.ButtonGroup.ButtonDirectiveStyle.Secondary,
								"neutral" => Ds.ButtonGroup.ButtonDirectiveStyle.Neutral,
								_ => Ds.ButtonGroup.ButtonDirectiveStyle.Primary
							},
							Align = button.Align,
							InGroup = button.IsInGroup,
							Link = new ButtonViewModel
							{
								DirectiveBlock = button,
								Type = button.Type,
								Align = button.Align,
								IsInGroup = button.IsInGroup
							}.RenderBlock()
						})
				);
				return true;
			default:
				return false;
		}
	}

	/// <summary>Writes a fenced code block as a design-system code block. Callout lists after the block stay with the shared renderer.</summary>
	public static void WriteCodeBlock(HtmlRenderer renderer, EnhancedCodeBlock block)
	{
		var legacy = new CodeViewModel
		{
			CrossReferenceName = string.Empty,
			Language = block.Language,
			Caption = block.Caption,
			ApiSegments = block.ApiSegments,
			EnhancedCodeBlock = block
		};
		Write(
			renderer,
			Ds
				.CodeBlock
				._CodeBlock
				.Create(new Ds.CodeBlock.CodeBlockModel { Language = block.Language, Caption = block.Caption, Body = CodeBody(legacy) })
		);
	}

	private static HtmlString CodeBody(CodeViewModel code)
	{
		if (code.ApiSegments.Count == 0)
			return code.RenderBlock();

		var html = new System.Text.StringBuilder();
		foreach (var segment in code.ApiSegments)
		{
			_ = html.Append($"<code class=\"language-apiheader\">{code.RenderLineWithCallouts(segment.Header, segment.LineNumber)}</code>");
			if (segment.ContentLinesWithNumbers.Count > 0)
				_ = html.Append(
					$"<code class=\"language-json\">{code.RenderContentLinesWithCallouts(segment.ContentLinesWithNumbers)}</code>"
				);
		}
		return new HtmlString(html.ToString());
	}

	private static void WriteStep(HtmlRenderer renderer, StepBlock step)
	{
		var legacy = new StepViewModel
		{
			DirectiveBlock = step,
			Title = step.Title,
			Anchor = step.Anchor,
			HeadingLevel = step.HeadingLevel,
			RenderAsHeading = step.RenderAsHeading
		};
		Write(
			renderer,
			Ds
				.Stepper
				._Step
				.Create(new Ds.Stepper.StepModel
				{
					Title = step.Title,
					TitleHtml = string.IsNullOrEmpty(step.Title) ? null : legacy.RenderTitle(),
					Anchor = step.Anchor,
					HeadingLevel = step.HeadingLevel,
					AsHeading = step.RenderAsHeading,
					Body = legacy.RenderBlock()
				})
		);
	}

	/// <summary>The applies-to badge is still the legacy component until the popover moves to the design system.</summary>
	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	private static IHtmlContent? AppliesToBadge(AdmonitionBlock block, bool showTooltip)
	{
		if (block.AppliesTo is null)
			return null;

		var html = ApplicableToComponent
			.Create(new ApplicableToViewModel
			{
				AppliesTo = block.AppliesTo,
				Inline = true,
				ShowTooltip = showTooltip,
				DesignSystem = true,
				VersionsConfig = block.Build.VersionsConfiguration
			})
			.RenderAsync()
			.GetAwaiter()
			.GetResult();
		return new HtmlString(html);
	}

	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	private static void Write<T>(HtmlRenderer renderer, RazorSlice<T> slice)
	{
		var html = slice.RenderAsync().GetAwaiter().GetResult();
		_ = renderer.Write(html.EnsureTrimmed());
	}

	/// <summary>Renders a block's children when no legacy view model applies.</summary>
	private sealed class BodyViewModel : DirectiveViewModel;
}
