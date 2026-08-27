// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Isolated;

namespace Elastic.Documentation.Build.Tests;

public class IsolatedBuildRedirectTests
{
	[Theory]
	[InlineData("migration/freeze/gh-action.md", "/en/docs-builder", "/en/docs-builder/migration/freeze/gh-action")]
	[InlineData("schema-support/cli-schema/index.md", "/en/docs-builder", "/en/docs-builder/schema-support/cli-schema")]
	[InlineData("index.md", "/en/docs-builder", "/en/docs-builder")]
	[InlineData("migration/freeze/index.md", "/en/docs-builder", "/en/docs-builder/migration/freeze")]
	[InlineData("cli/installation.md", "/en/docs-builder", "/en/docs-builder/cli/installation")]
	[InlineData("index.md", "", "/")]
	[InlineData("index.md", "/", "/")]
	[InlineData("migrate/index.md", "", "/migrate")]
	[InlineData("migrate/index.md", "/", "/migrate")]
	public void ToAbsoluteUrl_VariousPaths_ProducesExpectedUrl(string path, string pathPrefix, string expected) =>
		IsolatedBuildService.ToAbsoluteUrl(path, pathPrefix).Should().Be(expected);

	[Theory]
	[InlineData("migration/freeze/gh-action.md", "migration/freeze/gh-action.md")]
	[InlineData("cli/installation.md", "cli/installation.md")]
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
