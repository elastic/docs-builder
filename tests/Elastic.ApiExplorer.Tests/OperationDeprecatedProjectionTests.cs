// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Export;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Site.Navigation;
using Elastic.Documentation.Versions;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class OperationDeprecatedProjectionTests(ApiExplorerFixture fixture)
{
	private const string SpecTemplate =
		"""
		{
		  "openapi":"3.0.3", "info":{"title":"Lifecycle","version":"1"},
		  "paths":{"/thing":{"get":{"operationId":"thing-get","summary":"Get a thing",DEPRECATED"responses":{"200":{"description":"OK"}}}}}
		}
		""";

	[Test]
	public void Deprecated_SpecFlag_ReadsThroughApiOperation()
	{
		Operation("docs-get-source").Model.Deprecated.Should().BeTrue();
		Operation("docs-get").Model.Deprecated.Should().BeFalse();
	}

	[Test]
	public void CreateNavigation_CollapsedSiblings_AgreeOnDeprecated()
	{
		foreach (var navigation in fixture.Walk().OfType<OperationNavigationItem>())
			navigation.Siblings.Should().OnlyContain(s => s.Deprecated == navigation.Model.Deprecated);
	}

	[Test]
	public void Create_DeprecatedOperation_ProjectsDeprecatedOntoTheLeaf()
	{
		var model = NavigationRenderModel.Create(
			fixture.Navigation,
			[fixture.Navigation],
			isUsingNavigationDropdown: false,
			isPrimaryNavEnabled: false,
			isGlobalAssemblyBuild: false
		);
		var leaves = Flatten(model.Tree).ToArray();

		var deprecated = leaves.Single(n => n.Url == Operation("docs-get-source").Url);
		deprecated.Deprecated.Should().BeTrue();
		deprecated.HttpMethod.Should().Be("get");
		leaves.Single(n => n.Url == Operation("docs-get").Url).Deprecated.Should().BeFalse();
	}

	[Test]
	public async Task Create_FlippingDeprecated_ChangesTheContentHash()
	{
		var current = await HashAsync(SpecTemplate.Replace("DEPRECATED", "", StringComparison.Ordinal));
		var currentAgain = await HashAsync(SpecTemplate.Replace("DEPRECATED", "", StringComparison.Ordinal));
		var deprecated = await HashAsync(SpecTemplate.Replace("DEPRECATED", "\"deprecated\":true,", StringComparison.Ordinal));

		currentAgain.Should().Be(current);
		deprecated.Should().NotBe(current);
	}

	[Test]
	public void ConvertToDocuments_DeprecatedOperation_SetsApiDeprecated()
	{
		var versions = new VersionsConfiguration
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				[VersioningSystemId.Stack] = new VersioningSystem
				{
					Id = VersioningSystemId.Stack,
					Base = new SemVersion(8, 0, 0),
					Current = new SemVersion(99, 0, 0)
				}
			}
		};
		var documents = new OpenApiDocumentExporter(versions)
			.ConvertToDocuments(new IndexedTree("fixture", "fixture", "main", fixture.Document))
			.ToArray();

		documents.Single(d => d.Api!.OperationId == "docs-get-source").Api!.Deprecated.Should().BeTrue();
		documents.Single(d => d.Api!.OperationId == "docs-get").Api!.Deprecated.Should().BeFalse();
	}

	private OperationNavigationItem Operation(string operationId) =>
		fixture.Walk().OfType<OperationNavigationItem>().Single(n => n.Model.Operation.OperationId == operationId);

	private static IEnumerable<NavigationRenderNode> Flatten(IEnumerable<NavigationRenderNode> nodes) =>
		nodes.SelectMany(n => Flatten(n.NavigationItems).Prepend(n));

	private static async Task<string> HashAsync(string json)
	{
		var (generator, document) = await TestSpecs.CreateGeneratorWithSpecAsync(json);
		var navigation = generator.CreateNavigation("lifecycle", document);
		return NavigationRenderModel.Create(
			navigation,
			[navigation],
			isUsingNavigationDropdown: false,
			isPrimaryNavEnabled: false,
			isGlobalAssemblyBuild: false
		).ContentHash;
	}
}
