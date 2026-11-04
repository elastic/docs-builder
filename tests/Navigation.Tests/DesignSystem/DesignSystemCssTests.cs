// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Site.StyleGuide;

namespace Elastic.Documentation.Navigation.Tests.DesignSystem;

public static partial class DesignSystemFiles
{
	public static string SiteDirectory { get; } = Path.Join(
		Paths.GetSolutionDirectory()?.FullName ?? throw new InvalidOperationException("Solution directory not found"),
		"src",
		"Elastic.Documentation.Site"
	);

	public static IEnumerable<string> AllCss() =>
		Directory
			.EnumerateFiles(Path.Join(SiteDirectory, "Assets", "ds"), "*.css", SearchOption.AllDirectories)
			.Append(Path.Join(SiteDirectory, "Assets", "ds.css"))
			.Append(Path.Join(SiteDirectory, "Assets", "ds-guide.css"))
			.Concat(
				Directory.Exists(Path.Join(SiteDirectory, "Components"))
					? Directory.EnumerateFiles(Path.Join(SiteDirectory, "Components"), "*.css", SearchOption.AllDirectories)
					: []
			);

	// Raw values belong to the token and theme files; everything else must read tokens so a theme can restyle it.
	public static IEnumerable<string> ComponentCss() =>
		AllCss().Where(
			f => !f.Contains($"{Path.DirectorySeparatorChar}tokens{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
				!f.Contains($"{Path.DirectorySeparatorChar}themes{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
				!f.EndsWith("fonts.css", StringComparison.Ordinal)
		);

	public static string StripComments(string css) => CssComment().Replace(css, "");

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex CssComment();
}

public partial class DesignSystemCssTests
{
	public static IEnumerable<string> TokenNames() => StyleGuideTokens.AllNames.Select(t => t.Name);

	public static IEnumerable<string> ComponentCssFiles() => DesignSystemFiles.ComponentCss();

	[Test]
	[MethodDataSource(nameof(TokenNames))]
	public void StyleGuideToken_IsDefinedInDesignSystemCss(string token)
	{
		var definitions = DesignSystemFiles
			.AllCss()
			.SelectMany(f => File.ReadAllLines(f))
			.Any(l => l.TrimStart().StartsWith($"{token}:", StringComparison.Ordinal));

		definitions.Should().BeTrue($"the style guide documents {token}, so a design-system CSS file must define it");
	}

	[Test]
	[MethodDataSource(nameof(ComponentCssFiles))]
	public void ComponentCss_ReadsTokensOnly(string file)
	{
		var css = DesignSystemFiles.StripComments(File.ReadAllText(file));

		HexColor().Matches(css).Select(m => m.Value).Should().BeEmpty($"{Path.GetFileName(file)} must use colour roles, not hex literals");
		ColorFunction()
			.Matches(css)
			.Select(m => m.Value)
			.Should()
			.BeEmpty($"{Path.GetFileName(file)} must use colour roles, not colour functions");
		css.Should().NotContain("!important", $"{Path.GetFileName(file)} relies on layer order, not !important");
	}

	public static IEnumerable<string> AllCssFiles() => DesignSystemFiles.AllCss();

	// The system has three breakpoints (see Page width in the style guide); the elastic.co header ships its own at 1200.
	private static readonly HashSet<string> BreakpointWidths = ["760", "761", "860", "861", "1100", "1101"];

	[Test]
	[MethodDataSource(nameof(AllCssFiles))]
	public void Css_UsesOnlyTheSystemBreakpoints(string file)
	{
		if (file.EndsWith("elastic-nav.css", StringComparison.Ordinal))
			return;
		var css = DesignSystemFiles.StripComments(File.ReadAllText(file));

		MediaQuery()
			.Matches(css)
			.SelectMany(q => MediaWidth().Matches(q.Value))
			.Select(m => m.Groups[1].Value)
			.Where(w => !BreakpointWidths.Contains(w))
			.Should()
			.BeEmpty($"{Path.GetFileName(file)} may only use the 760, 860 and 1100 breakpoints");
	}

	// Container queries answer to a component's own width and are not breakpoints.
	[GeneratedRegex(@"@media[^{]*")]
	private static partial Regex MediaQuery();

	[GeneratedRegex(@"\((?:min|max)-width:\s*(\d+)px\)")]
	private static partial Regex MediaWidth();

	[Test]
	[MethodDataSource(nameof(AllCssFiles))]
	public void DesignSystemCss_DoesNotNeedNativeLightDark(string file)
	{
		var css = DesignSystemFiles.StripComments(File.ReadAllText(file));

		css.Should().NotContain(
			"light-dark(",
			"colour roles use the var(--ds-l) / var(--ds-d) toggle pair so they work in browsers without light-dark()"
		);
	}

	[Test]
	public void Theme_DeclaresRolesOnEveryPinnedElementSoNestedThemesWork()
	{
		var theme = File.ReadAllText(
			Path.Join(DesignSystemFiles.SiteDirectory, "Assets", "ds", "themes", "bold-minimal.css")
		).ReplaceLineEndings("\n");

		theme.Should().Contain(":root,\n[data-theme] {");
		theme.Should().Contain("[data-theme='dark']").And.Contain("[data-theme='light']").And.Contain("prefers-color-scheme: dark");
	}

	[GeneratedRegex(@"(?<![\w&-])#[0-9a-fA-F]{3,8}\b")]
	private static partial Regex HexColor();

	[GeneratedRegex(@"\b(?:rgb|rgba|hsl|hsla|oklch|oklab|lab|lch)\(")]
	private static partial Regex ColorFunction();
}
