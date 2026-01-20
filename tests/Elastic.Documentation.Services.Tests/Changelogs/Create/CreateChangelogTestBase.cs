// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Services.Changelog;
using FakeItEasy;

namespace Elastic.Documentation.Services.Tests.Changelogs.Create;

public abstract class CreateChangelogTestBase(ITestOutputHelper output) : ChangelogTestBase(output)
{
	protected IGitHubPrService MockGitHubService { get; } = A.Fake<IGitHubPrService>();

	protected ChangelogService CreateService() =>
		new(_loggerFactory, _configurationContext, MockGitHubService, _fileSystem);

	protected async Task<string> CreateConfigDirectory(string configContent)
	{
		var configDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		_fileSystem.Directory.CreateDirectory(configDir);
		var configPath = _fileSystem.Path.Combine(configDir, "changelog.yml");
		await _fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);
		return configPath;
	}

	protected string CreateOutputDirectory() =>
		_fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
}
