// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Documentation.Assembler;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Build.Tests;

public class AssemblerOpenApiBuildStepTests
{
	private static readonly string MinimalAssemblerYaml = """
		environments:
		  prod:
		    uri: https://www.elastic.co
		    path_prefix: docs
		    content_source: current
		  staging:
		    uri: https://staging-website.elastic.co
		    path_prefix: docs
		    content_source: next
		    feature_flags:
		      ASSEMBLER_API_EXPLORER: true
		narrative:
		  checkout_strategy: full
		references: {}
		""";

	[Fact]
	public async Task BuildAsync_SkipsWhenFeatureFlagDisabled()
	{
		var previousDirectory = Directory.GetCurrentDirectory();
		var tempDirectory = Directory.CreateTempSubdirectory("assembler-openapi-test-");
		try
		{
			Directory.SetCurrentDirectory(tempDirectory.FullName);

			var fileSystem = new FileSystem();
			var collector = new DiagnosticsCollector([]);
			var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
			var assemblyConfig = AssemblyConfiguration.Deserialize(MinimalAssemblerYaml);
			var scopedFs = FileSystemFactory.ScopeCurrentWorkingDirectory(fileSystem);
			var outputDirectory = fileSystem.Path.Join(tempDirectory.FullName, "output");
			var context = new AssembleContext(
				assemblyConfig,
				configurationContext,
				"prod",
				collector,
				scopedFs,
				scopedFs,
				tempDirectory.FullName,
				outputDirectory);

			await AssemblerOpenApiBuildStep.BuildAsync(
				NullLoggerFactory.Instance,
				context,
				[],
				configurationContext,
				TestContext.Current.CancellationToken);

			fileSystem.Directory.Exists(fileSystem.Path.Join(outputDirectory, "docs", "api"))
				.Should().BeFalse("OpenAPI generation must not run when the feature flag is disabled");
		}
		finally
		{
			Directory.SetCurrentDirectory(previousDirectory);
			tempDirectory.Delete(recursive: true);
		}
	}

	[Fact]
	public async Task BuildAsync_SkipsWhenDocsetNotFoundAndFlagEnabled()
	{
		var previousDirectory = Directory.GetCurrentDirectory();
		var tempDirectory = Directory.CreateTempSubdirectory("assembler-openapi-test-");
		try
		{
			Directory.SetCurrentDirectory(tempDirectory.FullName);

			var fileSystem = new FileSystem();
			var collector = new DiagnosticsCollector([]);
			var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
			var assemblyConfig = AssemblyConfiguration.Deserialize(MinimalAssemblerYaml);
			var scopedFs = FileSystemFactory.ScopeCurrentWorkingDirectory(fileSystem);
			var outputDirectory = fileSystem.Path.Join(tempDirectory.FullName, "output");
			var context = new AssembleContext(
				assemblyConfig,
				configurationContext,
				"staging",
				collector,
				scopedFs,
				scopedFs,
				tempDirectory.FullName,
				outputDirectory);

			await AssemblerOpenApiBuildStep.BuildAsync(
				NullLoggerFactory.Instance,
				context,
				[],
				configurationContext,
				TestContext.Current.CancellationToken);

			fileSystem.Directory.Exists(fileSystem.Path.Join(outputDirectory, "docs", "api"))
				.Should().BeFalse("OpenAPI generation must not run without a docs-builder docset checkout");
		}
		finally
		{
			Directory.SetCurrentDirectory(previousDirectory);
			tempDirectory.Delete(recursive: true);
		}
	}
}
