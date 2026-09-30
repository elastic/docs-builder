// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Publishing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Configuration.Tests;

public class UseNavigationPreviewTests
{
	private static ConfigurationFileProvider CreateProvider(MockFileSystem fileSystem) =>
		new(
			NullLoggerFactory.Instance,
			new ConfigurationFileSystem(fileSystem),
			skipPrivateRepositories: true,
			ConfigurationSource.Embedded
		);

	private static AssemblyConfiguration CreateConfig(params string[] privateRepoNames)
	{
		var refsYaml = string.Join("\n", privateRepoNames.Select(name => $"  {name}:\n    private: true"));
		var yaml = $"narrative:\n  repo: git@github.com:elastic/docs-content.git\nreferences:\n{refsYaml}";
		return AssemblyLoader.Deserialize(yaml, skipPrivateRepositories: true);
	}

	private static readonly string[] RequiredFiles =
	[
		"versions.yml",
		"products.yml",
		"assembler.yml",
		"navigation.yml",
		"legacy-url-mappings.yml",
		"search.yml",
		"related-learning.yml"
	];

	private static readonly string ConfigDirectory = ConfigurationFileProvider.AppDataConfigurationDirectory;

	private static ConfigurationFileProvider CreateRemoteProvider(MockFileSystem fileSystem)
	{
		foreach (var file in RequiredFiles)
			fileSystem.AddFile(Path.Join(ConfigDirectory, file), new MockFileData($"# {file}"));
		fileSystem.AddFile(Path.Join(ConfigDirectory, "git-ref.txt"), new MockFileData("test"));
		return new ConfigurationFileProvider(
			NullLoggerFactory.Instance,
			new ConfigurationFileSystem(fileSystem),
			false,
			ConfigurationSource.Remote
		);
	}

	[Test]
	public void RemoteConfiguration_IsReadInPlace()
	{
		var provider = CreateRemoteProvider(new MockFileSystem());

		provider.NavigationFile.FullName.Should().Be(Path.Join(ConfigDirectory, "navigation.yml"));
		provider.AssemblerFile.FullName.Should().Be(Path.Join(ConfigDirectory, "assembler.yml"));
	}

	[Test]
	public void NavigationPreviewFile_ReadsThePreviewWhenPresent()
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddFile(Path.Join(ConfigDirectory, "navigation_preview.yml"), new MockFileData("# preview"));
		var provider = CreateRemoteProvider(fileSystem);

		provider.NavigationPreviewFile.Name.Should().Be("navigation_preview.yml");
		provider.NavigationFile.Name.Should().Be("navigation.yml", "selecting the preview never repoints the provider's navigation.yml");
	}

	[Test]
	public void NavigationPreviewFile_FallsBackToNavigationWhenAbsent() =>
		CreateRemoteProvider(new MockFileSystem()).NavigationPreviewFile.Name.Should().Be("navigation.yml");

	[Test]
	public void NavigationPreviewEnabled_ReadsUnderscoredEnvironmentKey()
	{
		// Regression guard: the ctor-doesn't-normalize trap.
		// PublishEnvironmentDto.FeatureFlags keys use UPPER_SNAKE yaml convention; FeatureFlags.IsEnabled
		// looks up normalized lower-kebab keys. ToFeatureFlags() must bridge this via Set().
		var env = new PublishEnvironment { FeatureFlags = new Dictionary<string, bool> { ["NAVIGATION_PREVIEW"] = true } };

		var flags = env.ToFeatureFlags();
		flags.NavigationPreviewEnabled.Should().BeTrue("ToFeatureFlags() normalizes UPPER_SNAKE keys through Set() before storing them");
	}

	[Test]
	public void NavigationPreviewEnabled_FalseWhenNotSet()
	{
		var env = new PublishEnvironment { FeatureFlags = new Dictionary<string, bool>() };

		var flags = env.ToFeatureFlags();
		flags.NavigationPreviewEnabled.Should().BeFalse("flag must be inert when not declared in the environment");
	}

	[Test]
	public void ToFeatureFlags_DoesNotAffectOtherFlags()
	{
		// NAVIGATION_PREVIEW enabled must not accidentally enable sibling flags
		var env = new PublishEnvironment { FeatureFlags = new Dictionary<string, bool> { ["NAVIGATION_PREVIEW"] = true } };

		var flags = env.ToFeatureFlags();
		flags.WebsiteSearchEnabled.Should().BeFalse();
		flags.AirGappedEnabled.Should().BeFalse();
		flags.PrimaryNavEnabled.Should().BeFalse();
	}

	[Test]
	public void PreviewNavigation_FilterForAssembly_StripsPrivateRepos()
	{
		// The ordering guarantee: private-repo filtering is applied to the preview content,
		// and island: lines on public entries survive.
		var fileSystem = new MockFileSystem();
		var provider = CreateProvider(fileSystem);

		// language=yaml
		var previewYaml =
			"""
		                  toc:
		                    - toc: public-repo://reference
		                      path_prefix: reference
		                      island: true
		                    - toc: private-a://reference
		                      path_prefix: private-ref
		                  """;

		fileSystem.File.WriteAllText(provider.NavigationFile.FullName, previewYaml);

		var config = CreateConfig("private-a");
		var output = SiteNavigationText.Describe(
			SiteNavigationLoader.ForAssembly(
				SiteNavigationLoader.Deserialize(fileSystem.File.ReadAllText(provider.NavigationFile.FullName)),
				config,
				skipPrivateRepositories: true
			)
		);

		output.Should().NotContain("private-a://");
		output.Should().Contain("public-repo://reference");
		output.Should().Contain("island: true", "island marker on public entry survives private-repo stripping");
	}
}
