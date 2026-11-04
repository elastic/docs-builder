// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration;

namespace Elastic.Changelog.Tests.Changelogs;

public class BundleDescriptionInputTests() : ChangelogTestBase()
{
	private async Task<BundleDescriptionInputResult> ResolveAsync(
		string? description = null,
		string? descriptionFile = null,
		bool clearDescription = false,
		TextReader? stdin = null
	) =>
		await BundleDescriptionInput.ResolveAsync(
			Collector,
			FileSystem,
			new BundleDescriptionRequest(description, descriptionFile, ClearDescription: clearDescription, Stdin: stdin),
			TestContext.Current!.Execution.CancellationToken
		);

	[Test]
	public async Task ResolveAsync_NoSource_ReturnsNone()
	{
		var result = await ResolveAsync();

		result.Success.Should().BeTrue();
		result.HasPatch.Should().BeFalse();
		result.Value.Should().BeNull();
	}

	[Test]
	public async Task ResolveAsync_ClearDescription_ReturnsEmptyPatch()
	{
		var result = await ResolveAsync(clearDescription: true);

		result.Should().Be(BundleDescriptionInputResult.Patch(string.Empty));
	}

	[Test]
	public async Task ResolveAsync_EmptyDescription_ReturnsEmptyPatch()
	{
		// An empty --description is still an explicitly supplied source: profile mode must be able to
		// tell it apart from "no description flag at all" when rejecting a config/CLI collision.
		var result = await ResolveAsync(description: string.Empty);

		result.Success.Should().BeTrue();
		result.HasPatch.Should().BeTrue();
		result.Value.Should().BeEmpty();
	}

	[Test]
	public async Task ResolveAsync_DescriptionAndFile_Fails()
	{
		var result = await ResolveAsync("inline", "/tmp/desc.md");

		result.Success.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
	}

	[Test]
	public async Task ResolveAsync_ReadsFileAndTrimsTrailingNewline()
	{
		var path = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "desc.md");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(path)!);
		await FileSystem.File.WriteAllTextAsync(path, "Line one\nLine two\n", TestContext.Current!.Execution.CancellationToken);

		var result = await ResolveAsync(descriptionFile: path);

		result.Success.Should().BeTrue();
		result.HasPatch.Should().BeTrue();
		result.Value.Should().Be("Line one\nLine two");
	}

	[Test]
	public async Task ResolveAsync_Stdin_ReadsReader()
	{
		using var stdin = new StringReader("From stdin\n");

		var result = await ResolveAsync(descriptionFile: BundleDescriptionInput.StdinPath, stdin: stdin);

		result.Success.Should().BeTrue();
		result.Value.Should().Be("From stdin");
	}

	[Test]
	public async Task ResolveAsync_MissingFile_Fails()
	{
		var result = await ResolveAsync(descriptionFile: "/does-not-exist.md");

		result.Success.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
	}

	[Test]
	public async Task ResolveAsync_EmptyDescriptionFile_Fails()
	{
		// A CI variable that expands to nothing must not look like "no description flag was passed",
		// which would silently keep the config intro in profile mode.
		var result = await ResolveAsync(descriptionFile: "   ");

		result.Success.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("--description-file requires a path"));
	}

	[Test]
	public async Task ResolveAsync_DescriptionAndEmptyFile_Fails()
	{
		var result = await ResolveAsync(string.Empty, string.Empty);

		result.Success.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("mutually exclusive"));
	}

	[Test]
	public async Task ResolveAsync_FileWithNulByte_Fails()
	{
		// A UTF-16 file saved with no byte order mark decodes as UTF-8 into NUL-separated characters.
		// Accepting it would put an unreadable intro in a published bundle.
		var path = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "desc.md");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(path)!);
		await FileSystem.File.WriteAllTextAsync(
			path,
			"Line one\n" + (char)0 + "Line two",
			TestContext.Current!.Execution.CancellationToken
		);

		var result = await ResolveAsync(descriptionFile: path);

		result.Success.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("NUL byte"));
	}

	[Test]
	public async Task ResolveAsync_StdinWithNulByte_Fails()
	{
		using var stdin = new StringReader("Line one\n" + (char)0 + "Line two");

		var result = await ResolveAsync(descriptionFile: BundleDescriptionInput.StdinPath, stdin: stdin);

		result.Success.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("standard input"));
	}
}
