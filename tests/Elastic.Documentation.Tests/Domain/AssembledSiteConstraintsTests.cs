// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using CsCheck;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Links;
using Elastic.Documentation.Publishing;
using Nullean.Entities.CsCheck;
using Nullean.Entities.CsCheck.FileSystem;

namespace Elastic.Documentation.Tests.Domain;

public class AssembledSiteConstraintsTests
{
	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest
			.For(AssembledSiteConstraints.All, DomainGen.Of<AssembledSite>(o => o.UseFileSystem().Normalize<AssembledSite>(Placed)))
			.Options(o => o.UseFileSystem())
			.CounterExamples(CounterExamples)
			.Axes();

	/// <summary>
	/// A site as the assembler builds it: at least two repositories, each named once and published under its own prefix, with the nested tables of contents its docset declares
	/// (the generated docset's, renamed so no two share an identifier), every built table of contents mounted in the navigation, and no api key or redirect source in common.
	/// The generated docset keeps everything else it was generated with.
	/// </summary>
	private static AssembledSite Placed(AssembledSite site)
	{
		IReadOnlyList<AssembledRepository> source = site.Repositories.Count >= 2
			? site.Repositories
			: [.. site.Repositories, .. Enumerable.Repeat(site.Repositories.FirstOrDefault() ?? Kibana(), 2 - site.Repositories.Count)];
		var repositories = source.Select(
			(r, i) =>
				r with
				{
					Name = $"repo{i}",
					UrlPathPrefix = $"reference/repo{i}",
					Docset = r.Docset with
					{
						TableOfContents =
						[
							.. Enumerable.Range(
								0,
								r.Docset.TableOfContents.Descendants().OfCase<IsolatedTableOfContentsRef>().Count() % 3
							).Select(j => NestedToc($"nested{j}"))
						],
						Api = [],
						Redirects = null
					}
				}
		).ToList();
		var mounts = repositories.SelectMany(
			r => r.BuiltTocs().Select(
				t => Mount(
					t.Identifier,
					$"{r.UrlPathPrefix}/{t.Identifier[(t.Identifier.IndexOf("://", StringComparison.Ordinal) + 3)..]}".TrimEnd('/')
				)
			)
		);
		return site with
		{
			Repositories = repositories,
			Navigation = new SiteNavigationDefinition { TableOfContents = [.. mounts.Select(m => new SiteNavigationEntry(m))] }
		};
	}

	private static DeclaringFilePath Declared => DeclaringFilePath.From("/docs/docset.yml");

	private static TocItem NestedToc(string path) =>
		new IsolatedTableOfContentsRef(DocsetPath.From(path), ContainerPath.From(path), [], Declared);

	private static DocumentationSetDefinition Docset(
		IReadOnlyList<TocItem>? toc = null,
		IReadOnlyList<ApiDeclaration>? api = null,
		IReadOnlyDictionary<string, LinkRedirect>? redirects = null
	)
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData> { ["/docs/docset.yml"] = new("project: test") });
		return new DocumentationSetDefinition
		{
			SourceFile = fs.FileInfo.New("/docs/docset.yml"),
			SourceDirectory = fs.DirectoryInfo.New("/docs"),
			FileSystem = fs,
			TableOfContents = toc ?? [],
			Api = api ?? [],
			Redirects = redirects
		};
	}

	private static SiteTableOfContentsRef Mount(string source, string pathPrefix) => new(new Uri(source), pathPrefix, []);

	private static AssembledRepository Kibana() => new("kibana", "reference/kibana", Docset([NestedToc("guides")]));

	private static Dictionary<string, LinkRedirect> Redirect(string source) => new() { [source] = new LinkRedirect { To = "index.md" } };

	private static AssembledSite WithRepository(AssembledSite s, int index, Func<AssembledRepository, AssembledRepository> change) =>
		s with { Repositories = [.. s.Repositories.Select((r, i) => i == index ? change(r) : r)] };

	// each case changes the repositories the generator built (it builds at least two, named repo0, repo1...) or adds one that is wrong in one way
	private static IReadOnlyDictionary<string, Func<AssembledSite, AssembledSite>> CounterExamples =>
		new Dictionary<string, Func<AssembledSite, AssembledSite>>
		{
			["api-key-unique"] =
				s => WithRepository(
					WithRepository(s, 0, r => r with { Docset = r.Docset with { Api = [new ApiDeclaration("es", [])] } }),
					1,
					r => r with { Docset = r.Docset with { Api = [new ApiDeclaration("ES", [])] } }
				),
			["redirect-source-unique"] =
				s => WithRepository(
					WithRepository(
						s,
						0,
						r => r with { UrlPathPrefix = "reference", Docset = r.Docset with { Redirects = Redirect("old.md") } }
					),
					1,
					r => r with { UrlPathPrefix = "reference", Docset = r.Docset with { Redirects = Redirect("old.md") } }
				)
		};
}
