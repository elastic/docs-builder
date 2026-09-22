// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Markdown.Myst.Directives.Changelog;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class ChangelogBasicTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogBasicTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:::
		"""
		) =>
		// Create the default bundles folder with a test bundle
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			  lifecycle: ga
			entries:
			- title: Add new feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  areas:
			  - Search
			  prs:
			  - "123456"
			  description: This is a great new feature.
			- title: Fix important bug
			  type: bug-fix
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  areas:
			  - Indexing
			  prs:
			  - "123457"
			"""
			)
		);

	[Test]
	public void ParsesChangelogBlock() => Block.Should().NotBeNull();

	[Test]
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be("changelog");

	[Test]
	public void FindsBundlesFolder() => Block!.Found.Should().BeTrue();

	[Test]
	public void SetsCorrectBundlesFolderPath() =>
		Block!.BundlesFolderPath.Should().EndWith("changelog/bundles".Replace('/', Path.DirectorySeparatorChar));

	[Test]
	public void LoadsBundles() => Block!.LoadedBundles.Should().HaveCount(1);

	[Test]
	public void RendersMarkdownContent()
	{
		Html.Should().Contain("9.3.0");
		Html.Should().Contain("Features and enhancements");
		Html.Should().Contain("Add new feature");
		Html.Should().Contain("Fixes");
		Html.Should().Contain("Fix important bug");
	}
}

[InheritsTests]
public class ChangelogExcludeAmendTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogExcludeAmendTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:::
		"""
		)
	{
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			  lifecycle: ga
			entries:
			- title: Keep this feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  file:
			    name: keep.yaml
			    checksum: keep-checksum
			  prs:
			  - "123456"
			- title: Remove this feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  file:
			    name: removed.yaml
			    checksum: excluded
			  prs:
			  - "123457"
			"""
			)
		);
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.amend-1.yaml",
			new MockFileData(
				// language=yaml
				"""
			exclude-entries:
			- file:
			    name: removed.yaml
			    checksum: excluded
			"""
			)
		);
	}

	[Test]
	public void RendersWithoutExcludedEntry()
	{
		Html.Should().Contain("Keep this feature");
		Html.Should().NotContain("Remove this feature");
	}

	[Test]
	public void LoadsMergedEntryCount()
	{
		Block!.LoadedBundles.Should().HaveCount(1);
		Block.LoadedBundles[0].Entries.Should().HaveCount(1);
		Block.LoadedBundles[0].Entries[0].Title.Should().Be("Keep this feature");
	}
}

[InheritsTests]
public class ChangelogMultipleBundlesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogMultipleBundlesTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:::
		"""
		)
	{
		// Create multiple bundles with different versions
		FileSystem.AddFile(
			"docs/changelog/bundles/9.2.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.2.0
			entries:
			- title: Feature in 9.2.0
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.2.0
			  prs:
			  - "111111"
			"""
			)
		);

		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Feature in 9.3.0
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "222222"
			"""
			)
		);

		FileSystem.AddFile(
			"docs/changelog/bundles/9.10.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.10.0
			entries:
			- title: Feature in 9.10.0
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.10.0
			  prs:
			  - "333333"
			"""
			)
		);
	}

	[Test]
	public void LoadsBundles() => Block!.LoadedBundles.Should().HaveCount(3);

	[Test]
	public void RendersInSemverOrder()
	{
		// Should be sorted by semver descending: 9.10.0 > 9.3.0 > 9.2.0
		var idx910 = Html.IndexOf("9.10.0", StringComparison.Ordinal);
		var idx93 = Html.IndexOf("9.3.0", StringComparison.Ordinal);
		var idx92 = Html.IndexOf("9.2.0", StringComparison.Ordinal);

		idx910.Should().BeLessThan(idx93, "9.10.0 should appear before 9.3.0");
		idx93.Should().BeLessThan(idx92, "9.3.0 should appear before 9.2.0");
	}

	[Test]
	public void RendersAllVersions()
	{
		Html.Should().Contain("9.10.0");
		Html.Should().Contain("9.3.0");
		Html.Should().Contain("9.2.0");
	}
}

