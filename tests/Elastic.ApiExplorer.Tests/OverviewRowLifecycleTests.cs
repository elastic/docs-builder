// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Navigation;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class OverviewRowLifecycleTests(ApiExplorerFixture fixture)
{
	[Test]
	public void BuildTagChildren_DeprecatedOperation_FlagsTheRow()
	{
		var deprecated = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "docs-get-source");
		var tag = (INavigationItem)deprecated.Parent!;

		var rows = ApiOverviewBuilder.BuildTagChildren(tag, fixture.Context.VersionsConfiguration);

		rows.Single(r => r.Url == deprecated.Url).Deprecated.Should().BeTrue();
		rows.Where(r => r.Url != deprecated.Url).Should().OnlyContain(r => !r.Deprecated);
	}
}
