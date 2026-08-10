// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
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

/// <summary>
/// End-to-end check that the assembler OpenAPI step can generate HTML from the live version index.
/// Requires network access to CloudFront.
/// </summary>
public class AssemblerOpenApiBuildStepIntegrationTests
{
	[Fact]
	public async Task BuildAsync_GeneratesApiPagesWhenFlagEnabledAndDocsetPresent()
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows())
			return;

		var solutionDirectory = Paths.GetSolutionDirectory();
		if (solutionDirectory is null)
			return;

		var previousDirectory = Directory.GetCurrentDirectory();
		var outputRoot = Directory.CreateTempSubdirectory("assembler-openapi-integration-");
		try
		{
			Directory.SetCurrentDirectory(solutionDirectory.FullName);

			var fileSystem = new FileSystem();
			var collector = new DiagnosticsCollector([]);
			var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
			var assemblyConfig = AssemblyConfiguration.Create(configurationContext.ConfigurationFileProvider);
			var scopedFs = FileSystemFactory.ScopeCurrentWorkingDirectory(fileSystem);
			var outputDirectory = fileSystem.Path.Join(outputRoot.FullName, "output");
			var context = new AssembleContext(
				assemblyConfig,
				configurationContext,
				"staging",
				collector,
				scopedFs,
				scopedFs,
				solutionDirectory.FullName,
				outputDirectory);

			var stopwatch = Stopwatch.StartNew();
			await AssemblerOpenApiBuildStep.BuildAsync(
				NullLoggerFactory.Instance,
				context,
				[],
				configurationContext,
				TestContext.Current.CancellationToken);
			stopwatch.Stop();

			TestContext.Current.TestOutputHelper?.WriteLine(
				$"OpenAPI assembler step completed in {stopwatch.ElapsedMilliseconds} ms");

			var apiRoot = fileSystem.Path.Join(outputDirectory, "docs", "api");
			fileSystem.Directory.Exists(apiRoot).Should().BeTrue();

			var elasticsearchLanding = fileSystem.Path.Join(apiRoot, "doc", "elasticsearch", "index.html");
			fileSystem.File.Exists(elasticsearchLanding).Should().BeTrue(
				"staging assembler builds should emit the unversioned elasticsearch API landing page");

			var versionedLanding = fileSystem.Directory
				.EnumerateDirectories(fileSystem.Path.Join(apiRoot, "doc", "elasticsearch"))
				.FirstOrDefault(path => fileSystem.Path.GetFileName(path).StartsWith('v'));
			versionedLanding.Should().NotBeNull(
				"versioned products should emit at least one /vN/ tree under /docs/api/doc/elasticsearch/");
		}
		finally
		{
			Directory.SetCurrentDirectory(previousDirectory);
			outputRoot.Delete(recursive: true);
		}
	}
}
