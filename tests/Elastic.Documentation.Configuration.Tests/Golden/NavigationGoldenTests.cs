// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>What <c>navigation.yml</c> and <c>navigation_preview.yml</c> load into today, and what filtering out private repositories leaves.</summary>
public class NavigationGoldenTests
{
	private const string NavigationPath = "/config/navigation.yml";

	/// <summary>The diagnostics the load wrote, then the model, or what it threw.</summary>
	private static string Load(string yaml, bool dumpModel = true)
	{
		var collector = new CapturingDiagnosticsCollector();
		var sb = new StringBuilder();
		try
		{
			var file = SiteNavigationFile.Deserialize(yaml, collector, NavigationPath);
			_ = sb.AppendLine("diagnostics:").Append(collector.Captured.ToSnapshotText());
			_ = sb.Append(dumpModel ? ModelDump.ToText(file) : SiteNavDump.ToText(file));
		}
		catch (Exception e)
		{
			_ = sb.AppendLine("diagnostics:").Append(collector.Captured.ToSnapshotText());
			for (var current = e; current is not null; current = current.InnerException)
				_ = sb
					.Append(current == e ? "throws " : "inner ")
					.Append(current.GetType().Name)
					.Append(": ")
					.AppendLine(current.Message.Replace('\\', '/'));
		}
		return sb.ToString();
	}

	// the derived path properties repeat the path prefix; the small cases below show them in full
	private static string ModelWithoutDerivedPaths(SiteNavigationFile file) =>
		ModelDump.ToText(file, "PathRelativeToDocumentationSet", "PathRelativeToContainer", "Context");

	private static SiteNavigationFile Parse(string yaml) => SiteNavigationFile.Deserialize(yaml);

	[Test]
	public void Baseline_Navigation_MatchSnapshot()
	{
		var yaml = GoldenConfig.BaselineText(GoldenConfig.Navigation);
		var file = Parse(yaml);
		SiteNavDump.ToText(file).Should().MatchSnapshot(name: "tree");
		ModelWithoutDerivedPaths(file).Should().MatchSnapshot(name: "model");
	}

	[Test]
	public void Baseline_NavigationPreview_MatchSnapshot()
	{
		var yaml = GoldenConfig.BaselineText(GoldenConfig.NavigationPreview);
		var file = Parse(yaml);
		SiteNavDump.ToText(file).Should().MatchSnapshot(name: "tree");
		ModelWithoutDerivedPaths(file).Should().MatchSnapshot(name: "model");
	}

	[Test]
	public void Baseline_Navigation_LoadsWithoutDiagnostics() =>
		DiagnosticsOf(GoldenConfig.BaselineText(GoldenConfig.Navigation)).Should().MatchInlineSnapshot("");

	[Test]
	public void Baseline_NavigationPreview_LoadsWithoutDiagnostics() =>
		DiagnosticsOf(GoldenConfig.BaselineText(GoldenConfig.NavigationPreview)).Should().MatchInlineSnapshot("");

	private static string DiagnosticsOf(string yaml)
	{
		var collector = new CapturingDiagnosticsCollector();
		_ = SiteNavigationFile.Deserialize(yaml, collector, NavigationPath);
		return collector.Captured.ToSnapshotText();
	}

	[Test]
	public void Baseline_DeclaredSourcesAndPrefixes_MatchSnapshot()
	{
		var sb = new StringBuilder();
		foreach (var name in new[] { GoldenConfig.Navigation, GoldenConfig.NavigationPreview })
		{
			var file = Parse(GoldenConfig.BaselineText(name));
			_ = sb.Append("== ").AppendLine(name);
			_ = sb.AppendLine("declared sources:");
			foreach (var source in SiteNavigationFile.GetAllDeclaredSources(file).Select(u => u.ToString()).Order(StringComparer.Ordinal))
				_ = sb.Append("  ").AppendLine(source);
			_ = sb.AppendLine("phantom prefixes:");
			foreach (var prefix in SiteNavigationFile.GetPhantomPrefixes(file).Select(u => u.ToString()).Order(StringComparer.Ordinal))
				_ = sb.Append("  ").AppendLine(prefix);
			var collector = new CapturingDiagnosticsCollector();
			var fileSystem = new MockFileSystem();
			var valid = SiteNavigationFile.ValidatePathPrefixes(collector, file, fileSystem.FileInfo.New(NavigationPath));
			_ = sb.Append("path prefixes valid: ").AppendLine(valid.ToString());
			_ = sb.Append(collector.Captured.ToSnapshotText());
		}
		sb.ToString().Should().MatchSnapshot();
	}

