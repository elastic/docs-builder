// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Tests.Golden;
using Elastic.Documentation.Diagnostics;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Configuration.Tests;

// Property tests document their law and its consequence on purpose: a generated input is invisible and a law is not a
// value, so the doc comment is the only place a reader learns what is guaranteed and why it matters.

/// <summary>Named generators for the table of contents property tests.</summary>
internal static class TocGens
{
	/// <summary>
	/// A chain of nested tables of contents, t0/t1/…/tN-1, whose last toc.yml points back at the ancestor at BackTo.
	/// </summary>
	public static Gen<(int Depth, int BackTo)> Cycle { get; } = Gen.Int[1, 6].SelectMany(
		depth => Gen.Int[0, depth - 1].Select(backTo => (depth, backTo))
	);
}

public class TableOfContentsProperties
{
	/// <summary>
	/// Law: however deep a chain of nested toc.yml files, and whichever ancestor its last file points back to, loading
	/// terminates and reports the loop exactly once, as an error.
	/// Consequence if broken: the loader recurses until the process dies with a stack overflow, with no diagnostic
	/// pointing at the file that closes the loop.
	/// </summary>
	[Test]
	public void TocCycles_AreReportedOnceAndLoadingTerminates() =>
		TocGens.Cycle.Sample(
			cycle =>
			{
				var collector = new CapturingDiagnosticsCollector();
				var fileSystem = ChainWithCycle(cycle.Depth, cycle.BackTo);

				_ = DocumentationSetLoader.LoadAndResolve(
					collector,
					fileSystem.FileInfo.New("/docs/docset.yml"),
					new ScopedFileSystem(fileSystem, "/docs")
				);

				collector
					.Diagnostics
					.Should()
					.ContainSingle(d => d.Message.Contains("includes itself"))
					.Which
					.Severity
					.Should()
					.Be(Severity.Error);
			},
			print: cycle => $"depth {cycle.Depth}, last toc.yml points back to t{cycle.BackTo}"
		);

	private static MockFileSystem ChainWithCycle(int depth, int backTo)
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/docset.yml", new MockFileData("project: test\ntoc:\n  - file: index.md\n  - toc: t0\n"));
		var directory = "/docs/t0";
		for (var level = 0; level < depth; level++)
		{
			var next = level == depth - 1 ? string.Concat(Enumerable.Repeat("../", depth - backTo)) + $"t{backTo}" : $"t{level + 1}";
			fileSystem.AddFile($"{directory}/toc.yml", new MockFileData($"toc:\n  - file: index.md\n  - toc: {next}\n"));
			directory += $"/t{level + 1}";
		}
		return fileSystem;
	}
}
