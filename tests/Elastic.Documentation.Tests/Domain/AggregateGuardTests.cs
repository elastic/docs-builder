// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using AwesomeAssertions;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Codex;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Links;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Search;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// The compile-time guard (see Constraints-Conventions.md): production code outside Elastic.Documentation and
/// Elastic.Documentation.Configuration cannot create or edit an aggregate, because its setters and constructors are internal
/// and only those two assemblies (and the tests) see internals. This test fails when a sealed type gains a public setter,
/// init accessor or constructor with parameters, so the guard cannot regress silently.
/// </summary>
public class AggregateGuardTests
{
	/// <summary>Stage 1: the global-configuration aggregates and their member records.</summary>
	public static IEnumerable<Type> Stage1Types() =>
		[
			// assembler.yml
			typeof(AssemblyConfiguration),
			typeof(Repository),
			typeof(NarrativeRepository),
			typeof(PublishEnvironment),
			typeof(GoogleTagManager),
			typeof(Optimizely),
			// versions.yml, products.yml, legacy-url-mappings.yml
			typeof(GlobalCatalog),
			typeof(VersioningSystemCatalog),
			typeof(VersioningSystem),
			typeof(ProductCatalog),
			typeof(Product),
			typeof(ProductFeatures),
			typeof(LegacyUrlMappingCatalog),
			typeof(LegacyUrlMapping),
			// related-learning.yml
			typeof(RelatedLearningCatalog),
			typeof(RelatedLearningLink),
			// search.yml
			typeof(SearchTuning),
			typeof(QueryRule),
			typeof(QueryRuleCriteria),
			typeof(QueryRuleActions),
			typeof(SynonymGroup),
			// codex.yml
			typeof(CodexConfiguration),
			typeof(CodexGroupDefinition),
			typeof(CodexDocumentationSetReference),
			// navigation.yml
			typeof(SiteNavigationDefinition),
			typeof(SiteSectionRef),
			typeof(SiteTableOfContentsRef),
			typeof(SiteDropdownLinkRef),
			typeof(PhantomRegistration),
			typeof(NavigationTocMapping)
		];

	/// <summary>Stage 2: applicability, documentation sets, links, release notes. <c>SyncPlan</c> and <c>VersionIndexEntry</c> are guarded in their own projects' tests.</summary>
	public static IEnumerable<Type> Stage2Types() =>
		[
			// applicability: the applies_to block and its members
			typeof(ApplicableTo),
			typeof(DeploymentApplicability),
			typeof(ServerlessProjectApplicability),
			typeof(ProductApplicability),
			typeof(Applicability),
			typeof(AppliesCollection),
			// docset.yml and the resolved set
			typeof(DocumentationSetDefinition),
			typeof(DroppedRedirect),
			typeof(DeclaredFeatures),
			typeof(BrandingDeclaration),
			typeof(CtaDeclaration),
			typeof(ApiDeclaration),
			typeof(ApiDeclarationEntry),
			typeof(ResolvedDocumentationSet),
			typeof(DocumentationSetExclusions),
			typeof(CrossLinkEntry),
			typeof(EnabledExtensions),
			typeof(Branding),
			typeof(Cta),
			typeof(ResolvedApiConfiguration),
			typeof(TocEntryAuthoring),
			// table of contents cases
			typeof(FileRef),
			typeof(IndexFileRef),
			typeof(FolderIndexFileRef),
			typeof(DetectionRuleRef),
			typeof(DetectionRuleOverviewRef),
			typeof(CrossLinkRef),
			typeof(FolderRef),
			typeof(DeepLinkedFolderRef),
			typeof(IsolatedTableOfContentsRef),
			typeof(ListingOptions),
			typeof(ListingRef),
			typeof(ListingGroupRef),
			typeof(CliReferenceRef),
			// links.json, link-index.json and the git checkout
			typeof(LinkManifest),
			typeof(LinkMetadata),
			typeof(LinkSingleRedirect),
			typeof(LinkRedirect),
			typeof(LinkRegistry),
			typeof(LinkRegistryEntry),
			typeof(GitCheckoutInformation),
			// release notes: entries, bundles and the changelog configuration
			typeof(ChangelogEntry),
			typeof(Bundle),
			typeof(BundledEntry),
			typeof(BundledProduct),
			typeof(BundledFile),
			typeof(ProductReference),
			typeof(LoadedBundle),
			typeof(ChangelogConfiguration),
			typeof(RulesConfiguration),
			typeof(CreateRules),
			typeof(BundlePerProductRule),
			typeof(BundleRules),
			typeof(PublishRules),
			typeof(BundleConfiguration),
			typeof(BundleProfile),
			typeof(BundleReleases),
			typeof(ExtractConfiguration),
			typeof(PivotConfiguration),
			typeof(TypeEntry),
			typeof(ProductsConfig),
			typeof(DefaultProduct),
			typeof(PublishBlocker)
		];

	public static List<string> PublicSetters(IEnumerable<Type> types) =>
		types.SelectMany(
			t => t
				.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
				.Where(p => p.SetMethod is { IsPublic: true })
				.Select(p => $"{t.Name}.{p.Name}")
		).ToList();

	public static List<string> PublicParameterizedConstructors(IEnumerable<Type> types) =>
		types.SelectMany(
			t => t
				.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
				.Where(c => c.GetParameters().Length > 0)
				.Select(c => $"{t.Name}({string.Join(", ", c.GetParameters().Select(p => p.ParameterType.Name))})")
		).ToList();

	[Test]
	public void Stage1Aggregates_HaveNoPublicSetterOrInit() =>
		PublicSetters(Stage1Types())
			.Should()
			.BeEmpty("a public setter or init accessor lets any production assembly edit an aggregate; make it internal");

	[Test]
	public void Stage1Aggregates_HaveNoPublicConstructorWithParameters() =>
		PublicParameterizedConstructors(Stage1Types())
			.Should()
			.BeEmpty("a public constructor lets any production assembly build an aggregate; make it internal");

	[Test]
	public void Stage2Aggregates_HaveNoPublicSetterOrInit() =>
		PublicSetters(Stage2Types())
			.Should()
			.BeEmpty("a public setter or init accessor lets any production assembly edit an aggregate; make it internal");

	[Test]
	public void Stage2Aggregates_HaveNoPublicConstructorWithParameters() =>
		PublicParameterizedConstructors(Stage2Types())
			.Should()
			.BeEmpty("a public constructor lets any production assembly build an aggregate; make it internal");
}