	[Test]
	public void Baseline_PrivateRepositoriesFiltered_Navigation() => FilteredNavigation(GoldenConfig.Navigation, previewFirst: false);

	[Test]
	public void Baseline_PrivateRepositoriesFiltered_NavigationPreview() =>
		FilteredNavigation(GoldenConfig.NavigationPreview, previewFirst: true);

	private static void FilteredNavigation(string name, bool previewFirst)
	{
		using var provider = GoldenConfig.CreateProvider(skipPrivateRepositories: true);
		if (previewFirst)
			_ = provider.UseNavigationPreview();
		var config = AssemblyConfiguration.Create(provider);
		var filtered = provider.CreateNavigationFile(config);
		var text = filtered.OpenText().ReadToEnd();
		var unfiltered = GoldenConfig.BaselineText(name);

		text.Should().MatchSnapshot(name: "filtered-yaml");
		SiteNavDump.ToText(Parse(text)).Should().MatchSnapshot(name: "filtered-tree");
		// the filter is asked again: the same file comes back
		provider.CreateNavigationFile(config).FullName.Should().Be(filtered.FullName);
		unfiltered.Should().NotBe(text);
	}

	[Test]
	public void FilterWithoutPrivateRepositories_Filtered_ReturnsTheFileUntouched()
	{
		using var provider = GoldenConfig.CreateProvider(skipPrivateRepositories: true);
		var config = AssemblyConfiguration.Deserialize("references:\n  open:\n", skipPrivateRepositories: true);
		provider.CreateNavigationFile(config).FullName.Should().Be(provider.NavigationFile.FullName);
	}

	[Test]
	public void FilterWhenNotSkippingPrivate_Filtered_ReturnsTheFileUntouched()
	{
		using var provider = GoldenConfig.CreateProvider();
		var config = AssemblyConfiguration.Deserialize("references:\n  closed:\n    private: true\n");
		provider.CreateNavigationFile(config).FullName.Should().Be(provider.NavigationFile.FullName);
	}

	private static string Filter(string navigationYaml, string assemblerYaml)
	{
		using var provider = GoldenConfig.CreateProvider(GoldenConfig.Navigation, navigationYaml, skipPrivateRepositories: true);
		var config = AssemblyConfiguration.Deserialize(assemblerYaml, skipPrivateRepositories: true);
		return provider.CreateNavigationFile(config).OpenText().ReadToEnd();
	}

	[Test]
	public void FilterRemovesAPrivateTocAndMovesItsChildrenUp_Filtered_Outcome() =>
		Filter(
			"""
			toc:
			  - toc: open://one
			    path_prefix: one
			    children:
			      - toc: closed://two
			        path_prefix: two
			        children:
			          - toc: open://three
			            path_prefix: three
			      - toc: open://four
			        path_prefix: four
			  - toc: open://five
			    path_prefix: five
			""",
			"references:\n  open:\n  closed:\n    private: true\n"
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			toc:
			  - toc: open://one
			    path_prefix: one
			    children:
			      - toc: open://three
			        path_prefix: three
			      - toc: open://four
			        path_prefix: four
			  - toc: open://five
			    path_prefix: five
			"""
			);

	[Test]
	public void FilterOfAPrivateTocAtTheTopLevel_Filtered_Outcome() =>
		Filter(
			"toc:\n  - toc: closed://one\n    path_prefix: one\n  - toc: open://two\n    path_prefix: two\n",
			"references:\n  open:\n  closed:\n    private: true\n"
		).Should().MatchInlineSnapshot("""
			toc:
			  - toc: open://two
			    path_prefix: two
			""");

	[Test]
	public void FilterOfSkippedPrivateRepository_Filtered_IsNotRemoved() =>
		Filter("toc:\n  - toc: closed://one\n    path_prefix: one\n", "references:\n  closed:\n    private: true\n    skip: true\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				toc:
				  - toc: closed://one
				    path_prefix: one
				"""
			);

