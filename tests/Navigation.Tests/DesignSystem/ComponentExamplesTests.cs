// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AwesomeAssertions;
using Elastic.Documentation.Site.Components;
using Elastic.Documentation.Site.StyleGuide;

namespace Elastic.Documentation.Navigation.Tests.DesignSystem;

public partial class ComponentExamplesTests
{
	private static readonly HtmlParser Parser = new();

	// Classes that are contracts with other tools rather than styling: translation services skip notranslate, and the
	// client-side highlighter adds hljs and reads language-*. Everything else an example emits must be defined in the design-system CSS.
	private static readonly HashSet<string> MarkerClasses = ["notranslate", "hljs"];

	private static bool IsMarker(string name) => MarkerClasses.Contains(name) || name.StartsWith("language-", StringComparison.Ordinal);

	private static readonly Lazy<HashSet<string>> DefinedClasses = new(
		() => DesignSystemFiles
			.AllCss()
			.SelectMany(f => ClassSelector().Matches(DesignSystemFiles.StripComments(File.ReadAllText(f))).Select(m => m.Groups[1].Value))
			.ToHashSet(StringComparer.Ordinal)
	);

	public static IEnumerable<(string Section, string Example)> Examples() => from sections in StyleGuideCatalog.ComponentPages.Values
																			  from section in sections
																			  from example in section.Examples
																			  select (section.ClassName, example.Title);

	private static ComponentExample Find(string section, string title) =>
		StyleGuideCatalog.ComponentPages.Values.SelectMany(s => s).First(s => s.ClassName == section).Examples.First(e => e.Title == title);

	private static async Task<IElement> RenderAsync(string section, string title)
	{
		var html = await Find(section, title).RenderAsync(TestContext.Current!.Execution.CancellationToken);
		return Parser.ParseDocument($"<body>{html}</body>").Body!;
	}

	[Test]
	[MethodDataSource(nameof(Examples))]
	public async Task Example_Renders_WithoutUnknownIconsOrBooleanArtifacts((string Section, string Example) e)
	{
		var body = await RenderAsync(e.Section, e.Example);

		body.InnerHtml.Should().NotBeNullOrWhiteSpace();
		body.InnerHtml.Should().NotContain("unknown icon");
		body.InnerHtml.Should().NotContain("=\"False\"").And.NotContain("=\"True\"");
	}

	[Test]
	[MethodDataSource(nameof(Examples))]
	public async Task Example_UsesOnlyClassesDefinedInDesignSystemCss((string Section, string Example) e)
	{
		var body = await RenderAsync(e.Section, e.Example);

		var used = body
			.QuerySelectorAll("[class]")
			.SelectMany(el => el.ClassList)
			.Concat(body.QuerySelectorAll("svg").SelectMany(el => el.ClassList))
			.Distinct();

		used
			.Where(c => !IsMarker(c) && !DefinedClasses.Value.Contains(c))
			.Should()
			.BeEmpty("every class an example emits must be styled by the design system");
	}

	[Test]
	[MethodDataSource(nameof(Examples))]
	public async Task Example_ExposesAnAccessibleNameOnEveryControl((string Section, string Example) e)
	{
		var body = await RenderAsync(e.Section, e.Example);

		foreach (var control in body.QuerySelectorAll("a[href], button"))
		{
			var name = control.GetAttribute("aria-label") ?? control.TextContent;
			name.Trim().Should().NotBeEmpty($"{control.OuterHtml} needs text or an aria-label");
		}

		foreach (var input in body.QuerySelectorAll("input:not([type=hidden]), select, textarea"))
		{
			var id = input.GetAttribute("id");
			var labelled = input.GetAttribute("aria-label") is not null
				|| input.Closest("label") is not null
				|| (id is not null && body.QuerySelector($"label[for='{id}']") is not null);
			labelled.Should().BeTrue($"{input.OuterHtml} needs a label");
		}

		foreach (var describedBy in body.QuerySelectorAll("[aria-describedby]").SelectMany(
			el => el.GetAttribute("aria-describedby")!.Split(' ')
		))
			body.QuerySelector($"#{describedBy}").Should().NotBeNull($"aria-describedby points at #{describedBy}");
	}

	[Test]
	public async Task Examples_HaveUniqueIdsPerPage()
	{
		foreach (var (slug, sections) in StyleGuideCatalog.ComponentPages)
		{
			var ids = new List<string>();
			foreach (var example in sections.SelectMany(s => s.Examples))
			{
				var html = await example.RenderAsync(TestContext.Current!.Execution.CancellationToken);
				ids.AddRange(Parser.ParseDocument($"<body>{html}</body>").Body!.QuerySelectorAll("[id]").Select(el => el.Id!));
			}

			ids.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key).Should().BeEmpty($"ids on the {slug} page must be unique");
		}
	}

	public static IEnumerable<string> ComponentStylesheets() =>
		Directory.EnumerateFiles(Path.Join(DesignSystemFiles.SiteDirectory, "Components"), "*.css", SearchOption.AllDirectories);

	[Test]
	[MethodDataSource(nameof(ComponentStylesheets))]
	public void ComponentStylesheet_IsImportedByTheBundle(string file)
	{
		var bundle = File.ReadAllText(Path.Join(DesignSystemFiles.SiteDirectory, "Assets", "ds.css"));
		var relative = Path.GetRelativePath(Path.Join(DesignSystemFiles.SiteDirectory, "Assets"), file).Replace('\\', '/');

		bundle.Should().Contain($"@import '{relative}' layer(ds.components);");
	}

	[GeneratedRegex(@"\.([a-zA-Z_][\w-]*)")]
	private static partial Regex ClassSelector();
}