/// <summary>
/// Verifies the <c>:version:</c> option filters local-folder bundles down to the single matching
/// target, leaving the others out of both the loaded set and the rendered output.
/// </summary>
[InheritsTests]
public class ChangelogVersionFilterTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogVersionFilterTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:version: 9.3.0
		:::
		"""
		)
	{
		FileSystem.AddFile(
			"docs/changelog/bundles/9.2.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.2.0
			entries:
			- title: Feature in 9.2.0
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.2.0
			  prs:
			  - "111111"
			"""
			)
		);

		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Feature in 9.3.0
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "222222"
			"""
			)
		);
	}

	[Test]
	public void CapturesVersionOption() => Block!.VersionFilter.Should().Be("9.3.0");

	[Test]
	public void LoadsOnlyMatchingBundle() => Block!.LoadedBundles.Should().ContainSingle().Which.Version.Should().Be("9.3.0");

	[Test]
	public void RendersOnlyMatchingVersion()
	{
		Html.Should().Contain("Feature in 9.3.0");
		Html.Should().NotContain("Feature in 9.2.0");
	}
}

/// <summary>
/// Verifies a <c>:version:</c> value that matches no bundle renders nothing and warns instead of
/// silently falling back to all versions.
/// </summary>
[InheritsTests]
public class ChangelogVersionFilterNoMatchTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogVersionFilterNoMatchTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:version: 1.2.3
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Feature in 9.3.0
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "222222"
		"""
			)
		);

	[Test]
	public void LoadsNoBundles() => Block!.LoadedBundles.Should().BeEmpty();

	[Test]
	public void EmitsWarningForUnmatchedVersion() =>
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("No changelog bundle matches :version:"));
}