	[Test]
	public void FilterMatchesTheRepositoryNamePrefixOnly_Filtered_Outcome() =>
		Filter(
			"toc:\n  - toc: closed-but-longer://one\n    path_prefix: one\n  - toc: closed://two\n    path_prefix: two\n",
			"references:\n  closed:\n    private: true\n"
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			toc:
			  - toc: closed-but-longer://one
			    path_prefix: one
			"""
			);

	[Test]
	public void EmptyFile_Loaded_Outcome() =>
		Load("")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		throws InvalidOperationException: Failed to deserialize YAML into 'Elastic.Documentation.Configuration.Toc.SiteNavigationFile'.
		"""
			);

	[Test]
	public void EmptyToc_Loaded_Outcome() =>
		Load("toc: []\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		model <SiteNavigationFile>:
		  Phantoms: []
		  TableOfContents: []
		"""
			);

	[Test]
	public void TocAsMap_Loaded_Outcome() =>
		Load("toc:\n  a: b\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		model <SiteNavigationFile>:
		  Phantoms: []
		  TableOfContents: []
		"""
			);

	[Test]
	public void UnknownTopLevelKey_Loaded_Outcome() =>
		Load("bogus: 1\ntoc: []\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		error /config/navigation.yml:1 Unknown key 'bogus'. Valid keys: phantoms, toc.
		model <SiteNavigationFile>:
		  Phantoms: []
		  TableOfContents: []
		"""
			);

	[Test]
	public void Phantoms_Loaded_Outcome() =>
		Load("phantoms:\n  - toc: redirects\n  - toc: other://thing\n  - toc:\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			model <SiteNavigationFile>:
			  Phantoms:
			    - <PhantomRegistration>:
			      Source: "redirects"
			    - <PhantomRegistration>:
			      Source: "other://thing"
			    - <PhantomRegistration>:
			      Source: null
			  TableOfContents: []
			"""
			);

	[Test]
	public void PhantomUnknownKey_Loaded_Outcome() =>
		Load("phantoms:\n  - toc: a\n    extra: b\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		error /config/navigation.yml:3 Unknown key 'extra' in $.phantoms[0]. Valid keys: toc.
		model <SiteNavigationFile>:
		  Phantoms:
		    - <PhantomRegistration>:
		      Source: "a"
		  TableOfContents: []
		"""
			);

	[Test]
	public void BareTocSourceDefaultsToDocsContent_Loaded_Outcome() =>
		Load("toc:\n  - toc: reference/elasticsearch\n    path_prefix: reference/elasticsearch\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			model <SiteNavigationFile>:
			  Phantoms: []
			  TableOfContents:
			    - <SiteTableOfContentsRef>:
			      Source: docs-content://reference/elasticsearch
			      PathPrefix: "reference/elasticsearch"
			      Children: []
			      Island: False
			      NavigationTitle: null
			      PathRelativeToDocumentationSet: "reference/elasticsearch"
			      PathRelativeToContainer: "reference/elasticsearch"
			      Context: ""
			"""
			);

	[Test]
	public void TocWithEveryKey_Loaded_Outcome() =>
		Load(
			"""
			toc:
			  - toc: repo://a
			    path_prefix: p/a
			    island: true
			    navigation_title: Title A
			    children:
			      - toc: repo://b
			        path_prefix: p/b
			        island: false
			""",
			dumpModel: true
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			model <SiteNavigationFile>:
			  Phantoms: []
			  TableOfContents:
			    - <SiteTableOfContentsRef>:
			      Source: repo://a/
			      PathPrefix: "p/a"
			      Children:
			        - <SiteTableOfContentsRef>:
			          Source: repo://b/
			          PathPrefix: "p/b"
			          Children: []
			          Island: False
			          NavigationTitle: null
			          PathRelativeToDocumentationSet: "p/b"
			          PathRelativeToContainer: "p/b"
			          Context: ""
			      Island: True
			      NavigationTitle: "Title A"
			      PathRelativeToDocumentationSet: "p/a"
			      PathRelativeToContainer: "p/a"
			      Context: ""
			"""
			);

	[Test]
	public void TocIslandSpelledYes_Loaded_Outcome() =>
		Load("toc:\n  - toc: repo://a\n    island: yes\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				model <SiteNavigationFile>:
				  Phantoms: []
				  TableOfContents:
				    - <SiteTableOfContentsRef>:
				      Source: repo://a/
				      PathPrefix: ""
				      Children: []
				      Island: False
				      NavigationTitle: null
				      PathRelativeToDocumentationSet: ""
				      PathRelativeToContainer: ""
				      Context: ""
				"""
			);

