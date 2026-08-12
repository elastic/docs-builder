// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class DuplicateHandlingTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
	public async Task RenderChangelogs_WithDuplicateFileName_EmitsWarning()
	{
		// Arrange
		// Same changelog entry bundled under the same file name in two bundles
		// language=yaml
		var changelog =
			"""
			title: Duplicate feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "100"
			""";

		var fileName = "1755268130-duplicate.yaml";

		// Create bundle files
		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		// language=yaml
		var bundleHeader =
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";

		var bundle1 = FileSystem.Path.Join(bundleDir, "bundle1.yaml");
		var bundleContent1 = CreateResolvedBundleContent(bundleHeader, (fileName, changelog));
		await FileSystem.File.WriteAllTextAsync(bundle1, bundleContent1, TestContext.Current.CancellationToken);

		var bundle2 = FileSystem.Path.Join(bundleDir, "bundle2.yaml");
		var bundleContent2 = CreateResolvedBundleContent(bundleHeader, (fileName, changelog));
		await FileSystem.File.WriteAllTextAsync(bundle2, bundleContent2, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles =
			[
				new BundleInput { BundleFile = bundle1 },
				new BundleInput { BundleFile = bundle2 }
			],
			Output = outputDir
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("appears in multiple bundles"));
	}

	[Fact]
	public async Task RenderChangelogs_WithDuplicateFileNameInSameBundle_EmitsWarning()
	{
		// Arrange
		// language=yaml
		var changelog =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "100"
			""";

		var fileName = "1755268130-test-feature.yaml";

		// Create bundle file with the same file name inlined twice
		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleHeader =
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, (fileName, changelog), (fileName, changelog));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles =
			[
				new BundleInput { BundleFile = bundleFile }
			],
			Output = outputDir
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("appears multiple times in the same bundle") &&
			d.File == bundleFile);
	}

	[Fact]
	public async Task RenderChangelogs_WithDuplicatePr_EmitsWarning()
	{
		// Arrange
		// Create changelog entries with same PR
		// language=yaml
		var changelog1 =
			"""
			title: First feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "100"
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Second feature
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "100"
			""";

		// Create bundle files
		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		// language=yaml
		var bundleHeader =
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";

		var bundle1 = FileSystem.Path.Join(bundleDir, "bundle1.yaml");
		var bundleContent1 = CreateResolvedBundleContent(bundleHeader, ("1755268130-first.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundle1, bundleContent1, TestContext.Current.CancellationToken);

		var bundle2 = FileSystem.Path.Join(bundleDir, "bundle2.yaml");
		var bundleContent2 = CreateResolvedBundleContent(bundleHeader, ("1755268140-second.yaml", changelog2));
		await FileSystem.File.WriteAllTextAsync(bundle2, bundleContent2, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles =
			[
				new BundleInput { BundleFile = bundle1 },
				new BundleInput { BundleFile = bundle2 }
			],
			Output = outputDir
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("appears in multiple bundles"));
	}
}
