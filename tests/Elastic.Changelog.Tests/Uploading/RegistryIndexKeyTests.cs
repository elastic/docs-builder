// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Uploading;

namespace Elastic.Changelog.Tests.Uploading;

public class RegistryIndexKeyTests
{
	[Theory]
	[InlineData("elasticsearch/registry-index.json")]
	[InlineData("kibana/registry-index.json")]
	[InlineData("elastic-agent/registry-index.json")]
	[InlineData("cloud_hosted/registry-index.json")]
	[InlineData("a/registry-index.json")]
	public void IsRegistryIndex_ValidProductKeys_ReturnsTrue(string key) =>
		RegistryIndexKey.IsRegistryIndex(key).Should().BeTrue();

	[Theory]
	[InlineData("")]
	[InlineData("registry-index.json")]
	[InlineData("/registry-index.json")]
	[InlineData("elasticsearch/bundles/registry-index.json")]
	[InlineData("elasticsearch/registry-index.yaml")]
	[InlineData("elasticsearch/changelogs/registry-index.json")]
	[InlineData("../registry-index.json")]
	[InlineData("elastic search/registry-index.json")]
	[InlineData("elastic.search/registry-index.json")]
	[InlineData("elastic/search/registry-index.json")]
	public void IsRegistryIndex_InvalidKeys_ReturnsFalse(string key) =>
		RegistryIndexKey.IsRegistryIndex(key).Should().BeFalse();
}