	[Test]
	public void TocIslandUppercase_Loaded_Outcome() =>
		Load("toc:\n  - toc: repo://a\n    island: TRUE\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				model <SiteNavigationFile>:
				  Phantoms: []
				  TableOfContents:
				    - <SiteTableOfContentsRef>:
				      Source: repo://a/
				      PathPrefix: ""
				      Children: []
				      Island: True
				      NavigationTitle: null
				      PathRelativeToDocumentationSet: ""
				      PathRelativeToContainer: ""
				      Context: ""
				"""
			);

	[Test]
	public void TocBlankNavigationTitle_Loaded_Outcome() =>
		Load("toc:\n  - toc: repo://a\n    navigation_title: '  '\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				model <SiteNavigationFile>:
				  Phantoms: []
				  TableOfContents:
				    - <SiteTableOfContentsRef>:
				      Source: repo://a/
				      PathPrefix: ""
				      Children: []
				      Island: False
				      NavigationTitle: null
				      PathRelativeToDocumentationSet: ""
				      PathRelativeToContainer: ""
				      Context: ""
				"""
			);

	[Test]
	public void TocWithoutPathPrefix_Loaded_Outcome() =>
		Load("toc:\n  - toc: repo://a\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				model <SiteNavigationFile>:
				  Phantoms: []
				  TableOfContents:
				    - <SiteTableOfContentsRef>:
				      Source: repo://a/
				      PathPrefix: ""
				      Children: []
				      Island: False
				      NavigationTitle: null
				      PathRelativeToDocumentationSet: ""
				      PathRelativeToContainer: ""
				      Context: ""
				"""
			);

	[Test]
	public void TocBlankSource_Loaded_Outcome() =>
		Load("toc:\n  - toc: ''\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		model <SiteNavigationFile>:
		  Phantoms: []
		  TableOfContents:
		    - <SiteTableOfContentsRef>:
		      Source: docs-content:///
		      PathPrefix: ""
		      Children: []
		      Island: False
		      NavigationTitle: null
		      PathRelativeToDocumentationSet: ""
		      PathRelativeToContainer: ""
		      Context: ""
		"""
			);

	[Test]
	public void TocSourceThatIsNotAUri_Loaded_Outcome() =>
		Load("toc:\n  - toc: 'http://['\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		throws YamlException: Invalid TOC source: 'http://[' could not be parsed as a URI
		inner InvalidOperationException: Invalid TOC source: 'http://[' could not be parsed as a URI
		"""
			);

	[Test]
	public void TocSourceWithSpaces_Loaded_Outcome() =>
		Load("toc:\n  - toc: 'my repo://a b'\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		throws YamlException: Invalid TOC source: 'my repo://a b' could not be parsed as a URI
		inner InvalidOperationException: Invalid TOC source: 'my repo://a b' could not be parsed as a URI
		"""
			);

	[Test]
	public void TocSourceInUppercase_Loaded_Outcome() =>
		Load("toc:\n  - toc: Repo://Path/To\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		model <SiteNavigationFile>:
		  Phantoms: []
		  TableOfContents:
		    - <SiteTableOfContentsRef>:
		      Source: repo://path/To
		      PathPrefix: ""
		      Children: []
		      Island: False
		      NavigationTitle: null
		      PathRelativeToDocumentationSet: ""
		      PathRelativeToContainer: ""
		      Context: ""
		"""
			);

	[Test]
	public void TocSourceAsList_Loaded_Outcome() =>
		Load("toc:\n  - toc: [a, b]\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		throws YamlException: toc entry has no 'toc:' key and will be ignored. Found keys: 'toc'. Check for typos.
		"""
			);