[InheritsTests]
public class ChangelogCustomPathTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogCustomPathTests() : base(
			// language=markdown
			"""
		:::{changelog} /release-notes/bundles
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/release-notes/bundles/1.0.0.yaml",
			new MockFileData(
				// language=yaml
				"""
		products:
		- product: my-product
		  target: 1.0.0
		entries:
		- title: First release
		  type: feature
		  products:
		  - product: my-product
		    target: 1.0.0
		  prs:
		  - "1"
		"""
			)
		);

	[Test]
	public void FindsBundlesFolder() => Block!.Found.Should().BeTrue();

	[Test]
	public void SetsCorrectBundlesFolderPath() =>
		Block!.BundlesFolderPath.Should().EndWith("release-notes/bundles".Replace('/', Path.DirectorySeparatorChar));

	[Test]
	public void RendersContent()
	{
		Html.Should().Contain("1.0.0");
		Html.Should().Contain("First release");
	}
}

/// <summary>
/// Verifies <c>:cdn:</c> product validation. An invalid product name is rejected before it is
/// assigned to the block and before any network access, so this test exercises the wiring without
/// touching the CDN.
/// </summary>
[InheritsTests]
public class ChangelogCdnInvalidProductTests() : DirectiveTest<ChangelogBlock>(
	// language=markdown
	"""
	:::{changelog}
	:cdn: invalid$product
	:::
	"""
)
{
	[Test]
	public void DoesNotCaptureInvalidCdnProduct() => Block!.CdnProduct.Should().BeNull();

	[Test]
	public void DoesNotSourceFromLocalFolder() => Block!.BundlesFolderPath.Should().BeNull();

	[Test]
	public void EmitsErrorForInvalidProduct()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Invalid :cdn: product"));
	}
}

/// <summary>
/// Verifies CDN-sourced bundles render into the page body (not just the page TOC). The directive is a
/// selector over release notes prefetched at startup, so the test injects a resolver holding the bundle
/// instead of hitting the network. Regression guard: the HTML renderer previously gated on the
/// (CDN-null) local bundles folder path and silently emitted an empty body.
/// </summary>
[InheritsTests]
public class ChangelogCdnRenderTests() : DirectiveTest<ChangelogBlock>(
	// language=markdown
	"""
	:::{changelog}
	:cdn: cdn-render-test
	:::
	"""
)
{
	private const string Product = "cdn-render-test";

	protected override IReleaseNotesResolver GetReleaseNotesResolver() =>
		ChangelogCdnTestResolver.For(
			Product,
			("9.4.0.yaml",
			// language=yaml
			"""
				products:
				- product: cdn-render-test
				  target: 9.4.0
				  repo: elasticsearch
				  owner: elastic
				entries:
				- title: Faster vector search on the CDN
				  type: enhancement
				  products:
				  - product: cdn-render-test
				    target: 9.4.0
				  prs:
				  - "999"
				""")
		);

	[Test]
	public void FoundFromCdn() => Block!.Found.Should().BeTrue();

	[Test]
	public void RendersCdnBundleBody()
	{
		Html.Should().Contain("9.4.0");
		Html.Should().Contain("Features and enhancements");
		Html.Should().Contain("Faster vector search on the CDN");
	}
}

/// <summary>
/// Verifies <c>:cdn:</c> combined with <c>:version:</c> renders only the matching prefetched bundle.
/// Version filtering is applied to the injected resolver's bundles, so no network access occurs.
/// </summary>
[InheritsTests]
public class ChangelogCdnVersionFilterTests() : DirectiveTest<ChangelogBlock>(
	// language=markdown
	"""
	:::{changelog}
	:cdn: cdn-version-test
	:version: 9.4.0
	:::
	"""
)
{
	private const string Product = "cdn-version-test";

	protected override IReleaseNotesResolver GetReleaseNotesResolver() =>
		ChangelogCdnTestResolver.For(
			Product,
			("9.4.0.yaml",
			// language=yaml
			"""
				products:
				- product: cdn-version-test
				  target: 9.4.0
				  repo: elasticsearch
				  owner: elastic
				entries:
				- title: Selected version entry
				  type: enhancement
				  products:
				  - product: cdn-version-test
				    target: 9.4.0
				  prs:
				  - "999"
				"""),
			("9.3.0.yaml",
			// language=yaml
			"""
				products:
				- product: cdn-version-test
				  target: 9.3.0
				  repo: elasticsearch
				  owner: elastic
				entries:
				- title: Filtered out entry
				  type: enhancement
				  products:
				  - product: cdn-version-test
				    target: 9.3.0
				  prs:
				  - "998"
				""")
		);

	[Test]
	public void CapturesVersionFilter() => Block!.VersionFilter.Should().Be("9.4.0");

	[Test]
	public void RendersOnlyMatchingVersion()
	{
		Block!.Found.Should().BeTrue();
		Html.Should().Contain("Selected version entry");
		Html.Should().NotContain("Filtered out entry");
	}
}

/// <summary>
/// Verifies a valueless <c>:cdn:</c> infers the product from the current repository name. With a
/// <c>.git</c> marker present the mock git checkout reports the repository as <c>docs-builder</c>, so
/// the directive selects that product from the injected resolver.
/// </summary>
[InheritsTests]
public class ChangelogCdnInferredProductTests() : DirectiveTest<ChangelogBlock>(
	// language=markdown
	"""
	:::{changelog}
	:cdn:
	:::
	"""
)
{
	private const string InferredProduct = "docs-builder";

	// A .git marker makes FindGitRoot resolve a checkout directory, which lets the mock-aware
	// GitCheckoutInformationFactory report the repository name (docs-builder) for inference.
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddDirectory(Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".git"));

	protected override IReleaseNotesResolver GetReleaseNotesResolver() =>
		ChangelogCdnTestResolver.For(
			InferredProduct,
			("9.4.0.yaml",
			// language=yaml
			"""
				products:
				- product: docs-builder
				  target: 9.4.0
				  repo: docs-builder
				  owner: elastic
				entries:
				- title: Inferred product from the repository
				  type: enhancement
				  products:
				  - product: docs-builder
				    target: 9.4.0
				  prs:
				  - "999"
				""")
		);

	[Test]
	public void InfersProductFromRepository() => Block!.CdnProduct.Should().Be(InferredProduct);

	[Test]
	public void RendersInferredCdnBundleBody()
	{
		Block!.Found.Should().BeTrue();
		Html.Should().Contain("Inferred product from the repository");
	}
}

/// <summary>
/// A valueless <c>:cdn:</c> must fail with a clear error when the product cannot be inferred (no git
/// information available), rather than silently rendering empty.
/// </summary>
[InheritsTests]
public class ChangelogCdnInferredProductUnavailableTests() : DirectiveTest<ChangelogBlock>(
	// language=markdown
	"""
	:::{changelog}
	:cdn:
	:::
	"""
)
{
	// Force Unavailable so InferCdnProductFromRepository() returns null — the "could not be inferred" path.
	protected override GitCheckoutInformation? GetGitCheckoutInformation() => GitCheckoutInformation.Unavailable;

	[Test]
	public void EmitsErrorWhenProductCannotBeInferred()
	{
		Block!.Found.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("could not be inferred"));
	}
}

/// <summary>
/// A <c>:cdn:</c> product that is not declared under <c>release_notes</c> in docset.yml must fail with a
/// clear error (the bundles were never prefetched), pointing the author at the declaration to add.
/// </summary>
[InheritsTests]
public class ChangelogCdnUndeclaredProductTests() : DirectiveTest<ChangelogBlock>(
	// language=markdown
	"""
	:::{changelog}
	:cdn: not-declared
	:::
	"""
)
{
	[Test]
	public void EmitsErrorWhenProductIsNotDeclared()
	{
		Block!.Found.Should().BeFalse();
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Message.Contains("not declared in docset.yml") && d.Message.Contains("release_notes"));
	}
}

/// <summary>
/// Test helper that builds an <see cref="IReleaseNotesResolver"/> backed by in-memory bundle content,
/// standing in for the startup CDN prefetch.
/// </summary>
internal static class ChangelogCdnTestResolver
{
	public static IReleaseNotesResolver For(string product, params (string FileName, string Content)[] bundleContents)
	{
		var bundles = new BundleLoader(new MockFileSystem()).LoadBundlesFromContent(bundleContents, _ => { });
		return new ReleaseNotesResolver(new FetchedReleaseNotes
		{
			BundlesByProduct = new Dictionary<string, IReadOnlyList<LoadedBundle>>(StringComparer.Ordinal)
			{
				[product] = bundles
			}.ToFrozenDictionary(StringComparer.Ordinal),
			DeclaredProducts = new[] { product }.ToFrozenSet(StringComparer.Ordinal)
		});
	}
}

[InheritsTests]
public class ChangelogNotFoundTests() : DirectiveTest<ChangelogBlock>(
	// language=markdown
	"""
	:::{changelog} /missing-bundles
	:::
	"""
)
{
	[Test]
	public void ReportsFolderNotFound() => Block!.Found.Should().BeFalse();

	[Test]
	public void EmitsErrorForMissingFolder()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.Contains("does not exist"));
	}
}

[InheritsTests]
public class ChangelogDefaultPathMissingTests() : DirectiveTest<ChangelogBlock>(
	// language=markdown
	"""
	:::{changelog}
	:::
	"""
)
{
	[Test]
	public void EmitsErrorForMissingDefaultFolder()
	{
		// No bundles folder created, so it should emit an error
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.Contains("does not exist"));
	}
}

/// <summary>
/// Tests for breaking changes rendering.
/// Breaking changes should always render on the page when using :type: all.
/// </summary>
[InheritsTests]
public class ChangelogWithBreakingChangesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogWithBreakingChangesTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: all
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Breaking change in API
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: The API has changed significantly.
		  impact: Users must update their code.
		  action: Follow the migration guide.
		  prs:
		  - "222222"
		"""
			)
		);

	[Test]
	public void RendersBreakingChangesSection()
	{
		Html.Should().Contain("Breaking changes");
		Html.Should().Contain("Breaking change in API");
	}

	[Test]
	public void RendersImpactAndAction()
	{
		Html.Should().Contain("Impact");
		Html.Should().Contain("Users must update their code");
		Html.Should().Contain("Action");
		Html.Should().Contain("Follow the migration guide");
	}
}

