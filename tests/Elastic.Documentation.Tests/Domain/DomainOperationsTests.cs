// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.Versions;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>The pure operations of the aggregates outside release notes: valid input, a violation the derivation causes, and the edges.</summary>
public class DomainOperationsTests
{
	private static readonly DateTime Published = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	private static SiteTableOfContentsRef Toc(string source, string prefix, params SiteTableOfContentsRef[] children) =>
		new(new Uri(source), prefix, children);

	private static SiteNavigationEntry Mount(SiteTableOfContentsRef toc) => new(toc);

	private static SiteNavigationEntry Section(string title, params SiteTableOfContentsRef[] children) =>
		new(new SiteSectionRef(title, null, children, []));

	// ---- SiteNavigationDefinition.WithoutRepositories ----

	[Test]
	public void WithoutRepositories_NothingToRemove_ChecksAndReturnsTheNavigationItself()
	{
		var navigation = new SiteNavigationDefinition { TableOfContents = [Mount(Toc("docs-content://a", "a"))] };

		var result = navigation.WithoutRepositories(new HashSet<string>());

		result.Value.Should().BeSameAs(navigation);
		result.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public void WithoutRepositories_PrivateMount_PromotesItsChildrenAndDropsPhantoms()
	{
		var navigation = new SiteNavigationDefinition
		{
			Phantoms = [new PhantomRegistration { Source = "private-a://hidden" }, new PhantomRegistration { Source = "public://visible" }],
			TableOfContents =
			[
				Mount(Toc("private-a://top", "top", Toc("public://promoted", "promoted"))),
				Mount(Toc("public://after", "after"))
			]
		};

		var result = navigation.WithoutRepositories(new HashSet<string> { "private-a" });

		result.HasErrors.Should().BeFalse();
		result.Value!.Phantoms.Select(p => p.Source).Should().Equal("public://visible");
		result.Value.TableOfContents.OfCase<SiteTableOfContentsRef>().Select(t => t.PathPrefix).Should().Equal("promoted", "after");
		navigation.TableOfContents.Should().HaveCount(2, "the navigation filtered is not modified");
	}

	[Test]
	public void WithoutRepositories_SectionWhoseOnlyMountWasPrivate_IsReportedByTheRecheck()
	{
		var navigation = new SiteNavigationDefinition
		{
			TableOfContents = [Mount(Toc("docs-content://a", "a")), Section("Private", Toc("private-a://only", "only"))]
		};

		var result = navigation.WithoutRepositories(new HashSet<string> { "private-a" }, file: "navigation.yml");

		result.Value.Should().NotBeNull();
		result.Diagnostics.Should().ContainSingle(d => d.RuleId == "section-not-empty" && d.File == "navigation.yml");
	}

	[Test]
	public void WithoutRepositories_Appended_AreMountedAfterTheRestAndChecked()
	{
		var navigation = new SiteNavigationDefinition { TableOfContents = [Mount(Toc("docs-content://a", "same"))] };

		var result = navigation.WithoutRepositories(new HashSet<string>(), [Mount(Toc("other://b", "same"))]);

		result.Value!.TableOfContents.OfCase<SiteTableOfContentsRef>().Select(t => t.Source.Scheme).Should().Equal("docs-content", "other");
		result.HasErrors.Should().BeTrue("two repositories now publish under the same path prefix");
		result.Diagnostics.Should().Contain(d => d.RuleId == "path-prefixes-unique");
	}

	// ---- LinkRegistry.WithLinkRegistryEntry / LinkRegistryEntry.FromS3Key ----

	private static LinkRegistryEntry Entry(string repository, string branch, DateTime? updatedAt = null) =>
		LinkRegistryEntry.FromS3Key($"elastic/{repository}/{branch}/links.json", "etag", updatedAt ?? Published, "abc123");

	private static LinkRegistry Registry() =>
		new()
		{
			Repositories = new Dictionary<string, Dictionary<string, LinkRegistryEntry>>
			{
				["docs-content"] = new() { ["main"] = Entry("docs-content", "main") },
				["kibana"] = new() { ["main"] = Entry("kibana", "main") }
			}
		};

	[Test]
	public void FromS3Key_ReadsRepositoryAndBranchFromTheKey()
	{
		var entry = LinkRegistryEntry.FromS3Key("elastic/kibana/8.x/links.json", "\"e\"", Published, "refs/heads/8.x");

		entry.Repository.Should().Be("kibana");
		entry.Branch.Should().Be("8.x");
		entry.Path.Should().Be("elastic/kibana/8.x/links.json");
		entry.ETag.Should().Be("\"e\"");
		entry.UpdatedAt.Should().Be(Published);
		entry.GitReference.Should().Be("refs/heads/8.x");
	}

	[Test]
	public void FromS3Key_KeyWithoutBranchSegment_Throws()
	{
		var make = () => LinkRegistryEntry.FromS3Key("elastic/kibana", "e", Published, "r");

		_ = make.Should().Throw<ArgumentException>();
	}

	[Test]
	public void WithLinkRegistryEntry_NewerEntry_ReplacesAndDoesNotModifyTheOriginal()
	{
		var before = Registry();

		var result = before.WithLinkRegistryEntry(Entry("kibana", "main", Published.AddDays(1)));

		result.Diagnostics.Should().BeEmpty();
		result.Value!.Repositories["kibana"]["main"].UpdatedAt.Should().Be(Published.AddDays(1));
		before.Repositories["kibana"]["main"].UpdatedAt.Should().Be(Published, "the registry it was derived from is not modified");
	}

	[Test]
	public void WithLinkRegistryEntry_OlderEntry_KeepsTheExistingOne()
	{
		var result = Registry().WithLinkRegistryEntry(Entry("kibana", "main", Published.AddDays(-1)));

		result.Value!.Repositories["kibana"]["main"].UpdatedAt.Should().Be(Published);
	}

	[Test]
	public void WithLinkRegistryEntry_NewBranchOfAKnownRepository_IsAdded()
	{
		var result = Registry().WithLinkRegistryEntry(Entry("kibana", "8.x"));

		result.Value!.Repositories["kibana"].Keys.Should().BeEquivalentTo(["main", "8.x"]);
	}

	[Test]
	public void WithLinkRegistryEntry_RepositoryWithoutMainOrMaster_IsAnErrorOfTheRecheckAndStillFiled()
	{
		var result = Registry().WithLinkRegistryEntry(Entry("tools", "dev"));

		result.HasErrors.Should().BeTrue();
		result.Value!.Repositories.Should().ContainKey("tools");
		result.Diagnostics.Should().OnlyContain(d => d.Severity == Severity.Error, "only errors are in a derived result");
	}

	// ---- LinkManifest.From / Empty ----

	private static GitCheckoutInformation Origin() => GitCheckoutInformation.ForElasticRepository("docs-builder", "abc", "main");

	[Test]
	public void LinkManifestFrom_OrdersPagesByPathKeepsTheFirstOfARepeatedPathAndSortsDistinctCrossLinks()
	{
		var result = LinkManifest.From(
			Origin(),
			"/docs",
			[
				new LinkPage("b.md", ["x"], false, []),
				new LinkPage("a.md", ["one", "two"], true, ["two"]),
				new LinkPage("b.md", ["ignored"], false, [])
			],
			["docs-content://z", "docs-content://a", "docs-content://z"],
			null
		);

		result.HasErrors.Should().BeFalse();
		var manifest = result.Value!;
		manifest.Links.Keys.Should().Equal("a.md", "b.md");
		manifest.Links["b.md"].Anchors.Should().Equal("x");
		manifest.Links["a.md"].Hidden.Should().BeTrue();
		manifest.CrossLinks.Should().Equal("docs-content://a", "docs-content://z");
		manifest.UrlPathPrefix.Should().Be("/docs");
		manifest.Redirects.Should().BeNull();
	}

	[Test]
	public void LinkManifestFrom_PageWithoutAnchors_IsWrittenWithoutAListAndReportedByTheSchemaRule()
	{
		var result = LinkManifest.From(Origin(), "", [new LinkPage("a.md", [], false, [])], [], null);

		result.Value!.Links["a.md"].Anchors.Should().BeNull("the wire format omits an empty anchors list");
		result.Diagnostics.Should().Contain(d => d.Message.Contains("has no anchors list"));
	}

	[Test]
	public void LinkManifestFrom_RedirectsAreKeyedIgnoringCaseAndCopied()
	{
		var declared = new Dictionary<string, LinkRedirect> { ["Old.md"] = new() { To = "new.md" } };

		var result = LinkManifest.From(Origin(), "", [new LinkPage("new.md", ["a"], false, [])], [], declared);

		result.Value!.Redirects.Should().ContainKey("old.md");
		result.Value.Redirects.Should().NotBeSameAs(declared);
	}

	[Test]
	public void LinkManifestFrom_UnavailableOrigin_IsAnErrorAndTheValueRidesAlong()
	{
		var result = LinkManifest.From(GitCheckoutInformation.Unavailable, "", [], [], null);

		result.HasErrors.Should().BeTrue();
		result.Value.Should().NotBeNull();
	}

	[Test]
	public void LinkManifestEmpty_IsAMainBranchManifestWithNoPages()
	{
		var result = LinkManifest.Empty("kibana");

		result.Diagnostics.Should().BeEmpty();
		var manifest = result.Value!;
		manifest.Links.Should().BeEmpty();
		manifest.CrossLinks.Should().BeEmpty();
		manifest.UrlPathPrefix.Should().BeEmpty();
		manifest
			.Origin
			.Should()
			.BeEquivalentTo(new GitCheckoutInformation
			{
				Branch = "main",
				RepositoryName = "kibana",
				Remote = "origin",
				Ref = "refs/heads/main"
			});
	}

	[Test]
	public void LinkManifestEmpty_BlankRepository_IsReported() => LinkManifest.Empty("").HasErrors.Should().BeTrue();

	// ---- ApplicableTo / AppliesCollection ----

	[Test]
	public void ForStack_LifecycleAndVersion_IsOnlyTheStack()
	{
		var version = VersionSpec.TryParse("9.1", out var v) ? v : null;

		var applicableTo = ApplicableTo.ForStack(ProductLifecycle.TechnicalPreview, version);

		applicableTo
			.Stack
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeEquivalentTo(new Applicability { Lifecycle = ProductLifecycle.TechnicalPreview, Version = version });
		applicableTo.Deployment.Should().BeNull();
		applicableTo.Serverless.Should().BeNull();
		applicableTo.Product.Should().BeNull();
	}

	[Test]
	public void ForStack_WithoutVersion_AppliesToEveryVersion()
	{
		var applicableTo = ApplicableTo.ForStack(ProductLifecycle.GenerallyAvailable, null);

		applicableTo.Stack.Should().ContainSingle().Which.Version.Should().BeNull();
	}

	[Test]
	public void WithoutDiagnostics_DropsOnlyTheDiagnostics()
	{
		var applicableTo = ApplicableTo.ForStack(ProductLifecycle.GenerallyAvailable, null) with
		{
			Diagnostics = new ApplicabilityDiagnosticsCollection([(Severity.Warning, "careful")])
		};

		var cleaned = applicableTo.WithoutDiagnostics();

		cleaned.Diagnostics.Should().BeNull();
		cleaned.Stack.Should().BeEquivalentTo(applicableTo.Stack);
		applicableTo.Diagnostics.Should().NotBeNull("the original is not modified");
	}

	[Test]
	public void AppliesCollectionOf_KeepsTheOrderGiven()
	{
		var a = new Applicability { Lifecycle = ProductLifecycle.Beta, Version = null };
		var b = new Applicability { Lifecycle = ProductLifecycle.Deprecated, Version = null };

		AppliesCollection.Of([b, a]).Should().Equal(b, a);
		AppliesCollection.Of([]).Should().BeEmpty();
	}

	// ---- GitCheckoutInformation ----

	[Test]
	public void Resolved_NamesTheRepositoryByTheLastSegmentOfTheRemote_AndBlankGitHubRefIsNone()
	{
		var info = GitCheckoutInformation.Resolved("sha", "main", "elastic/docs-builder", "");

		info.RepositoryName.Should().Be("docs-builder");
		info.GitHubRef.Should().BeNull();
		info.Ref.Should().Be("sha");
		info.Branch.Should().Be("main");
		GitCheckoutInformation
			.Resolved("sha", "main", "elastic/docs-builder", "refs/pull/1/merge")
			.GitHubRef
			.Should()
			.Be("refs/pull/1/merge");
	}

	[Test]
	public void ForElasticRepository_RemoteIsElasticSlashName()
	{
		var info = GitCheckoutInformation.ForElasticRepository("kibana", "sha", "8.x");

		info.Remote.Should().Be("elastic/kibana");
		info.RepositoryName.Should().Be("kibana");
		info.Branch.Should().Be("8.x");
		info.Ref.Should().Be("sha");
		info.IsAvailable.Should().BeTrue();
	}

	[Test]
	public void ForClonedRepository_KeepsTheNamesAsGiven()
	{
		var info = GitCheckoutInformation.ForClonedRepository("name", "git@github.com:x/y.git", "dev", "commit", null);

		info.RepositoryName.Should().Be("name");
		info.Remote.Should().Be("git@github.com:x/y.git");
		info.Branch.Should().Be("dev");
		info.Ref.Should().Be("commit");
		info.GitHubRef.Should().BeNull();
	}

	[Test]
	public void LegacyTestCheckout_IsAvailable() => GitCheckoutInformation.LegacyTestCheckout.IsAvailable.Should().BeTrue();

	// ---- Repository ----

	[Test]
	public void AsConfigurationCheckout_OnlyChecksOutTheConfigFolderAndKeepsTheRest()
	{
		var repository = new Repository
		{
			Name = "docs-builder",
			Origin = "git@github.com:elastic/docs-builder.git",
			GitReferenceCurrent = "main"
		};

		var checkout = repository.AsConfigurationCheckout();

		checkout.SparsePaths.Should().Equal("config");
		checkout.Name.Should().Be("docs-builder");
		checkout.Origin.Should().Be(repository.Origin);
		repository.SparsePaths.Should().Equal("docs");
	}
}
