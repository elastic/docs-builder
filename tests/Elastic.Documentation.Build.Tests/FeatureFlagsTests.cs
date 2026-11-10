// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.FileSystems;

namespace Elastic.Documentation.Build.Tests;

// The environment-variable override test mutates process-wide state that every flag-reading test here depends on
[NotInParallel(nameof(FeatureFlagsTests))]
public class FeatureFlagsTests
{
	[Test]
	public void AssemblerApiExplorerEnabled_ReadsYamlKey()
	{
		var flags = new FeatureFlags([]);
		flags.Set("ASSEMBLER_API_EXPLORER", true);

		flags.AssemblerApiExplorerEnabled.Should().BeTrue();
	}

	[Test]
	public void AssemblerApiExplorerEnabled_DefaultsToFalse()
	{
		var flags = new FeatureFlags([]);

		flags.AssemblerApiExplorerEnabled.Should().BeFalse();
	}

	[Test]
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

	[Test]
	public void StagingEnvironment_EnablesAssemblerApiExplorer() => AssertEnvironmentEnablesAssemblerApiExplorer("staging");

	[Test]
	public void PreviewEnvironment_EnablesAssemblerApiExplorer() => AssertEnvironmentEnablesAssemblerApiExplorer("preview");

	[Test]
	public void EdgeEnvironment_EnablesAssemblerApiExplorer() => AssertEnvironmentEnablesAssemblerApiExplorer("edge");

	[Test]
	public void DemoEnvironment_EnablesAssemblerApiExplorer() => AssertEnvironmentEnablesAssemblerApiExplorer("demo");

	[Test]
	public void AirGappedEnvironment_EnablesAssemblerApiExplorer() => AssertEnvironmentEnablesAssemblerApiExplorer("air-gapped");

	[Test]
	public void EdgeEnvironment_DoesNotAllowIndexing()
	{
		var config = AssemblyConfiguration.Create(new ConfigurationFileProvider(new TestLoggerFactory(), new ConfigurationFileSystem()));

		config.Environments["edge"].AllowIndexing.Should().BeFalse();
	}

	[Test]
	public void EdgeEnvironment_DisablesNavigationPreview()
	{
		var config = AssemblyConfiguration.Create(new ConfigurationFileProvider(new TestLoggerFactory(), new ConfigurationFileSystem()));

		config.Environments["edge"].FeatureFlags.Should().ContainKey("NAVIGATION_PREVIEW").WhoseValue.Should().BeFalse();
	}

	[Test]
	public void DemoEnvironment_DoesNotAllowIndexing()
	{
		var config = AssemblyConfiguration.Create(new ConfigurationFileProvider(new TestLoggerFactory(), new ConfigurationFileSystem()));

		config.Environments["demo"].AllowIndexing.Should().BeFalse();
	}

	[Test]
	public void DemoEnvironment_EnablesNavigationPreview()
	{
		var config = AssemblyConfiguration.Create(new ConfigurationFileProvider(new TestLoggerFactory(), new ConfigurationFileSystem()));

		config.Environments["demo"].FeatureFlags.Should().ContainKey("NAVIGATION_PREVIEW").WhoseValue.Should().BeTrue();
	}

	[Test]
	[Arguments("prod")]
	[Arguments("staging")]
	[Arguments("dev")]
	[Arguments("local")]
	public void OtherEnvironments_DoNotEnableNavigationPreview(string environmentName)
	{
		var config = AssemblyConfiguration.Create(new ConfigurationFileProvider(new TestLoggerFactory(), new ConfigurationFileSystem()));

		config.Environments[environmentName].FeatureFlags.GetValueOrDefault("NAVIGATION_PREVIEW").Should().BeFalse();
	}

	private static void AssertEnvironmentEnablesAssemblerApiExplorer(string environmentName)
	{
		var config = AssemblyConfiguration.Create(new ConfigurationFileProvider(new TestLoggerFactory(), new ConfigurationFileSystem()));
		var environment = config.Environments[environmentName];

		environment.FeatureFlags.Should().ContainKey("ASSEMBLER_API_EXPLORER").WhoseValue.Should().BeTrue();

		var features = new FeatureFlags([]);
		foreach (var (key, value) in environment.FeatureFlags)
			features.Set(key, value);
		features.AssemblerApiExplorerEnabled.Should().BeTrue();
	}

	[Test]
	public void EnabledKeys_CoversEveryTypedFlag()
	{
		var flagProperties = typeof(FeatureFlags).GetProperties().Where(p => p.PropertyType == typeof(bool) && p.CanWrite).ToList();
		flagProperties.Should().NotBeEmpty();

		var seen = new List<string>();
		foreach (var property in flagProperties)
		{
			var flags = new FeatureFlags([]);
			property.SetValue(flags, true);

			var enabled = flags.EnabledKeys();
			enabled
				.Should()
				.ContainSingle($"{property.Name} is one flag and enables one key")
				.Which
				.Should()
				.BeOneOf(FeatureFlags.KnownKeys);
			seen.AddRange(enabled);
		}

		seen.Should().BeEquivalentTo(FeatureFlags.KnownKeys, "a typed flag missing from KnownKeys would never reach the page");
		FeatureFlags.KnownKeys.Should().BeInAscendingOrder(StringComparer.Ordinal);
	}

	[Test]
	public void EnabledKeys_DefaultsToNone() => new FeatureFlags([]).EnabledKeys().Should().BeEmpty();

	[Test]
	public void EnabledKeys_EnvironmentVariableOverridesYaml()
	{
		var previous = Environment.GetEnvironmentVariable("FEATURE_PAGE_FEEDBACK");
		try
		{
			Environment.SetEnvironmentVariable("FEATURE_PAGE_FEEDBACK", "false");
			var flags = new FeatureFlags(new Dictionary<string, bool> { ["page-feedback"] = true, ["guide-nav"] = true });

			flags.EnabledKeys().Should().Equal("guide-nav");
		}
		finally
		{
			Environment.SetEnvironmentVariable("FEATURE_PAGE_FEEDBACK", previous);
		}
	}

	[Test]
	public void ProdEnvironment_DoesNotEnableAssemblerApiExplorer()
	{
		var config = AssemblyConfiguration.Create(new ConfigurationFileProvider(new TestLoggerFactory(), new ConfigurationFileSystem()));
		var prod = config.Environments["prod"];

		prod.FeatureFlags.Should().NotContainKey("ASSEMBLER_API_EXPLORER");

		var features = new FeatureFlags([]);
		foreach (var (key, value) in prod.FeatureFlags)
			features.Set(key, value);
		features.AssemblerApiExplorerEnabled.Should().BeFalse();
	}
}