	[Test]
	public void EntryWithoutTocKey_Loaded_Outcome() =>
		Load("toc:\n  - path_prefix: a\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		throws YamlException: toc entry has no 'toc:' key and will be ignored. Found keys: 'path_prefix'. Check for typos.
		"""
			);

	[Test]
	public void EntryThatIsAScalar_Loaded_Outcome() =>
		Load("toc:\n  - just-a-string\n  - toc: repo://a\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				model <SiteNavigationFile>:
				  Phantoms: []
				  TableOfContents:
				    - <SiteTableOfContentsRef>:
				      Source: repo://a/
				      PathPrefix: ""
				      Children: []
				      Island: False
				      NavigationTitle: null
				      PathRelativeToDocumentationSet: ""
				      PathRelativeToContainer: ""
				      Context: ""
				"""
			);

	[Test]
	public void EntryUnknownKeyTypo_Loaded_Outcome() =>
		Load("toc:\n  - toc: repo://a\n    path_prefx: a\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /config/navigation.yml:3 Unknown key 'path_prefx' in $.toc[0]. Did you mean 'path_prefix'?
				model <SiteNavigationFile>:
				  Phantoms: []
				  TableOfContents:
				    - <SiteTableOfContentsRef>:
				      Source: repo://a/
				      PathPrefix: ""
				      Children: []
				      Island: False
				      NavigationTitle: null
				      PathRelativeToDocumentationSet: ""
				      PathRelativeToContainer: ""
				      Context: ""
				"""
			);

	[Test]
	public void ChildUnknownKey_Loaded_Outcome() =>
		Load("toc:\n  - toc: repo://a\n    children:\n      - toc: repo://b\n        colour: red\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				error /config/navigation.yml:5 Unknown key 'colour' in $.toc[0].children[0]. Valid keys: children, island, navigation_title, path_prefix, toc.
				model <SiteNavigationFile>:
				  Phantoms: []
				  TableOfContents:
				    - <SiteTableOfContentsRef>:
				      Source: repo://a/
				      PathPrefix: ""
				      Children:
				        - <SiteTableOfContentsRef>:
				          Source: repo://b/
				          PathPrefix: ""
				          Children: []
				          Island: False
				          NavigationTitle: null
				          PathRelativeToDocumentationSet: ""
				          PathRelativeToContainer: ""
				          Context: ""
				      Island: False
				      NavigationTitle: null
				      PathRelativeToDocumentationSet: ""
				      PathRelativeToContainer: ""
				      Context: ""
				"""
			);

	[Test]
	public void ChildWithoutTocKey_Loaded_Outcome() =>
		Load("toc:\n  - toc: repo://a\n    children:\n      - path_prefix: b\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			throws YamlException: toc entry has no 'toc:' key and will be ignored. Found keys: 'path_prefix'. Check for typos.
			"""
			);

	[Test]
	public void ChildrenAsMap_Loaded_Outcome() =>
		Load("toc:\n  - toc: repo://a\n    children:\n      b: c\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				model <SiteNavigationFile>:
				  Phantoms: []
				  TableOfContents:
				    - <SiteTableOfContentsRef>:
				      Source: repo://a/
				      PathPrefix: ""
				      Children: []
				      Island: False
				      NavigationTitle: null
				      PathRelativeToDocumentationSet: ""
				      PathRelativeToContainer: ""
				      Context: ""
				"""
			);

	[Test]
	public void ChildrenAsScalar_Loaded_Outcome() =>
		Load("toc:\n  - toc: repo://a\n    children: none\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				model <SiteNavigationFile>:
				  Phantoms: []
				  TableOfContents:
				    - <SiteTableOfContentsRef>:
				      Source: repo://a/
				      PathPrefix: ""
				      Children: []
				      Island: False
				      NavigationTitle: null
				      PathRelativeToDocumentationSet: ""
				      PathRelativeToContainer: ""
				      Context: ""
				"""
			);

	[Test]
	public void ChildOfAChildIsNotAnEntryKind_Loaded_Outcome() =>
		Load("toc:\n  - toc: repo://a\n    children:\n      - section: S\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /config/navigation.yml:4 Unknown key 'section' in $.toc[0].children[0]. Valid keys: children, island, navigation_title, path_prefix, toc.
			throws YamlException: toc entry has no 'toc:' key and will be ignored. Found keys: . Check for typos.
			"""
			);

