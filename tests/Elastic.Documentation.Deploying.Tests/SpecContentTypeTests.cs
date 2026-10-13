// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Deploying.Synchronization;

namespace Elastic.Documentation.Deploying.Tests;

public class SpecContentTypeTests
{
	[Test]
	[Arguments("docs/api/doc/elasticsearch.json", "application/json")]
	[Arguments("docs/api/doc/elasticsearch/v8.yaml", "text/plain; charset=utf-8")]
	[Arguments("docs/api/doc/elasticsearch.YML", "text/plain; charset=utf-8")]
	public void SpecContentType_SpecFile_ReturnsExplicitType(string key, string expected) =>
		AwsS3SyncApplyStrategy.SpecContentType(key).Should().Be(expected);

	[Test]
	[Arguments("docs/index.html")]
	[Arguments("docs/index.md")]
	[Arguments("docs/_static/main.js")]
	public void SpecContentType_OtherFile_IsNullSoTheSdkDecides(string key) =>
		AwsS3SyncApplyStrategy.SpecContentType(key).Should().BeNull();
}
