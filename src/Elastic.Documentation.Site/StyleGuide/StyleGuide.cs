// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components;
using Elastic.Documentation.Site.Components.Admonition;
using Elastic.Documentation.Site.Components.AppliesBadge;
using Elastic.Documentation.Site.Components.AppliesPopover;
using Elastic.Documentation.Site.Components.AskAi;
using Elastic.Documentation.Site.Components.Badge;
using Elastic.Documentation.Site.Components.Banner;
using Elastic.Documentation.Site.Components.Breadcrumb;
using Elastic.Documentation.Site.Components.Button;
using Elastic.Documentation.Site.Components.ButtonGroup;
using Elastic.Documentation.Site.Components.Card;
using Elastic.Documentation.Site.Components.Check;
using Elastic.Documentation.Site.Components.Chip;
using Elastic.Documentation.Site.Components.CodeBlock;
using Elastic.Documentation.Site.Components.CtaCard;
using Elastic.Documentation.Site.Components.DeploymentInfo;
using Elastic.Documentation.Site.Components.DiagnosticsPanel;
using Elastic.Documentation.Site.Components.Disclosure;
using Elastic.Documentation.Site.Components.DocsBar;
using Elastic.Documentation.Site.Components.EmptyState;
using Elastic.Documentation.Site.Components.Feedback;
using Elastic.Documentation.Site.Components.FilterBar;
using Elastic.Documentation.Site.Components.Flyout;
using Elastic.Documentation.Site.Components.Image;
using Elastic.Documentation.Site.Components.Kbd;
using Elastic.Documentation.Site.Components.Link;
using Elastic.Documentation.Site.Components.LinkMenu;
using Elastic.Documentation.Site.Components.LinkTile;
using Elastic.Documentation.Site.Components.Listing;
using Elastic.Documentation.Site.Components.Method;
using Elastic.Documentation.Site.Components.NavSide;
using Elastic.Documentation.Site.Components.PageActions;
using Elastic.Documentation.Site.Components.PageCard;
using Elastic.Documentation.Site.Components.PageJumper;
using Elastic.Documentation.Site.Components.Pager;
using Elastic.Documentation.Site.Components.Prose;
using Elastic.Documentation.Site.Components.ReleaseCard;
using Elastic.Documentation.Site.Components.SearchDialog;
using Elastic.Documentation.Site.Components.SearchField;
using Elastic.Documentation.Site.Components.SearchPage;
using Elastic.Documentation.Site.Components.Segmented;
using Elastic.Documentation.Site.Components.SelectField;
using Elastic.Documentation.Site.Components.SiteFooter;
using Elastic.Documentation.Site.Components.SiteHeader;
using Elastic.Documentation.Site.Components.Skeleton;
using Elastic.Documentation.Site.Components.SplitButton;
using Elastic.Documentation.Site.Components.Stepper;
using Elastic.Documentation.Site.Components.Switch;
using Elastic.Documentation.Site.Components.Tabs;
using Elastic.Documentation.Site.Components.TextField;
using Elastic.Documentation.Site.Components.ThemeSwitch;
using Elastic.Documentation.Site.Components.Toast;
using Elastic.Documentation.Site.Components.Toc;
using Elastic.Documentation.Site.Components.Tooltip;
using Elastic.Documentation.Site.Components.VectorSizing;

namespace Elastic.Documentation.Site.StyleGuide;

public sealed record StyleGuidePage(string Slug, string Title, string Group, string Lead);

public sealed class StyleGuideViewModel
{
	public required StyleGuidePage Page { get; init; }
	public required IReadOnlyList<StyleGuidePage> Pages { get; init; }
	public required Func<string, string> Static { get; init; }

	/// <summary>An exported copy has no dev server behind it, so it leaves out the download button.</summary>
	public bool Offline { get; init; }
}

/// <summary>The living style guide served at <c>/style-guide</c>. Pages render real design-system markup, so the guide cannot drift from the product.</summary>
public static class StyleGuideCatalog
{
	public const string Route = "/style-guide";