/// <summary>
/// Tests for deprecations rendering.
/// Deprecations should always render on the page when using :type: all.
/// </summary>
[InheritsTests]
public class ChangelogWithDeprecationsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogWithDeprecationsTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: all
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Deprecated old API
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: The old API is deprecated.
		  impact: The API will be removed in a future version.
		  action: Use the new API instead.
		  prs:
		  - "333333"
		"""
			)
		);

	[Test]
	public void RendersDeprecationsSection()
	{
		Html.Should().Contain("Deprecations");
		Html.Should().Contain("Deprecated old API");
	}
}

[InheritsTests]
public class ChangelogEmptyBundleTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogEmptyBundleTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries: []
		"""
			)
		);

	[Test]
	public void OmitsEmptyVersionBlock()
	{
		Html.Should().NotContain("No new features, enhancements, or fixes");
		Html.Should().NotContain("9.3.0");
	}
}

[InheritsTests]
public class ChangelogEmptyFolderTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogEmptyFolderTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:::
		"""
		) =>
		// Create the folder but don't add any YAML files
		FileSystem.AddDirectory("docs/changelog/bundles");

	[Test]
	public void ReportsFolderEmpty() => Block!.Found.Should().BeFalse();

	[Test]
	public void EmitsErrorForEmptyFolder()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.Contains("contains no YAML files"));
	}
}

[InheritsTests]
public class ChangelogAbsolutePathTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogAbsolutePathTests() : base(
			// language=markdown
			"""
		:::{changelog} /release-notes/bundles
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/release-notes/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Test feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "444444"
		"""
			)
		);

	[Test]
	public void FindsBundlesFolderWithAbsolutePath() => Block!.Found.Should().BeTrue();

	[Test]
	public void SetsCorrectBundlesFolderPath() => Block!.BundlesFolderPath.Should().Contain("release-notes");
}

/// <summary>
/// Tests the section order - critical types (breaking changes, security, known issues, deprecations)
/// should appear BEFORE features/fixes when using :type: all.
/// </summary>
[InheritsTests]
public class ChangelogSectionOrderTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogSectionOrderTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: all
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: New feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		- title: Security fix
		  type: security
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "222222"
		- title: Breaking API change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: API changed.
		  impact: Users must update.
		  action: Follow guide.
		  prs:
		  - "333333"
		- title: Known issue
		  type: known-issue
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Issue exists.
		  impact: Some impact.
		  action: Workaround available.
		  prs:
		  - "444444"
		- title: Deprecated feature
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Feature deprecated.
		  impact: Will be removed.
		  action: Use new feature.
		  prs:
		  - "555555"
		- title: Bug fix
		  type: bug-fix
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "666666"
		"""
			)
		);

	[Test]
	public void BreakingChangesAppearsFirst()
	{
		var breakingIdx = Html.IndexOf("Breaking changes", StringComparison.Ordinal);
		var featuresIdx = Html.IndexOf("Features and enhancements", StringComparison.Ordinal);
		var fixesIdx = Html.IndexOf(">Fixes<", StringComparison.Ordinal);

		breakingIdx.Should().BeLessThan(featuresIdx, "Breaking changes should appear before Features");
		breakingIdx.Should().BeLessThan(fixesIdx, "Breaking changes should appear before Fixes");
	}

	[Test]
	public void SecurityAppearsBeforeFeatures()
	{
		var securityIdx = Html.IndexOf(">Security<", StringComparison.Ordinal);
		var featuresIdx = Html.IndexOf("Features and enhancements", StringComparison.Ordinal);

		securityIdx.Should().BeLessThan(featuresIdx, "Security should appear before Features");
	}

	[Test]
	public void KnownIssuesAppearsBeforeFeatures()
	{
		var knownIssuesIdx = Html.IndexOf("Known issues", StringComparison.Ordinal);
		var featuresIdx = Html.IndexOf("Features and enhancements", StringComparison.Ordinal);

		knownIssuesIdx.Should().BeLessThan(featuresIdx, "Known issues should appear before Features");
	}

	[Test]
	public void DeprecationsAppearsBeforeFeatures()
	{
		var deprecationsIdx = Html.IndexOf("Deprecations", StringComparison.Ordinal);
		var featuresIdx = Html.IndexOf("Features and enhancements", StringComparison.Ordinal);

		deprecationsIdx.Should().BeLessThan(featuresIdx, "Deprecations should appear before Features");
	}
}

