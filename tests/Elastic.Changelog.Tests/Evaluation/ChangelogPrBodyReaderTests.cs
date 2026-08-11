// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Changelog.Evaluation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;

namespace Elastic.Changelog.Tests.Evaluation;

public class ChangelogPrBodyReaderTests(ITestOutputHelper output)
{
	[Fact]
	public async Task ReadAsync_PrBodyFileUnderWorkingDir_ReadsBody()
	{
		var bodyPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "changelog-pr-body.md");
		var mockFs = CreateMockFileSystem();
		mockFs.AddFile(bodyPath, new MockFileData("Release Notes: adds billing metadata"));
		var collector = new TestDiagnosticsCollector(output);

		var result = await ChangelogPrBodyReader.ReadAsync(bodyPath, collector, mockFs, TestContext.Current.CancellationToken);

		result.Should().Be("Release Notes: adds billing metadata");
		collector.Diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task ReadAsync_PrBodyFileMissing_EmitsWarning()
	{
		var bodyPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "missing-pr-body.md");
		var scopedFs = CheckoutsFileSystem.FromWorkingDirectory(CreateMockFileSystem());
		var collector = new TestDiagnosticsCollector(output);

		var result = await ChangelogPrBodyReader.ReadAsync(bodyPath, collector, scopedFs, TestContext.Current.CancellationToken);

		result.Should().BeNull();
		collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("points to a missing file", StringComparison.Ordinal));
	}

	[Fact]
	public async Task ReadAsync_PrBodyFileOutsideScope_EmitsWarning()
	{
		var runnerTemp = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
		var bodyPath = Path.Join(runnerTemp, "changelog-pr-body.md");
		var mockFs = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName });
		mockFs.AddFile(bodyPath, new MockFileData("Release Notes: something important"));
		var scopedFs = CheckoutsFileSystem.FromWorkingDirectory(mockFs);
		var collector = new TestDiagnosticsCollector(output);

		var result = await ChangelogPrBodyReader.ReadAsync(bodyPath, collector, scopedFs, TestContext.Current.CancellationToken);

		result.Should().BeNull();
		collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("PR_BODY_FILE", StringComparison.Ordinal));
	}

	[Fact]
	public async Task ReadAsync_PrBodyFileExceedsMaxSize_TruncatesAndHints()
	{
		var bodyPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "large-pr-body.md");
		var largeContent = new string('x', ChangelogPrBodyReader.MaxPrBodyFileBytes + 1000);
		var mockFs = CreateMockFileSystem();
		mockFs.AddFile(bodyPath, new MockFileData(largeContent));
		var collector = new TestDiagnosticsCollector(output);

		var result = await ChangelogPrBodyReader.ReadAsync(bodyPath, collector, mockFs, TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		result.Length.Should().Be(ChangelogPrBodyReader.MaxPrBodyFileBytes);
		collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("exceeds", StringComparison.Ordinal));
	}

	private static MockFileSystem CreateMockFileSystem() =>
		new(new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName });
}
