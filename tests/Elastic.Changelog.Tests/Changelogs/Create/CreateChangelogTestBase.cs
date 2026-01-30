// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Creation;
using Elastic.Changelog.GitHub;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Changelogs.Create;

public abstract class CreateChangelogTestBase(ITestOutputHelper output) : ChangelogTestBase(output)
{
	protected IGitHubPrService MockGitHubService { get; } = A.Fake<IGitHubPrService>();

	protected ChangelogCreationService CreateService() =>
		new(LoggerFactory, ConfigurationContext, MockGitHubService, FileSystem);

	protected async Task<string> CreateConfigDirectory(string configContent)
	{
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var configPath = FileSystem.Path.Combine(configDir, "changelog.yml");
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);
		return configPath;
	}

	protected string CreateOutputDirectory() =>
		FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
}
