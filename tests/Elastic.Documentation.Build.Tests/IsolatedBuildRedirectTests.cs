// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Isolated;

namespace Elastic.Documentation.Build.Tests;

public class IsolatedBuildRedirectTests
{
	[Test]
	[Arguments("migration/freeze/gh-action.md", "/en/docs-builder", "/en/docs-builder/migration/freeze/gh-action")]
	[Arguments("schema-support/cli-schema/index.md", "/en/docs-builder", "/en/docs-builder/schema-support/cli-schema")]
	[Arguments("index.md", "/en/docs-builder", "/en/docs-builder")]
	[Arguments("migration/freeze/index.md", "/en/docs-builder", "/en/docs-builder/migration/freeze")]
	[Arguments("cli/installation.md", "/en/docs-builder", "/en/docs-builder/cli/installation")]
	[Arguments("index.md", "", "/")]
	[Arguments("index.md", "/", "/")]
	[Arguments("migrate/index.md", "", "/migrate")]
	[Arguments("migrate/index.md", "/", "/migrate")]
	public void ToAbsoluteUrl_VariousPaths_ProducesExpectedUrl(string path, string pathPrefix, string expected) =>
		IsolatedBuildService.ToAbsoluteUrl(path, pathPrefix).Should().Be(expected);

	[Test]
	[Arguments("migration/freeze/gh-action.md", "migration/freeze/gh-action.md")]
	[Arguments("cli/installation.md", "cli/installation.md")]
	public void ToAbsoluteUrl_FromAndToEquivalent_SelfRedirectDetected(string path, string to)
	{
		var prefix = "/en/docs-builder";
		IsolatedBuildService
			.ToAbsoluteUrl(path, prefix)
			.TrimEnd('/')
			.Should()
			.Be(IsolatedBuildService.ToAbsoluteUrl(to, prefix).TrimEnd('/'));
	}
}
