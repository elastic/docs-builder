// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Validates that <c>source: github_release</c> emits a deprecation warning.
/// Use <c>bundle.releases.github</c> to map release tags to profiles instead.
/// </summary>
public class BundleProfileGitHubReleaseTests : ChangelogTestBase
{
	public BundleProfileGitHubReleaseTests() : base() { }

	[Test]
	public async Task SourceGithubRelease_EmitsDeprecationWarning()
	{
		// language=yaml
		var configContent =
			"""
			bundle:
			  profiles:
			    es-release:
			      source: github_release
			      product: elasticsearch
			""";

		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current!.Execution.CancellationToken);

		var loader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		var config = await loader.LoadChangelogConfigurationRequired(
			Collector,
			configPath,
			TestContext.Current!.Execution.CancellationToken
		);

		config.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		Collector
			.Diagnostics
			.Should()
			.Contain(
				d => d.Severity == Severity.Warning && d.Message.Contains("source") && d.Message.Contains(
					"deprecated"
				) && d.Message.Contains("bundle.releases.github")
			);
	}
}
