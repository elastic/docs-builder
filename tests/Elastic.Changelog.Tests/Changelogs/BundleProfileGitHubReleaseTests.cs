// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Validates that <c>source: github_release</c> is rejected with a clear error.
/// Use <c>bundle.releases.github</c> to map release tags to profiles instead.
/// </summary>
public class BundleProfileGitHubReleaseTests : ChangelogTestBase
{
	public BundleProfileGitHubReleaseTests(ITestOutputHelper output) : base(output) { }

	[Fact]
	public async Task SourceGithubRelease_IsRemovedHardError()
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
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var loader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		var config = await loader.LoadChangelogConfigurationRequired(Collector, configPath, TestContext.Current.CancellationToken);

		config.Should().BeNull();
		Collector.Errors.Should().Be(1);
		Collector
			.Diagnostics
			.Should()
			.Contain(
				d => d.Severity == Severity.Error && d.Message.Contains("source: github_release") && d.Message.Contains(
					"bundle.releases.github"
				)
			);
	}
}