/// <summary>
/// Tests header levels: ## (h2) for versions, ### (h3) for sections.
/// </summary>
[InheritsTests]
public class ChangelogHeaderLevelsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHeaderLevelsTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: New feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		- title: Bug fix
		  type: bug-fix
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "222222"
		"""
			)
		);

	[Test]
	public void VersionHeaderIsH2()
	{
		// Version should be h2
		Html.Should().Contain("<h2");
		Html.Should().Contain("9.3.0");
	}

	[Test]
	public void OnlyOneH2ForVersion()
	{
		// Only one h2 for the version header
		var h2Count = CountOccurrences(Html, "<h2");
		h2Count.Should().Be(1, "Should have exactly one h2 for the version");
	}

	[Test]
	public void SectionHeadersAreH3()
	{
		// Section headers should be h3 (children of version)
		Html.Should().Contain("<h3");
		// Should have h3 for features + fixes = 2
		var h3Count = CountOccurrences(Html, "<h3");
		h3Count.Should().Be(2, "Should have h3 for each section (features, fixes)");
	}

	private static int CountOccurrences(string text, string pattern)
	{
		var count = 0;
		var index = 0;
		while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
		{
			count++;
			index += pattern.Length;
		}
		return count;
	}
}

/// <summary>
/// Verifies that when a changelog entry has both a title and a description,
/// the rendered output does not concatenate them without a separator.
/// Regression test for: "allowlist.This PR introduces..." (no space between title and description).
/// </summary>
[InheritsTests]
public class ChangelogTitleDescriptionSpacingTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTitleDescriptionSpacingTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:description-visibility: keep-descriptions
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Added missing banner-related Kibana settings to the settings allowlist
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: This PR introduces the following settings.
		"""
			)
		);

	[Test]
	public void RendersTitleText() => Html.Should().Contain("Added missing banner-related Kibana settings to the settings allowlist");

	[Test]
	public void RendersDescriptionText() => Html.Should().Contain("This PR introduces the following settings");

	[Test]
	public void DoesNotConcatenateTitleAndDescriptionWithoutSeparator() => Html.Should().NotContain("allowlist.This PR introduces");
}