	public static IReadOnlyList<StyleGuidePage> Pages { get; } =
	[
		new(
			"foundations",
			"Foundations",
			"Foundations",
			"Colour roles, type, spacing, radius and elevation. Every value on this page is read live from the design tokens."
		),
		new(
			"themes",
			"Themes",
			"Foundations",
			"How light, dark and custom themes work, and how to write your own by overriding tokens only."
		),
		new(
			"layouts",
			"Page width",
			"Foundations",
			"The two width modes, the content measure, and how the sidebar and rail take what is left."
		),
		new(
			"page-layouts",
			"Page layouts",
			"Foundations",
			"The four layouts a page is built on, drawn to scale in both width modes. A new page starts from one of these."
		),
		new(
			"breakpoints",
			"Breakpoints",
			"Foundations",
			"The three widths everything breaks at, and how each layout breaks, shown in live frames."
		),
		new(
			"hero-variants",
			"Hero variants",
			"Foundations",
			"The ten hero variants, the hero with an aside, and the call-to-action band, each with the teal call-to-action button."
		),
		new(
			"backgrounds",
			"Hero and section backgrounds",
			"Foundations",
			"The backgrounds a band can have, from plain to the hero gradient, and which pages use each."
		),
		new("grid", "Grid and rhythm", "Foundations", "The content grid, and the six steps that space everything on a page."),
		new("motion", "Motion", "Foundations", "Durations, easing and the reduced-motion policy."),
		new(
			"decisions",
			"Design decisions and gaps",
			"Foundations",
			"What was normalized, which components were built without a design, and what is not ported yet."
		),
		new(
			"actions",
			"Actions and inputs",
			"Components",
			"Buttons, links and form controls. Each example is the real component, rendered from the model call shown under Code."
		),
		new(
			"content",
			"Content directives",
			"Components",
			"The components behind MyST directives: admonitions, dropdowns, steppers, tabs and buttons."
		),
		new(
			"chrome",
			"Header and footer",
			"Components",
			"The bars above and below the page: the site header, the deployment details, the docs bar with its menus, and the footer."
		),
		new(
			"sidebar",
			"Sidebar and rail",
			"Components",
			"The tree beside the content, with and without islands, and the rail on its right: outline, page actions, call to action and feedback."
		),
		new(
			"search",
			"Search",
			"Components",
			"The search field, the dialog in each of its states, the page jumper that searches as you type, and the full search page."
		),
		new("cards", "Cards and tiles", "Components", "Cards, link tiles and release cards. Hover a card to see the slab shadow."),
		new("feedback", "Feedback and overlays", "Components", "Tooltips, toasts, banners, skeletons, empty states and flyouts."),
		new("navigation", "Navigation parts", "Components", "Breadcrumbs and previous/next links."),
		new(
			"heroes",
			"Heroes and sections",
			"Components",
			"The bands that open a page and the sections that make up the landing pages, in every variant."
		),
		new("badges", "Labels and badges", "Components", "Status and tag badges, HTTP methods, applies-to badges and keyboard keys."),
		new(
			"previews",
			"Page previews",
			"Previews",
			"Whole pages of the isolated, assembler and Codex builds, rendered with placeholder content."
		)
	];

