// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Constraints;
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
	public void Assembly_BreaksNoConstraint() => AssemblyConstraints.Check(AssemblyLoader.Create(Provider)).Should().BeEmpty();

	[Test]
	public void VersioningSystems_BreakNoConstraint() =>
		VersioningSystemConstraints.Check(CatalogLoader.Load(Provider).OrThrow().VersioningSystems).Should().BeEmpty();

	[Test]
	public void RelatedLearning_BreaksNoConstraint() =>
		RelatedLearningConstraints.Check(RelatedLearningLoader.Load(Provider).OrThrow()).Should().BeEmpty();

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
		);

		SiteNavigationConstraints.Check(navigation).Select(v => v.Message).Should().BeEmpty();
	}
}
