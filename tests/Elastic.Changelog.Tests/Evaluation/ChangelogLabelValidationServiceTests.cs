// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Actions.Core.Services;
using AwesomeAssertions;
using Elastic.Changelog.Evaluation;
using Elastic.Changelog.Tests.Changelogs;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Evaluation;

public class ChangelogLabelValidationServiceTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	private static readonly string Root = Paths.WorkingDirectoryRoot.FullName;
	private readonly ICoreService _mockCore = A.Fake<ICoreService>();

	// All three required types must be present or the config loader emits an error and falls back to Default.
	private const string MinimalConfig =
		"""
		pivot:
		  types:
		    feature: "type:feature"
		    bug-fix:
		    breaking-change:
		""";

	private const string ConfigWithExcludeRule =
		"""
		pivot:
		  types:
		    feature: "type:feature"
		    bug-fix:
		    breaking-change:
		rules:
		  create:
		    exclude: "changelog:skip"
		""";

	private string ConfigPath => Path.Join(Root, "changelog.yml");

	private string MetadataPath => Path.Join(Root, GithubDecisionMetadataWriter.ArtifactDir, GithubDecisionMetadataWriter.MetadataFilename);

	private async Task WriteConfig(string content)
	{
		FileSystem.Directory.CreateDirectory(Root);
		await FileSystem.File.WriteAllTextAsync(ConfigPath, content);
	}

	private async Task<GithubDecisionMetadata?> ReadMetadata()
	{
		var reader = new GithubDecisionMetadataWriter(LoggerFactory, RunnerTempFileSystem);
		return await reader.ReadAsync(MetadataPath, CancellationToken.None);
	}

	private ChangelogLabelValidationService CreateService(IEnvironmentVariables? env = null) =>
		new(LoggerFactory, ConfigurationContext, _mockCore, RunnerTempFileSystem, env);

	private ValidateLabelsArguments DefaultArgs(string[]? labels = null, int prNumber = 0) =>
		new()
		{
			Config = ConfigPath,
			PrLabels = labels ?? ["type:feature"],
			PrNumber = prNumber,
			HeadRef = "feature/test",
			HeadSha = "abc123",
			CanCommit = true
		};

	private void VerifyOutputSet(string name, string value) => A.CallTo(() => _mockCore.SetOutputAsync(name, value)).MustHaveHappened();

	[Fact]
	public async Task ValidateLabels_MatchingTypeLabel_ReturnsTrue()
	{
		await WriteConfig(MinimalConfig);

		var result = await CreateService().ValidateLabels(Collector, DefaultArgs(["type:feature"]), CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "ok");
	}

	[Fact]
	public async Task ValidateLabels_NoMatchingLabel_ReturnsFalse()
	{
		await WriteConfig(MinimalConfig);

		var result = await CreateService().ValidateLabels(Collector, DefaultArgs([]), CancellationToken.None);

		result.Should().BeFalse();
		VerifyOutputSet("status", "no-label");
	}

	[Fact]
	public async Task ValidateLabels_SkipLabelPresent_ReturnsSkipped()
	{
		await WriteConfig(ConfigWithExcludeRule);

		var result = await CreateService().ValidateLabels(Collector, DefaultArgs(["changelog:skip"]), CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "skipped");
	}

	[Fact]
	public async Task ValidateLabels_OnCI_WithPrNumber_WritesMetadataFile()
	{
		await WriteConfig(MinimalConfig);
		var env = A.Fake<IEnvironmentVariables>();
		A.CallTo(() => env.IsRunningOnCI).Returns(true);

		await CreateService(env).ValidateLabels(Collector, DefaultArgs(["type:feature"], prNumber: 42), CancellationToken.None);

		var metadata = await ReadMetadata();
		metadata.Should().NotBeNull();
		metadata!.PrNumber.Should().Be(42);
		metadata.HeadRef.Should().Be("feature/test");
		metadata.Status.Should().Be("ok");
	}

	[Fact]
	public async Task ValidateLabels_NotOnCI_DoesNotWriteMetadataFile()
	{
		await WriteConfig(MinimalConfig);
		var env = A.Fake<IEnvironmentVariables>();
		A.CallTo(() => env.IsRunningOnCI).Returns(false);

		await CreateService(env).ValidateLabels(Collector, DefaultArgs(["type:feature"], prNumber: 42), CancellationToken.None);

		RunnerTempFileSystem.File.Exists(MetadataPath).Should().BeFalse();
	}

	[Fact]
	public async Task ValidateLabels_OnCI_NoPrNumber_DoesNotWriteMetadataFile()
	{
		await WriteConfig(MinimalConfig);
		var env = A.Fake<IEnvironmentVariables>();
		A.CallTo(() => env.IsRunningOnCI).Returns(true);

		await CreateService(env).ValidateLabels(Collector, DefaultArgs(["type:feature"], prNumber: 0), CancellationToken.None);

		RunnerTempFileSystem.File.Exists(MetadataPath).Should().BeFalse();
	}

	[Fact]
	public async Task ValidateLabels_NoLabel_OnCI_WritesMetadataWithNoLabelStatus()
	{
		await WriteConfig(MinimalConfig);
		var env = A.Fake<IEnvironmentVariables>();
		A.CallTo(() => env.IsRunningOnCI).Returns(true);

		await CreateService(env).ValidateLabels(Collector, DefaultArgs([], prNumber: 42), CancellationToken.None);

		var metadata = await ReadMetadata();
		metadata.Should().NotBeNull();
		metadata!.PrNumber.Should().Be(42);
		metadata.Status.Should().Be("no-label");
	}
}