/// <summary>
/// Verifies that when a bundle has a release-date field, it is rendered in the output.
/// </summary>
[InheritsTests]
public class ChangelogReleaseDateTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogReleaseDateTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:release-dates:
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/1.34.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: apm-agent-dotnet
			  target: 1.34.0
			release-date: "2026-04-09"
			entries:
			- title: Add tracing improvements
			  type: feature
			  products:
			  - product: apm-agent-dotnet
			    target: 1.34.0
			  prs:
			  - "500"
			"""
			)
		);

	[Test]
	public void RendersReleaseDate() => Html.Should().Contain("Released: April 9, 2026");

	[Test]
	public void RendersEntries() => Html.Should().Contain("Add tracing improvements");
}

/// <summary>
/// Verifies that when a bundle has no release-date field, no "Released:" text appears.
/// </summary>
[InheritsTests]
public class ChangelogNoReleaseDateTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogNoReleaseDateTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: New feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "100"
		"""
			)
		);

	[Test]
	public void DoesNotRenderReleaseDate() => Html.Should().NotContain("Released:");
}

/// <summary>
/// Verifies that both release-date and description render together.
/// </summary>
[InheritsTests]
public class ChangelogReleaseDateWithDescriptionTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogReleaseDateWithDescriptionTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:release-dates:
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/1.34.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: apm-agent-dotnet
			  target: 1.34.0
			release-date: "2026-04-09"
			description: |
			  This release includes tracing improvements and bug fixes.
			entries:
			- title: Add tracing improvements
			  type: feature
			  products:
			  - product: apm-agent-dotnet
			    target: 1.34.0
			  prs:
			  - "500"
			"""
			)
		);

	[Test]
	public void RendersReleaseDate() => Html.Should().Contain("Released: April 9, 2026");

	[Test]
	public void RendersDescription() => Html.Should().Contain("This release includes tracing improvements and bug fixes.");

	[Test]
	public void RendersEntries() => Html.Should().Contain("Add tracing improvements");
}
