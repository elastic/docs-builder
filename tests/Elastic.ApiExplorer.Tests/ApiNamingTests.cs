// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;

namespace Elastic.ApiExplorer.Tests;

public class ApiNamingTests
{
	[Test]
	[Arguments("Logstash APIs", "Logstash API")]
	[Arguments("Elasticsearch API", "Elasticsearch API")]
	[Arguments("APIs for Kibana", "API for Kibana")]
	[Arguments("Rapids", "Rapids")]
	public void Singularize_Title_ReplacesOnlyTheWordAPIs(string title, string expected) =>
		ApiNaming.Singularize(title).Should().Be(expected);

	[Test]
	public void UsesPlural_SingularTitle_ReturnsFalse() => ApiNaming.UsesPlural("Elasticsearch API").Should().BeFalse();

	[Test]
	public void CatalogLabels_AreSingular()
	{
		ApiNaming.UsesPlural(ApiCatalog.PageTitle).Should().BeFalse();
		ApiNaming.UsesPlural(Infrastructure.ApiBreadcrumbs.CatalogLabel).Should().BeFalse();
	}
}
