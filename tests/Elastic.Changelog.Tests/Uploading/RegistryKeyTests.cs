// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Uploading;

namespace Elastic.Changelog.Tests.Uploading;

public class RegistryKeyTests
{
	[Theory]
	// Bundle index: {product}/registry.json
	[InlineData("elasticsearch/registry.json")]
	[InlineData("kibana/registry.json")]
	[InlineData("elastic-agent/registry.json")]
	[InlineData("cloud_hosted/registry.json")]
	[InlineData("a/registry.json")]
	// Changelog-entry index: {product}/changelog/registry.json
	[InlineData("elasticsearch/changelog/registry.json")]
	[InlineData("kibana/changelog/registry.json")]
	[InlineData("cloud_hosted/changelog/registry.json")]
	public void IsRegistry_ValidProductKeys_ReturnsTrue(string key) =>
		RegistryKey.IsRegistry(key).Should().BeTrue();

	[Theory]
	[InlineData("")]
	[InlineData("registry.json")]
	[InlineData("/registry.json")]
	[InlineData("elasticsearch/bundle/registry.json")]
	[InlineData("elasticsearch/registry.yaml")]
	[InlineData("elasticsearch/changelog/registry.yaml")]
	[InlineData("elasticsearch/changelog/bundle/registry.json")]
	[InlineData("../registry.json")]
	[InlineData("elastic search/registry.json")]
	[InlineData("elastic.search/registry.json")]
	[InlineData("elastic/search/registry.json")]
	[InlineData("elastic search/changelog/registry.json")]
	[InlineData("elastic/search/changelog/registry.json")]
	public void IsRegistry_InvalidKeys_ReturnsFalse(string key) =>
		RegistryKey.IsRegistry(key).Should().BeFalse();
}
