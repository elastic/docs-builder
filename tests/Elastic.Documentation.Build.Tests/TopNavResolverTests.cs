// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Assembler.Links;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;

namespace Elastic.Documentation.Build.Tests;

public class TopNavResolverTests
{
	private static readonly IFileInfo NavigationFile =
		new MockFileSystem().FileInfo.New("/config/navigation.yml");

	[Fact]
	public void RelativeUrlGainsThePathPrefix()
	{
		var model = Resolve("""
		                    top_nav:
		                      - title: Reference
		                        url: /reference/
		                    """, out var collector);

		collector.Errors.Should().Be(0);
		model.Should().NotBeNull();
		var link = model.Items.Should().ContainSingle().Which.Should().BeOfType<TopNavLinkItem>().Subject;
		link.Title.Should().Be("Reference");
		link.Url.Should().Be("/docs/reference/");
		link.IsExternal.Should().BeFalse();
	}

	[Fact]
	public void AbsoluteUrlIsMarkedExternalAndLeftUntouched()
	{
		var model = Resolve("""
		                    top_nav:
		                      - title: APIs
		                        url: https://www.elastic.co/docs/api/
		                    """, out var collector);

		collector.Errors.Should().Be(0);
		var link = model!.Items.Should().ContainSingle().Which.Should().BeOfType<TopNavLinkItem>().Subject;
		link.IsExternal.Should().BeTrue();
		link.Url.Should().Be("https://www.elastic.co/docs/api/");
	}

	[Fact]
	public void PageReferenceResolvesThroughTheCrossLinkIndex()
	{
		var model = Resolve("""
		                    top_nav:
		                      - title: Products
		                        children:
		                          - title: Stack products
		                            children:
		                              - title: Elasticsearch
		                                page: docs-content://products/elasticsearch.md
		                    """, out var collector);

		collector.Errors.Should().Be(0);
		var dropdown = model!.Items.Should().ContainSingle().Which.Should().BeOfType<TopNavDropdownItem>().Subject;
		dropdown.Title.Should().Be("Products");
		var group = dropdown.Groups.Should().ContainSingle().Which;
		group.Label.Should().Be("Stack products");
		group.Links.Should().ContainSingle().Which.Url.Should().Be("/docs/products/elasticsearch/");
	}

	[Fact]
	public void ChildlessChildrenAreCollectedIntoAnUnlabelledGroup()
	{
		var model = Resolve("""
		                    top_nav:
		                      - title: Products
		                        children:
		                          - title: All products
		                            url: /products/
		                    """, out var collector);

		collector.Errors.Should().Be(0);
		var dropdown = model!.Items.Should().ContainSingle().Which.Should().BeOfType<TopNavDropdownItem>().Subject;
		var group = dropdown.Groups.Should().ContainSingle().Which;
		group.Label.Should().BeNull();
		group.Links.Should().ContainSingle().Which.Url.Should().Be("/docs/products/");
	}

	[Fact]
	public void UnresolvablePageEmitsAnErrorAndDropsTheEntry()
	{
		var model = Resolve("""
		                    top_nav:
		                      - title: Missing
		                        page: docs-content://does/not/exist.md
		                      - title: Reference
		                        url: /reference/
		                    """, out var collector);

		collector.Errors.Should().Be(1);
		model!.Items.Should().ContainSingle().Which.Title.Should().Be("Reference");
	}

	[Fact]
	public void SettingBothUrlAndPageIsAnError()
	{
		var model = Resolve("""
		                    top_nav:
		                      - title: Confused
		                        url: /reference/
		                        page: docs-content://products/elasticsearch.md
		                    """, out var collector);

		collector.Errors.Should().Be(1);
		model.Should().BeNull();
	}

	[Fact]
	public void EntryWithoutUrlPageOrChildrenIsAnError()
	{
		_ = Resolve("""
		            top_nav:
		              - title: Dangling
		            """, out var collector);

		collector.Errors.Should().Be(1);
	}

	[Fact]
	public void NestingBeyondOneGroupLevelIsAnError()
	{
		_ = Resolve("""
		            top_nav:
		              - title: Products
		                children:
		                  - title: Stack products
		                    children:
		                      - title: Too deep
		                        children:
		                          - title: Way too deep
		                            url: /x/
		            """, out var collector);

		collector.Errors.Should().Be(1);
	}

	[Fact]
	public void AbsentTopNavResolvesToNull()
	{
		var model = Resolve("""
		                    toc:
		                      - toc: docs-content://products
		                        path_prefix: products
		                    """, out var collector);

		collector.Errors.Should().Be(0);
		model.Should().BeNull();
	}

	[Fact]
	public void ActiveUrlPrefersTheLongestWholeSegmentMatch()
	{
		var model = Resolve("""
		                    top_nav:
		                      - title: Reference
		                        url: /reference/
		                      - title: Reference APIs
		                        url: /reference/apis/
		                      - title: External
		                        url: https://example.com/reference/apis/
		                    """, out _);

		model!.ActiveUrl("/docs/reference/apis/search").Should().Be("/docs/reference/apis/");
		model.ActiveUrl("/docs/reference/other").Should().Be("/docs/reference/");
		// a sibling path that merely shares a textual prefix must not match
		model.ActiveUrl("/docs/references/other").Should().BeNull();
		model.ActiveUrl(null).Should().BeNull();
	}

	private static TopNavRenderModel? Resolve(string yaml, out DiagnosticsCollector collector)
	{
		collector = new TestDiagnosticsCollector();
		_ = collector.StartAsync(TestContext.Current.CancellationToken);

		var navigationFile = SiteNavigationFile.Deserialize(yaml);
		var environment = new PublishEnvironment
		{
			Name = "test",
			Uri = "https://www.elastic.co",
			PathPrefix = "docs"
		};
		var mappings = new Dictionary<Uri, NavigationTocMapping>
		{
			[new Uri("docs-content://products")] = new()
			{
				Source = new Uri("docs-content://products"),
				SourcePathPrefix = "products"
			}
		}.ToFrozenDictionary();

		var crossLinks = new FetchedCrossLinks
		{
			DeclaredRepositories = ["docs-content"],
			LinkIndexEntries = FrozenDictionary<string, LinkRegistryEntry>.Empty,
			LinkReferences = new Dictionary<string, RepositoryLinks>
			{
				["docs-content"] = new()
				{
					Origin = GitCheckoutInformation.Unavailable,
					UrlPathPrefix = null,
					CrossLinks = [],
					Links = new Dictionary<string, LinkMetadata>
					{
						["products/elasticsearch.md"] = new()
						{
							Anchors = null,
							Hidden = false
						}
					}
				}
			}.ToFrozenDictionary()
		};

		var resolver = new CrossLinkResolver(crossLinks, new PublishEnvironmentUriResolver(mappings, environment));
		return TopNavResolver.Resolve(navigationFile, resolver, environment.PathPrefix, collector, NavigationFile);
	}

	private sealed class TestDiagnosticsCollector() : DiagnosticsCollector([]);
}
