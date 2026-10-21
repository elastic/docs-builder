// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Codex;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Links;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Search;
using Nullean.Entities;
using Nullean.Entities.CsCheck;
using Nullean.Entities.CsCheck.FileSystem;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// A constraint set reports; it does not throw. Each test runs a set over values built from nothing but hostile leaves (blank, NUL and lone-surrogate
/// strings, NaN, undefined enums, nulls, empty and duplicated collections, equal start and end) and requires every rule to answer with a violation
/// or none. These values are nothing a loader would produce; they are what a rule that reads a member without looking must survive.
/// </summary>
public class AdversarialConstraintTests
{
	private static void Reports<T>(ConstraintSet<T> set, Action<AutoOptions>? configure = null)
	{
		var generated = 0;
		var violated = 0;
		GenAuto.Of<T>(AutoMode.Adversarial, configure).Sample(
			value =>
			{
				var validate = () => set.Validate(value);
				validate.Should().NotThrow();
				generated++;
				if (set.Validate(value).Count > 0)
					violated++;
			},
			seed: Props.Seed,
			iter: 400,
			threads: 1,
			print: value => value?.ToString() ?? "null"
		);

		// not vacuous: hostile values must trip rules, or the generator is not producing what the test is about
		generated.Should().BeGreaterThan(0);
		violated.Should().BeGreaterThan(0, "the hostile values never broke a rule of {0}", typeof(T).Name);
	}

	[Test]
	public void ProductCatalog() => Reports(ProductCatalogConstraints.All);

	[Test]
	public void VersioningSystemCatalog() => Reports(VersioningSystemConstraints.All);

	[Test]
	public void ProductCatalogInVersioningSystemCatalog() => Reports(ProductCatalogInVersioningSystemCatalogConstraints.All);

	[Test]
	public void LegacyUrlMappingCatalogInProductCatalog() => Reports(LegacyUrlMappingCatalogInProductCatalogConstraints.All);

	[Test]
	public void SearchTuning() => Reports(SearchTuningConstraints.All);

	[Test]
	public void RelatedLearningCatalog() => Reports(RelatedLearningConstraints.All);

	[Test]
	public void AssemblyConfiguration() => Reports(AssemblyConstraints.All);

	[Test]
	public void LinkManifest() => Reports(LinkManifestConstraints.All);

	[Test]
	public void LinkRegistry() => Reports(LinkRegistryConstraints.All);

	[Test]
	public void ApplicableTo() => Reports(ApplicableToConstraints.All);

	[Test]
	public void Bundle() => Reports(BundleConstraints.All);

	[Test]
	public void LoadedBundle() => Reports(LoadedBundleConstraints.All);

	[Test]
	public void LoadedBundles() => Reports(LoadedBundlesConstraints.All);

	[Test]
	public void ChangelogEntry() => Reports(ChangelogEntryConstraints.All);

	[Test]
	public void SiteNavigationDefinition() => Reports(SiteNavigationConstraints.All);

	[Test]
	public void CodexConfiguration() => Reports(CodexConfigurationConstraints.All);

	[Test]
	public void CodexPublishing() => Reports(CodexPublishingConstraints.All);

	[Test]
	public void ChangelogConfiguration() => Reports(ChangelogConfigurationConstraints.All);

	[Test]
	public void ChangelogEntryInConfiguration() => Reports(ChangelogEntryInConfigurationConstraints.All);

	[Test]
	public void NavigationInAssembly() => Reports(NavigationInAssemblyConstraints.All, o => o.UseFileSystem());

	[Test]
	public void DocumentationSetInCatalog() => Reports(DocumentationSetConstraints.All, o => o.UseFileSystem());

	[Test]
	public void DocumentationSetInCodex() => Reports(DocumentationSetInCodexConstraints.All, o => o.UseFileSystem());

	[Test]
	public void DocsetDiscovery() => Reports(DocsetDiscoveryConstraints.All, o => o.UseFileSystem());

	[Test]
	public void AssembledSite() => Reports(AssembledSiteConstraints.All, o => o.UseFileSystem());

	[Test]
	public void LinkGraph() => Reports(LinkGraphConstraints.All, o => o.UseFileSystem());
}
