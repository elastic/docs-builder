// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Floating;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class ApiLinkSectionsTests
{
	private static ApiProductTrees Trees(params (string Moniker, string[] Names)[] versions)
	{
		var documents = versions.Select(
			v => new VersionedOpenApiDocument(
				new ResolvedApiVersion { Moniker = v.Moniker, Version = v.Moniker, IsLocal = false },
				new OpenApiDocument()
			)
		).ToArray();
		// the first name of a page is its canonical path; the others are its aliases
		var pageNames = versions.ToDictionary(
			v => v.Moniker,
			v => (IReadOnlyDictionary<string, IReadOnlyList<string>>)v.Names.ToDictionary(n => n, _ => (IReadOnlyList<string>)v.Names)
		);
		return new ApiProductTrees(new ResolvedProductDocuments(documents, null), [], null, pageNames);
	}

	[Test]
	public void Create_AliasWithDifferentCanonicalPagePerVersion_KeepsEachTargetWithItsVersion()
	{
		var trees = Trees(
			("main", ["operation/operation-search", "operation/operation-search-1"]),
			("8", ["operation/operation-search-1", "operation/operation-search"])
		);

		var links = ApiLinkSections.Create("api/doc/elasticsearch", trees);

		links.TryResolvePage("main", "operation/operation-search-1", out var main).Should().BeTrue();
		main.Should().Be("api/doc/elasticsearch/operation/operation-search");
		links.TryResolvePage("8", "operation/operation-search", out var v8).Should().BeTrue();
		v8.Should().Be("api/doc/elasticsearch/v8/operation/operation-search-1");
	}

	[Test]
	public void Create_AliasOnlyInOneVersion_DoesNotResolveInTheOthers()
	{
		var trees = Trees(("main", ["operation/operation-search"]), ("8", ["operation/operation-search", "operation/operation-search-1"]));

		var links = ApiLinkSections.Create("api/doc/elasticsearch", trees);

		links.TryResolvePage("8", "operation/operation-search-1", out _).Should().BeTrue();
		links.TryResolvePage("main", "operation/operation-search-1", out _).Should().BeFalse();
	}
}
