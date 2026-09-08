// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.FileSystems;

namespace Elastic.Documentation.Build.Tests;

public class FeatureFlagsTests
{
	[Fact]
	public void AssemblerApiExplorerEnabled_ReadsYamlKey()
	{
		var flags = new FeatureFlags([]);
		flags.Set("ASSEMBLER_API_EXPLORER", true);

		flags.AssemblerApiExplorerEnabled.Should().BeTrue();
	}

	[Fact]
	public void AssemblerApiExplorerEnabled_DefaultsToFalse()
	{
		var flags = new FeatureFlags([]);

		flags.AssemblerApiExplorerEnabled.Should().BeFalse();
	}

	[Fact]
	public void AssemblerApiExplorerEnabled_EnvironmentVariableOverridesYaml()
	{
		var previous = Environment.GetEnvironmentVariable("FEATURE_ASSEMBLER_API_EXPLORER");
		try
		{
			Environment.SetEnvironmentVariable("FEATURE_ASSEMBLER_API_EXPLORER", "false");
			var flags = new FeatureFlags(new Dictionary<string, bool> { ["assembler-api-explorer"] = true });

			flags.AssemblerApiExplorerEnabled.Should().BeFalse();
		}
		finally
		{
			Environment.SetEnvironmentVariable("FEATURE_ASSEMBLER_API_EXPLORER", previous);
		}
	}

	[Fact]
	public void StagingEnvironment_EnablesAssemblerApiExplorer() => AssertEnvironmentEnablesAssemblerApiExplorer("staging");

	[Fact]
	public void PreviewEnvironment_EnablesAssemblerApiExplorer() => AssertEnvironmentEnablesAssemblerApiExplorer("preview");

	private static void AssertEnvironmentEnablesAssemblerApiExplorer(string environmentName)
	{
		var config = AssemblyConfiguration.Create(
			new ConfigurationFileProvider(new TestLoggerFactory(null), new ConfigurationFileSystem())
		);
		var environment = config.Environments[environmentName];

		environment.FeatureFlags.Should().ContainKey("ASSEMBLER_API_EXPLORER").WhoseValue.Should().BeTrue();

		var features = new FeatureFlags([]);
		foreach (var (key, value) in environment.FeatureFlags)
			features.Set(key, value);
		features.AssemblerApiExplorerEnabled.Should().BeTrue();
	}

	[Fact]
	public void ProdEnvironment_DoesNotEnableAssemblerApiExplorer()
	{
		var config = AssemblyConfiguration.Create(
			new ConfigurationFileProvider(new TestLoggerFactory(null), new ConfigurationFileSystem())
		);
		var prod = config.Environments["prod"];

		prod.FeatureFlags.Should().NotContainKey("ASSEMBLER_API_EXPLORER");

		var features = new FeatureFlags([]);
		foreach (var (key, value) in prod.FeatureFlags)
			features.Set(key, value);
		features.AssemblerApiExplorerEnabled.Should().BeFalse();
	}

	[Fact]
	public void PageFeedbackEnabled_DefaultsToFalse()
	{
		var flags = new FeatureFlags([]);

		flags.PageFeedbackEnabled.Should().BeFalse();
	}

	[Theory]
	[InlineData("edge")]
	[InlineData("staging")]
	[InlineData("dev")]
	[InlineData("preview")]
	public void Environment_EnablesPageFeedback(string environmentName)
	{
		var config = AssemblyConfiguration.Create(
			new ConfigurationFileProvider(new TestLoggerFactory(null), new ConfigurationFileSystem())
		);
		var environment = config.Environments[environmentName];

		environment.FeatureFlags.Should().ContainKey("PAGE_FEEDBACK").WhoseValue.Should().BeTrue();
		environment.ToFeatureFlags().PageFeedbackEnabled.Should().BeTrue();
	}

	[Fact]
	public void EdgeEnvironment_LeavesNavigationPreviewDisabled()
	{
		var config = AssemblyConfiguration.Create(
			new ConfigurationFileProvider(new TestLoggerFactory(null), new ConfigurationFileSystem())
		);
		var edge = config.Environments["edge"];

		edge.FeatureFlags.Should().NotContainKey("NAVIGATION_PREVIEW");
		edge.ToFeatureFlags().NavigationPreviewEnabled.Should().BeFalse();
	}

	[Fact]
	public void ProdEnvironment_DoesNotEnablePageFeedback()
	{
		var config = AssemblyConfiguration.Create(
			new ConfigurationFileProvider(new TestLoggerFactory(null), new ConfigurationFileSystem())
		);
		var prod = config.Environments["prod"];

		prod.FeatureFlags.Should().NotContainKey("PAGE_FEEDBACK");
		prod.ToFeatureFlags().PageFeedbackEnabled.Should().BeFalse();
	}
}