	/// <summary>The component sections shown on each component page, keyed by page slug.</summary>
	public static IReadOnlyDictionary<string, IReadOnlyList<ComponentSection>> ComponentPages
	{
		get;
	} = new Dictionary<string, IReadOnlyList<ComponentSection>>(StringComparer.OrdinalIgnoreCase)
	{
		["actions"] =
		[
			ButtonExamples.Buttons,
			LinkExamples.Links,
			TextFieldExamples.Fields,
			SelectFieldExamples.Selects,
			CheckExamples.Checks,
			SwitchExamples.Switches,
			ChipExamples.Chips,
			SegmentedExamples.Segmenteds,
			FilterBarExamples.FilterBars,
			SplitButtonExamples.SplitButtons
		],
		["content"] =
		[
			AdmonitionExamples.Admonitions,
			DisclosureExamples.Disclosures,
			StepperExamples.Steppers,
			TabsExamples.Tabs,
			ButtonDirectiveExamples.Buttons,
			CodeBlockExamples.CodeBlocks,
			ProseExamples.Articles,
			VectorSizingExamples.Calculators,
			DiagnosticsPanelExamples.Panels,
			PageCardExamples.Cards,
			ImageExamples.Images,
			ListingExamples.Listings
		],
		["chrome"] =
		[
			SiteHeaderExamples.Headers,
			ThemeSwitchExamples.Switches,
			DeploymentInfoExamples.Deployments,
			DocsBarExamples.Bars,
			LinkMenuExamples.Menus,
			SiteFooterExamples.Footers
		],
		["sidebar"] =
		[
			NavSideExamples.Sidebars,
			TocExamples.Tocs,
			PageActionsExamples.Actions,
			CtaCardExamples.Ctas,
			FeedbackExamples.Feedbacks
		],
		["search"] = [SearchFieldExamples.Searches, SearchDialogExamples.Dialogs, PageJumperExamples.Jumpers, SearchPageExamples.Pages],
		["feedback"] =
		[
			AskAiExamples.Assistants,
			TooltipExamples.Tooltips,
			ToastExamples.Toasts,
			BannerExamples.Banners,
			SkeletonExamples.Skeletons,
			EmptyStateExamples.EmptyStates,
			FlyoutExamples.Flyouts
		],
		["cards"] = [CardExamples.Cards, LinkTileExamples.Tiles, ReleaseCardExamples.Releases],
		["navigation"] = [BreadcrumbExamples.Trails, PagerExamples.Pagers],
		["badges"] =
		[
			BadgeExamples.Badges,
			MethodBadgeExamples.Methods,
			AppliesBadgeExamples.Badges,
			AppliesPopoverExamples.Popovers,
			KbdExamples.Keys
		]
	};

	public static StyleGuidePage Default => Pages[0];

	public static StyleGuidePage? Find(string? slug) =>
		string.IsNullOrEmpty(slug)
			? Default
			: Pages.FirstOrDefault(p => string.Equals(p.Slug, slug.Trim('/'), StringComparison.OrdinalIgnoreCase));

	/// <summary>Renders a guide page to a complete HTML document, or <c>null</c> when the slug is unknown.</summary>
	public static Task<string?> RenderAsync(string? slug, Func<string, string> staticUrl, Cancel ctx = default) =>
		RenderPageAsync(slug, staticUrl, offline: false, ctx);

	/// <summary>Renders a guide page, optionally as the offline copy that leaves out the dev-server controls.</summary>
	public static async Task<string?> RenderPageAsync(string? slug, Func<string, string> staticUrl, bool offline, Cancel ctx)
	{
		var page = Find(slug);
		if (page is null)
			return null;

		var model = new StyleGuideViewModel { Page = page, Pages = Pages, Static = staticUrl, Offline = offline };
		using var writer = new StringWriter();
		if (ComponentPages.TryGetValue(page.Slug, out var sections))
		{
			await ComponentsView.Create(new ComponentsPageModel { Guide = model, Sections = sections }).RenderAsync(
				writer,
				cancellationToken: ctx
			);
			return writer.ToString();
		}

		switch (page.Slug)
		{
			case "themes":
				await Themes.Create(model).RenderAsync(writer, cancellationToken: ctx);
				break;
			case "grid":
				await Grid.Create(model).RenderAsync(writer, cancellationToken: ctx);
				break;
			case "hero-variants":
				await HeroVariants.Create(model).RenderAsync(writer, cancellationToken: ctx);
				break;
			case "backgrounds":
				await Backgrounds.Create(model).RenderAsync(writer, cancellationToken: ctx);
				break;
			case "breakpoints":
				await Breakpoints.Create(model).RenderAsync(writer, cancellationToken: ctx);
				break;
			case "page-layouts":
				await PageLayouts.Create(model).RenderAsync(writer, cancellationToken: ctx);
				break;
			case "layouts":
				await Layouts.Create(model).RenderAsync(writer, cancellationToken: ctx);
				break;
			case "motion":
				await Motion.Create(model).RenderAsync(writer, cancellationToken: ctx);
				break;
			case "heroes":
				await Heroes.Create(model).RenderAsync(writer, cancellationToken: ctx);
				break;
			case "previews":
				await Previews.Create(model).RenderAsync(writer, cancellationToken: ctx);
				break;
			case "decisions":
				await Decisions.Create(model).RenderAsync(writer, cancellationToken: ctx);
				break;
			default:
				await Foundations.Create(model).RenderAsync(writer, cancellationToken: ctx);
				break;
		}
		return writer.ToString();
	}
}