	[Test]
	public void DuplicateKeyInEntry_Loaded_Outcome() =>
		Load("toc:\n  - toc: repo://a\n    toc: repo://b\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				model <SiteNavigationFile>:
				  Phantoms: []
				  TableOfContents:
				    - <SiteTableOfContentsRef>:
				      Source: repo://b/
				      PathPrefix: ""
				      Children: []
				      Island: False
				      NavigationTitle: null
				      PathRelativeToDocumentationSet: ""
				      PathRelativeToContainer: ""
				      Context: ""
				"""
			);

	[Test]
	public void SectionWithChildren_Loaded_Outcome() =>
		Load("toc:\n  - section: Reference\n    children:\n      - toc: repo://a\n        path_prefix: a\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				model <SiteNavigationFile>:
				  Phantoms: []
				  TableOfContents:
				    - <SiteSectionRef>:
				      Title: "Reference"
				      ExternalUrl: null
				      Children:
				        - <SiteTableOfContentsRef>:
				          Source: repo://a/
				          PathPrefix: "a"
				          Children: []
				          Island: False
				          NavigationTitle: null
				          PathRelativeToDocumentationSet: "a"
				          PathRelativeToContainer: "a"
				          Context: ""
				      DropdownLinks: []
				      FloatingKind: null
				      IsExternal: False
				      IsDropdown: False
				"""
			);

	[Test]
	public void SectionExternal_Loaded_Outcome() =>
		Load("toc:\n  - section: Blog\n    external: https://example.com/blog\n  - section: Empty\n    external: ''\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				model <SiteNavigationFile>:
				  Phantoms: []
				  TableOfContents:
				    - <SiteSectionRef>:
				      Title: "Blog"
				      ExternalUrl: "https://example.com/blog"
				      Children: []
				      DropdownLinks: []
				      FloatingKind: null
				      IsExternal: True
				      IsDropdown: False
				    - <SiteSectionRef>:
				      Title: "Empty"
				      ExternalUrl: null
				      Children: []
				      DropdownLinks: []
				      FloatingKind: null
				      IsExternal: False
				      IsDropdown: False
				"""
			);

	[Test]
	public void SectionDropdown_Loaded_Outcome() =>
		Load(
			"""
			toc:
			  - section: More
			    dropdown:
			      - title: One
			        url: https://example.com/1
			      - title: No url
			      - url: https://example.com/no-title
			      - title: Extra
			        url: https://example.com/3
			        icon: x
			      - plain
			"""
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			error /config/navigation.yml:10 Unknown key 'icon' in $.toc[0].dropdown[3]. Valid keys: title, url.
			model <SiteNavigationFile>:
			  Phantoms: []
			  TableOfContents:
			    - <SiteSectionRef>:
			      Title: "More"
			      ExternalUrl: null
			      Children: []
			      DropdownLinks:
			        - <SiteDropdownLinkRef>:
			          Title: "One"
			          Url: "https://example.com/1"
			        - <SiteDropdownLinkRef>:
			          Title: "Extra"
			          Url: "https://example.com/3"
			      FloatingKind: null
			      IsExternal: False
			      IsDropdown: True
			"""
			);

	[Test]
	public void SectionFloating_Loaded_Outcome() =>
		Load("toc:\n  - section: APIs\n    floating: api\n  - section: None\n    floating: ''\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			model <SiteNavigationFile>:
			  Phantoms: []
			  TableOfContents:
			    - <SiteSectionRef>:
			      Title: "APIs"
			      ExternalUrl: null
			      Children: []
			      DropdownLinks: []
			      FloatingKind: "api"
			      IsExternal: False
			      IsDropdown: False
			    - <SiteSectionRef>:
			      Title: "None"
			      ExternalUrl: null
			      Children: []
			      DropdownLinks: []
			      FloatingKind: null
			      IsExternal: False
			      IsDropdown: False
			"""
			);

	[Test]
	public void SectionAndTocInOneEntry_Loaded_SectionWins() =>
		Load("toc:\n  - section: S\n    toc: repo://a\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			diagnostics:
			model <SiteNavigationFile>:
			  Phantoms: []
			  TableOfContents:
			    - <SiteSectionRef>:
			      Title: "S"
			      ExternalUrl: null
			      Children: []
			      DropdownLinks: []
			      FloatingKind: null
			      IsExternal: False
			      IsDropdown: False
			"""
			);

