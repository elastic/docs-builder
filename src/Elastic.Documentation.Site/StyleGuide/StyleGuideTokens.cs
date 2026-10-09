// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.StyleGuide;

public sealed record TokenInfo(string Name, string Role);

/// <summary>
/// The tokens the style guide documents. A test asserts each name is defined in the design-system CSS,
/// so a renamed or removed token fails the build instead of leaving a stale swatch.
/// </summary>
public static class StyleGuideTokens
{
	public static IReadOnlyList<TokenInfo> Surfaces { get; } =
	[
		new("--color-page", "Page background"),
		new("--color-surface", "Cards, inputs, overlays"),
		new("--color-surface-sunken", "Wells, code, sidebars")
	];

	public static IReadOnlyList<TokenInfo> Text { get; } =
	[
		new("--color-heading", "Headings"),
		new("--color-text", "Body copy"),
		new("--color-text-muted", "Secondary copy, captions")
	];

	public static IReadOnlyList<TokenInfo> Borders { get; } =
	[
		new("--color-border", "Dividers, card outlines"),
		new("--color-border-control", "Input and control outlines"),
		new("--color-border-strong", "Slab outlines, emphasis")
	];

	public static IReadOnlyList<TokenInfo> Accent { get; } =
	[
		new("--color-accent", "Links, primary actions"),
		new("--color-accent-hover", "Hover for accent"),
		new("--color-on-accent", "Text on accent fills"),
		new("--color-accent-surface", "Selected, tinted fills"),
		new("--color-accent-border", "Tinted outlines"),
		new("--color-brand-deep", "Slab plate, brand blocks"),
		new("--color-brand-bright", "Brand highlight"),
		new("--color-highlight", "Editorial highlight"),
		new("--color-emphasis", "Tags and badges that stand out"),
		new("--color-on-emphasis", "Text on emphasis fills"),
		new("--color-focus-ring", "Keyboard focus")
	];

	public static IReadOnlyList<TokenInfo> Code { get; } =
	[
		new("--color-code-bg", "Code panel background"),
		new("--color-code-border", "Code panel outline"),
		new("--color-code-text", "Code text"),
		new("--color-code-muted", "Code headers and comments"),
		new("--color-code-gutter", "Line numbers"),
		new("--color-code-keyword", "Keywords"),
		new("--color-code-string", "Strings"),
		new("--color-code-number", "Numbers"),
		new("--color-code-attr", "Attributes"),
		new("--color-code-type", "Types"),
		new("--color-code-variable", "Variables"),
		new("--color-code-literal", "Literals"),
		new("--color-code-punctuation", "Punctuation")
	];

	public static IReadOnlyList<TokenInfo> Status { get; } =
	[
		new("--color-success", "Success text and icons"),
		new("--color-success-surface", "Success fill"),
		new("--color-warning", "Warning text and icons"),
		new("--color-warning-surface", "Warning fill"),
		new("--color-danger", "Error text and icons"),
		new("--color-danger-surface", "Error fill"),
		new("--color-important", "Important text and icons"),
		new("--color-important-surface", "Important fill")
	];

	public static IReadOnlyList<TokenInfo> Palette { get; } =
	[
		new("--palette-developer-blue", "Developer Blue"),
		new("--palette-midnight", "Midnight"),
		new("--palette-elastic-blue", "Elastic Blue"),
		new("--palette-elastic-blue-dark", "Elastic Blue, dark"),
		new("--palette-teal", "Teal"),
		new("--palette-pink", "Pink"),
		new("--palette-grey-50", "Grey 50"),
		new("--palette-grey-100", "Grey 100"),
		new("--palette-grey-300", "Grey 300"),
		new("--palette-grey-600", "Grey 600"),
		new("--palette-grey-800", "Grey 800"),
		new("--palette-white", "White")
	];

	public static IReadOnlyList<TokenInfo> TextSizes { get; } =
	[
		new("--text-xs", "12px · captions, labels"),
		new("--text-sm", "14px · UI text, tables"),
		new("--text-base", "16px · body"),
		new("--text-lg", "18px · lead"),
		new("--text-xl", "20px · h4"),
		new("--text-2xl", "24px · h3"),
		new("--text-3xl", "28px · h2"),
		new("--text-4xl", "40px · h1"),
		new("--text-5xl", "64px · display")
	];

	public static IReadOnlyList<TokenInfo> Spacing { get; } =
	[
		new("--space-1", "4px"),
		new("--space-2", "8px"),
		new("--space-3", "12px"),
		new("--space-4", "16px"),
		new("--space-5", "24px"),
		new("--space-6", "32px"),
		new("--space-7", "48px"),
		new("--space-8", "72px")
	];

	public static IReadOnlyList<TokenInfo> Radii { get; } =
	[
		new("--radius-sm", "4px · chips, code"),
		new("--radius-md", "6px · controls, cards"),
		new("--radius-lg", "12px · panels"),
		new("--radius-pill", "pill")
	];

	public static IReadOnlyList<TokenInfo> Shadows { get; } =
	[
		new("--shadow-slab-rest", "Resting slab"),
		new("--shadow-slab-sm", "Small slab"),
		new("--shadow-slab", "Raised slab")
	];

	public static IReadOnlyList<TokenInfo> Durations { get; } =
	[
		new("--duration-fast", "120ms · hover and focus colour changes"),
		new("--duration-base", "200ms · default for state changes"),
		new("--duration-slow", "360ms · entrances and large movement")
	];

	public static IReadOnlyList<TokenInfo> AllNames { get; } =
	[
		.. Surfaces,
		.. Text,
		.. Borders,
		.. Accent,
		.. Status,
		.. Palette,
		.. TextSizes,
		.. Spacing,
		.. Radii,
		.. Shadows,
		.. Durations,
		new("--ease-standard", "Standard easing"),
		new("--ease-spring", "Spring easing"),
		new("--font-body", "Body font"),
		new("--font-display", "Display font"),
		new("--font-mono", "Monospace font")
	];
}
