// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Publishing;
using static Elastic.Documentation.Configuration.Tests.Golden.Golden;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// The configuration docs-builder ships in <c>config/</c> breaks no constraint, warnings included. Warnings in global
/// configuration only surface in assembler builds, so this is where a change to <c>config/</c> meets them first.
/// </summary>
public class ShippedConfigurationTests
{
	private static ConfigurationFileProvider Provider => ProviderFor(Path.Join(Paths.GetSolutionDirectory()!.FullName, "config"));

	[Test]
	public void Assembly_BreaksNoConstraint() =>
		AssemblyConstraints
			.All
			.Validate(AssemblyLoader.Create(Provider))
			.Where(
				v =>
					v.ConstraintId is not (
					// FINDING G12: the preview environment's path_prefix is the placeholder ${ASSEMBLER_PREVIEW_PATH_PREFIX}, rewritten by a CI yq step.
					"environment-path-prefix-resolved"
					// FINDING G28: assembler.yml lists repositories with skip: true that are never built.
					or "repository-skipped"
					// FINDING G36: assembler.yml declares local, preview and air-gapped, which the runtime allow-list (dev, edge, staging, prod) does not accept.
					or "environment-name-in-runtime-list"
					// FINDING G37: every feature flag is written UPPER_SNAKE_CASE, which the loader lower-cases and rewrites to kebab-case.
					or "feature-flag-keys-normalized")
			)
			.Select(v => $"{v.ConstraintId}: {v.Message}")
			.Should()
			.BeEmpty();

	[Test]
	public void NavigationInAssembly_BreaksNoConstraint()
	{
		var assembly = AssemblyLoader.Create(Provider);
		var yaml = File.ReadAllText(Path.Join(Paths.GetSolutionDirectory()!.FullName, "config", "navigation.yml"));
		var navigation = SiteNavigationLoader.ForAssembly(
			SiteNavigationLoader.Deserialize(yaml),
			assembly,
			skipPrivateRepositories: false
		).OrThrowIfMissing();

		NavigationInAssemblyConstraints
			.All
			.Validate(new NavigationInAssembly(navigation, assembly))
			.Select(v => v.Message)
			.Should()
			.BeEmpty();
	}

	[Test]
	public void VersioningSystems_BreakNoConstraint() =>
		VersioningSystemConstraints
			.All
			.Validate(CatalogLoader.Load(Provider).OrThrow().VersioningSystems)
			// FINDING G90: versions.yml has no entry for apm-agent-android and apm-agent-ios, which are members of VersioningSystemId; looking either up throws.
			.Where(v => v.ConstraintId is not "versioning-systems-complete")
			.Should()
			.BeEmpty();

	[Test]
	public void Catalog_LoadReportsNoFinding() =>
		CatalogLoader
			.Load(Provider)
			.Diagnostics
			// FINDING G90: versions.yml has no entry for apm-agent-android and apm-agent-ios, which are members of VersioningSystemId; looking either up throws.
			.Where(d => d.RuleId is not "versioning-systems-complete")
			.Select(d => $"{d.RuleId}: {d.Message}")
			.Should()
			.BeEmpty();

	[Test]
	public void SearchTuning_LoadReportsNoFinding() =>
		SearchTuningLoader
			.Load(Provider)
			.Diagnostics
			// FINDING G97: search.yml writes explicit-mapping synonyms such as 'aggs => aggregations', whose terms contain spaces and are left out of the bidirectional map.
			.Where(d => d.RuleId is not "synonym-terms-single-word")
			.Select(d => $"{d.RuleId}: {d.Message}")
			.Should()
			.BeEmpty();

	[Test]
	public void RelatedLearning_BreaksNoConstraint() =>
		RelatedLearningConstraints.All.Validate(RelatedLearningLoader.Load(Provider).OrThrow()).Should().BeEmpty();

	[Test]
	[Arguments("navigation.yml")]
	[Arguments("navigation_preview.yml")]
	public void SiteNavigation_BreaksNoConstraint(string file)
	{
		var yaml = File.ReadAllText(Path.Join(Paths.GetSolutionDirectory()!.FullName, "config", file));
		var navigation = SiteNavigationLoader.ForAssembly(
			SiteNavigationLoader.Deserialize(yaml),
			AssemblyLoader.Create(Provider),
			skipPrivateRepositories: false
		).OrThrowIfMissing();

		SiteNavigationConstraints
			.All
			.Validate(navigation)
			// FINDING N14: elasticsearch://reference/elasticsearch-plugins/ starts with the mounted elasticsearch://reference/elasticsearch/, so a plain StartsWith resolves a plugin link to the shorter mount; docs-content://reference/apm-agents/ and reference/apm/ are the same case.
			.Where(v => v.ConstraintId is not "mount-sources-segment-prefix-free")
			.Select(v => $"{v.ConstraintId}: {v.Message}")
			.Should()
			.BeEmpty();
	}
}