	[Test]
	public void SectionWithoutTitle_Loaded_Outcome() =>
		Load("toc:\n  - section:\n    children: []\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		model <SiteNavigationFile>:
		  Phantoms: []
		  TableOfContents:
		    - <SiteSectionRef>:
		      Title: ""
		      ExternalUrl: null
		      Children: []
		      DropdownLinks: []
		      FloatingKind: null
		      IsExternal: False
		      IsDropdown: False
		"""
			);

	[Test]
	public void SectionUnknownKey_Loaded_Outcome() =>
		Load("toc:\n  - section: S\n    icon: x\n")
			.Should()
			.MatchInlineSnapshot(
				"""
		diagnostics:
		error /config/navigation.yml:3 Unknown key 'icon' in $.toc[0]. Valid keys: children, dropdown, external, floating, island, navigation_title, path_prefix, section, toc.
		model <SiteNavigationFile>:
		  Phantoms: []
		  TableOfContents:
		    - <SiteSectionRef>:
		      Title: "S"
		      ExternalUrl: null
		      Children: []
		      DropdownLinks: []
		      FloatingKind: null
		      IsExternal: False
		      IsDropdown: False
		"""
			);

	[Test]
	public void SectionChildrenWithIsland_Loaded_Outcome() =>
		Load("toc:\n  - section: S\n    island: true\n    children:\n      - toc: repo://a\n        island: true\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				diagnostics:
				model <SiteNavigationFile>:
				  Phantoms: []
				  TableOfContents:
				    - <SiteSectionRef>:
				      Title: "S"
				      ExternalUrl: null
				      Children:
				        - <SiteTableOfContentsRef>:
				          Source: repo://a/
				          PathPrefix: ""
				          Children: []
				          Island: True
				          NavigationTitle: null
				          PathRelativeToDocumentationSet: ""
				          PathRelativeToContainer: ""
				          Context: ""
				      DropdownLinks: []
				      FloatingKind: null
				      IsExternal: False
				      IsDropdown: False
				"""
			);

	[Test]
	public void DuplicatePathPrefixes_Validated_Outcome()
	{
		var file = Parse(
			"toc:\n  - toc: repo://a\n    path_prefix: same\n  - toc: repo://b\n    path_prefix: same/\n  - section: S\n    children:\n      - toc: repo://c\n        path_prefix: same\n"
		);
		var collector = new CapturingDiagnosticsCollector();
		var valid = SiteNavigationFile.ValidatePathPrefixes(collector, file, new MockFileSystem().FileInfo.New(NavigationPath));
		$"valid: {valid}\n{collector.Captured.ToSnapshotText()}".Should().MatchInlineSnapshot("valid: True");
	}

	// which of two duplicates is reported as the original follows the iteration order of a hash set of Uris, which changes
	// from one process to the next; only the verdict and the number of errors are stable
	[Test]
	public void PathPrefixesDifferingByCaseAndScheme_Validated_Outcome()
	{
		var file = Parse(
			"toc:\n  - toc: repo://a\n    path_prefix: Same\n  - toc: other://b\n    path_prefix: same\n  - toc: repo://c\n    path_prefix: ''\n  - toc: repo://d\n"
		);
		var collector = new CapturingDiagnosticsCollector();
		var valid = SiteNavigationFile.ValidatePathPrefixes(collector, file, new MockFileSystem().FileInfo.New(NavigationPath));
		$"valid: {valid}\nerrors: {collector.Captured.Count}".Should().MatchInlineSnapshot(
			"""
			valid: False
			errors: 1
			"""
		);
	}

	[Test]
	public void DeclaredSourcesOfASmallFile_Collected_Outcome()
	{
		var file = Parse(
			"phantoms:\n  - toc: x\ntoc:\n  - toc: repo://a\n    children:\n      - toc: repo://b\n  - section: S\n    children:\n      - toc: repo://c\n        children:\n          - toc: repo://a\n"
		);
		var sources = string.Join(
			'\n',
			SiteNavigationFile.GetAllDeclaredSources(file).Select(u => u.ToString()).Order(StringComparer.Ordinal)
		);
		var phantoms = string.Join('\n', SiteNavigationFile.GetPhantomPrefixes(file).Select(u => u.ToString()));
		$"{sources}\n--\n{phantoms}".Should().MatchInlineSnapshot(
			"""
			repo://a/
			repo://b/
			repo://c/
			--
			docs-content://x/
			"""
		);
	}
}
