// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Nullean.Snapshots;

namespace Elastic.ApiExplorer.Tests;

public class ApiPageHashTests
{
	private const string Versionless = "/docs/api/elasticsearch";
	private const string V8 = "/docs/api/elasticsearch/v8";

	private static string Page(string root) =>
		$"""
		See [search]({root}/operation/operation-search) and [bool]({root}/types/query#bool).
		Also [serverless](/docs/api/elasticsearch-serverless/operation/operation-search).
		Back to {root}
		""";

	[Test]
	public void RelativeToTree_SamePageInTwoTrees_ReadsTheSame() =>
		OpenApiGenerator
			.RelativeToTree(Page(Versionless), Versionless, [V8])
			.Should()
			.Be(OpenApiGenerator.RelativeToTree(Page(V8), V8, [Versionless]));

	[Test]
	public void RelativeToTree_VersionlessTree_KeepsSiblingApisAndOtherTrees() =>
		OpenApiGenerator
			.RelativeToTree(Page(Versionless) + $"\nOlder: [8.19]({V8}/operation/operation-search)", Versionless, [V8])
			.Should()
			.MatchInlineSnapshot(
				"""
				See [search]({api-root}/operation/operation-search) and [bool]({api-root}/types/query#bool).
				Also [serverless](/docs/api/elasticsearch-serverless/operation/operation-search).
				Back to {api-root}
				Older: [8.19](/docs/api/elasticsearch/v8/operation/operation-search)
				"""
			);
}
