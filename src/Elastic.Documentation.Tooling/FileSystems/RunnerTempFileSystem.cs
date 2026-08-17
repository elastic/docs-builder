// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Nullean.ScopedFileSystem;
using static Elastic.Documentation.Extensions.IDirectoryInfoExtensions;

namespace Elastic.Documentation.FileSystems;

/// <summary>
/// Scope for CI evaluation commands: the working-directory root plus any runner-provided paths
/// (RUNNER_TEMP, artifact output dirs, staging dirs, metadata file locations).
/// <para>
/// Used by <c>evaluate-pr</c>, <c>evaluate-artifact</c>, and <c>prepare-artifact</c> —
/// all three operate on paths vended by the CI environment, not a fixed docset or checkout.
/// Permits <c>.git</c> so changelog configuration can be located by the config loader.
/// </para>
/// </summary>
public class RunnerTempFileSystem(
	IDirectoryInfo workingRoot,
	IEnumerable<string>? ciPaths = null,
	IFileSystem? inner = null)
	: ScopedFileSystem(inner ?? Physical, BuildOptions(workingRoot, ciPaths)),
	IRunnerTempFileSystem
{
	private static readonly FileSystem Physical = new();

	private static ScopedFileSystemOptions BuildOptions(IDirectoryInfo workingRoot, IEnumerable<string>? ciPaths)
	{
		var fs = workingRoot.FileSystem;
		var roots = new List<string> { workingRoot.FullName };

		if (ciPaths is not null)
		{
			foreach (var path in ciPaths)
			{
				if (string.IsNullOrEmpty(path))
					continue;
				// Drop descendants and ancestors of workingRoot to avoid disjointness violations.
				var isDescendant = IsSubPath(path, workingRoot.FullName, fs);
				var isAncestor = IsSubPath(workingRoot.FullName, path, fs);
				var isDuplicate = roots.Contains(path, StringComparer.OrdinalIgnoreCase);
				if (!isDescendant && !isAncestor && !isDuplicate)
					roots.Add(path);
			}
		}

		return new ScopedFileSystemOptions([.. roots])
		{
			AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" },
			AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" }
		};
	}

	public static RunnerTempFileSystem ForEvaluatePr(IEnvironmentVariables env, IFileSystem? inner = null)
	{
		var fs = inner ?? Physical;
		var workingRoot = fs.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName);
		var runnerTemp = env.GetEnvironmentVariable("RUNNER_TEMP");
		return new RunnerTempFileSystem(workingRoot,
			ciPaths: string.IsNullOrWhiteSpace(runnerTemp) ? null : [runnerTemp],
			inner: inner);
	}

	public static RunnerTempFileSystem ForEvaluateArtifact(string metadataPath, IFileSystem? inner = null)
	{
		var fs = inner ?? Physical;
		var workingRoot = fs.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName);
		var metadataDir = System.IO.Path.GetDirectoryName(metadataPath);
		return new RunnerTempFileSystem(workingRoot,
			ciPaths: string.IsNullOrWhiteSpace(metadataDir) ? null : [metadataDir],
			inner: inner);
	}

	public static RunnerTempFileSystem ForPrepareArtifact(string? stagingDir, string? outputDir, IFileSystem? inner = null)
	{
		var fs = inner ?? Physical;
		var workingRoot = fs.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName);
		var ciPaths = new List<string>();
		if (!string.IsNullOrWhiteSpace(stagingDir))
			ciPaths.Add(stagingDir);
		if (!string.IsNullOrWhiteSpace(outputDir))
			ciPaths.Add(outputDir);
		return new RunnerTempFileSystem(workingRoot, ciPaths: ciPaths.Count > 0 ? ciPaths : null, inner: inner);
	}
}
