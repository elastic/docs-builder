// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration;

namespace Elastic.Changelog.Tests.Changelogs;

public class BundleDescriptionInputTests : ChangelogTestBase
{
	public BundleDescriptionInputTests(ITestOutputHelper output) : base(output) { }

	[Fact]
	public async Task ResolveAsync_NoSource_ReturnsNone()
	{
		var result = await BundleDescriptionInput.ResolveAsync(
			Collector,
			FileSystem,
			null,
			null,
			clearDescription: false,
			stdin: null,
			TestContext.Current.CancellationToken
		);

		result.Success.Should().BeTrue();
		result.HasPatch.Should().BeFalse();
		result.Value.Should().BeNull();
	}

	[Fact]
	public async Task ResolveAsync_ClearDescription_ReturnsEmptyPatch()
	{
		var result = await BundleDescriptionInput.ResolveAsync(
			Collector,
			FileSystem,
			null,
			null,
			clearDescription: true,
			stdin: null,
			TestContext.Current.CancellationToken
		);

		result.Should().Be(BundleDescriptionInputResult.Patch(string.Empty));
	}

	[Fact]
	public async Task ResolveAsync_DescriptionAndFile_Fails()
	{
		var result = await BundleDescriptionInput.ResolveAsync(
			Collector,
			FileSystem,
			"inline",
			"/tmp/desc.md",
			clearDescription: false,
			stdin: null,
			TestContext.Current.CancellationToken
		);

		result.Success.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task ResolveAsync_ReadsFileAndTrimsTrailingNewline()
	{
		var path = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "desc.md");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(path)!);
		await FileSystem.File.WriteAllTextAsync(path, "Line one\nLine two\n", TestContext.Current.CancellationToken);

		var result = await BundleDescriptionInput.ResolveAsync(
			Collector,
			FileSystem,
			null,
			path,
			clearDescription: false,
			stdin: null,
			TestContext.Current.CancellationToken
		);

		result.Success.Should().BeTrue();
		result.HasPatch.Should().BeTrue();
		result.Value.Should().Be("Line one\nLine two");
	}

	[Fact]
	public async Task ResolveAsync_Stdin_ReadsReader()
	{
		using var stdin = new StringReader("From stdin\n");
		var result = await BundleDescriptionInput.ResolveAsync(
			Collector,
			FileSystem,
			null,
			BundleDescriptionInput.StdinPath,
			clearDescription: false,
			stdin,
			TestContext.Current.CancellationToken
		);

		result.Success.Should().BeTrue();
		result.Value.Should().Be("From stdin");
	}

	[Fact]
	public async Task ResolveAsync_MissingFile_Fails()
	{
		var result = await BundleDescriptionInput.ResolveAsync(
			Collector,
			FileSystem,
			null,
			"/does-not-exist.md",
			clearDescription: false,
			stdin: null,
			TestContext.Current.CancellationToken
		);

		result.Success.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
	}
}
