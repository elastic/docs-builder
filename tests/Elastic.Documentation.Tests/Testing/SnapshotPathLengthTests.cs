// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Documentation.Tests.Testing;

/// <summary>
/// A verified snapshot is named after its test class and method, and a failed run writes the received file under
/// <c>.artifacts/.snapshots/received/{Project}/</c>, a longer path. Windows stops at 260 characters, and git on the CI runner
/// cannot even check out a file that is too long, so the whole job fails before any test runs.
/// </summary>
public class SnapshotPathLengthTests
{
	// "D:\a\docs-builder\docs-builder\" is where a GitHub runner puts the checkout.
	private const int CheckoutRootLength = 31;
	private const string ReceivedFolder = ".artifacts/.snapshots/received/";
	// Windows' MAX_PATH is 260 including the terminating character; keep a margin.
	private const int MaxLength = 250;

	[Test]
	public void VerifiedSnapshots_FitInAWindowsPath_EvenWhenTheReceivedFileIsWritten()
	{
		var root = FindRepositoryRoot();
		var tooLong = new List<string>();
		foreach (var testsFolder in new[] { "tests", "tests-integration" })
		{
			var folder = Path.Combine(root, testsFolder);
			if (!Directory.Exists(folder))
				continue;
			foreach (var project in Directory.EnumerateDirectories(folder))
			{
				var snapshots = Path.Combine(project, ".snapshots");
				if (!Directory.Exists(snapshots))
					continue;
				foreach (var file in Directory.EnumerateFiles(snapshots, "*", SearchOption.AllDirectories))
				{
					var relative = Path.GetRelativePath(snapshots, file).Replace('\\', '/');
					var length = CheckoutRootLength + ReceivedFolder.Length + Path.GetFileName(project).Length + 1 + relative.Length;
					if (length > MaxLength)
						tooLong.Add($"{length} {Path.GetRelativePath(root, file).Replace('\\', '/')}");
				}
			}
		}

		tooLong.Should().BeEmpty(
			"a snapshot path over {0} characters cannot be checked out on Windows; shorten the test's name or its class",
			MaxLength
		);
	}

	private static string FindRepositoryRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "docs-builder.slnx")))
			dir = dir.Parent;
		return dir?.FullName ?? throw new InvalidOperationException("docs-builder.slnx not found above the test binaries");
	}
}
