// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Navigation.Isolated;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

public class IdentifierCollectionTests(ITestOutputHelper output)
{
	[Fact]
	public void DocumentationSetNavigationCollectsRootIdentifier()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Test platform repository
		var platformContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.Deserialize(
			fileSystem.File.ReadAllText("/checkouts/current/platform/docs/docset.yml"));
		var platformNav = new DocumentationSetNavigation(platformDocset, platformContext, TestDocumentationFileFactory.Instance);

		// Root identifier should be <repository>://
		platformNav.Identifier.Should().Be(new Uri("platform://"));
		platformNav.TableOfContentNodes.Keys.Should().Contain(new Uri("platform://"));
	}

	[Fact]
	public void DocumentationSetNavigationCollectsNestedTocIdentifiers()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Test platform repository with nested TOCs
		var platformContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.Deserialize(
			fileSystem.File.ReadAllText("/checkouts/current/platform/docs/docset.yml"));
		var platformNav = new DocumentationSetNavigation(platformDocset, platformContext, TestDocumentationFileFactory.Instance);

		// Should collect identifiers from nested TOCs
		platformNav.TableOfContentNodes.Keys.Should().Contain(
		[
			new Uri("platform://"),
			new Uri("platform://deployment-guide"),
			new Uri("platform://cloud-guide")
		]);

		platformNav.TableOfContentNodes.Should().HaveCount(3);
	}

	[Fact]
	public void DocumentationSetNavigationWithSimpleStructure()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Test observability repository (no nested TOCs)
		var observabilityContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/observability", output);
		var observabilityDocset = DocumentationSetFile.Deserialize(
			fileSystem.File.ReadAllText("/checkouts/current/observability/docs/docset.yml"));
		var observabilityNav = new DocumentationSetNavigation(observabilityDocset, observabilityContext, TestDocumentationFileFactory.Instance);

		// Should only have root identifier
		observabilityNav.TableOfContentNodes.Keys.Should().Contain(new Uri("observability://"));
		observabilityNav.TableOfContentNodes.Should().HaveCount(1);
	}

	[Fact]
	public void TableOfContentsNavigationHasCorrectIdentifier()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Test platform repository with nested TOCs
		var platformContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.Deserialize(
			fileSystem.File.ReadAllText("/checkouts/current/platform/docs/docset.yml"));
		var platformNav = new DocumentationSetNavigation(platformDocset, platformContext, TestDocumentationFileFactory.Instance);

		// Get the deployment-guide TOC
		var deploymentGuide = platformNav.NavigationItems.ElementAt(1) as TableOfContentsNavigation;
		deploymentGuide.Should().NotBeNull();
		deploymentGuide!.Identifier.Should().Be(new Uri("platform://deployment-guide"));

		// Get the cloud-guide TOC
		var cloudGuide = platformNav.NavigationItems.ElementAt(2) as TableOfContentsNavigation;
		cloudGuide.Should().NotBeNull();
		cloudGuide!.Identifier.Should().Be(new Uri("platform://cloud-guide"));
	}

	[Fact]
	public void MultipleDocumentationSetsHaveDistinctIdentifiers()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Create multiple documentation sets
		var platformContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.Deserialize(
			fileSystem.File.ReadAllText("/checkouts/current/platform/docs/docset.yml"));
		var platformNav = new DocumentationSetNavigation(platformDocset, platformContext, TestDocumentationFileFactory.Instance);

		var observabilityContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/observability", output);
		var observabilityDocset = DocumentationSetFile.Deserialize(
			fileSystem.File.ReadAllText("/checkouts/current/observability/docs/docset.yml"));
		var observabilityNav = new DocumentationSetNavigation(observabilityDocset, observabilityContext, TestDocumentationFileFactory.Instance);

		// Each should have its own set of identifiers
		platformNav.TableOfContentNodes.Keys.Should().NotIntersectWith(observabilityNav.TableOfContentNodes.Keys);

		// Platform should have repository name in its identifiers
		platformNav.TableOfContentNodes.Keys.Should().AllSatisfy(id => id.Scheme.Should().Be("platform"));

		// Observability should have repository name in its identifiers
		observabilityNav.TableOfContentNodes.Keys.Should().AllSatisfy(id => id.Scheme.Should().Be("observability"));
	}
}
