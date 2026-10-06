// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Links;
using Elastic.Documentation.Publishing;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class AssembledSiteConstraintsTests
{
	[Test]
	public void Laws() => AssembledSiteConstraints.All.Laws(Valid(), CounterExamples);

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

	private static AssembledSite Site(params AssembledRepository[] repositories) =>
		new(
			repositories,
			new SiteNavigationDefinition
			{
				TableOfContents =
				[
					new SiteNavigationEntry(Mount("kibana://", "reference/kibana")),
					new SiteNavigationEntry(Mount("kibana://guides", "reference/kibana/guides")),
					new SiteNavigationEntry(Mount("elasticsearch://", "reference/elasticsearch"))
				]
			}
		);

	private static AssembledRepository Kibana(DocumentationSetDefinition? docset = null, string prefix = "reference/kibana") =>
		new("kibana", prefix, docset ?? Docset([NestedToc("guides")]));

	private static AssembledRepository Elasticsearch(
		DocumentationSetDefinition? docset = null,
		string prefix = "reference/elasticsearch"
	) => new("elasticsearch", prefix, docset ?? Docset());

	private static AssembledSite Valid() => Site(Kibana(), Elasticsearch());

	private static Dictionary<string, LinkRedirect> Redirect(string source) => new() { [source] = new LinkRedirect { To = "index.md" } };

	private static IReadOnlyDictionary<string, Func<AssembledSite, AssembledSite>> CounterExamples =>
		new Dictionary<string, Func<AssembledSite, AssembledSite>>
		{
			["toc-identifiers-unique"] = _ => Site(Kibana(), Kibana(prefix: "reference/kibana-again"), Elasticsearch()),
			["nested-toc-declared"] = _ => Site(Kibana(Docset([NestedToc("guides"), NestedToc("extra")])), Elasticsearch()),
			["built-toc-placed"] =
				_ => Site(Kibana(), Elasticsearch(), new AssembledRepository("logstash", "reference/logstash", Docset())),
			["api-key-unique"] =
				_ => Site(
					Kibana(Docset([NestedToc("guides")], [new ApiDeclaration("es", [])])),
					Elasticsearch(Docset(api: [new ApiDeclaration("ES", [])]))
				),
			["redirect-source-unique"] =
				_ => Site(
					Kibana(Docset([NestedToc("guides")], redirects: Redirect("old.md")), prefix: "reference"),
					Elasticsearch(Docset(redirects: Redirect("old.md")), prefix: "reference")
				)
		};
}
