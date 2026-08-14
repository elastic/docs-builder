// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Extensions;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.FileSystems;

/// <summary>
/// Bootstrap-only filesystem for git resolution steps in <see cref="DocumentationPathsResolver"/>.
/// Discarded before <c>BuildContext</c> exists.
/// <para>
/// Rooted at <c>maxParents</c> levels above the docset anchor —
/// git resolution measures from the anchor, not the invocation path, so that both
/// <c>--path repo/</c> and <c>--path repo/docs</c> converge on the same checkout under a single bound.
/// </para>
/// <para>
/// The optional <c>gitDirectories</c> parameter widens the scope to include the resolved
/// <c>.git</c> directories (needed for the second pass that reads <c>config</c>, <c>HEAD</c>, and
/// <c>refs/heads/*</c> — for worktrees these lie outside the anchor's ancestry).
/// </para>
/// </summary>
#pragma warning disable IDE0290 // Cannot use primary constructor — delegates to static helper
public class GitResolveFileSystem : ScopedFileSystem
{
	public GitResolveFileSystem(
		IDirectoryInfo anchor,
		int maxParents = 1,
		IReadOnlyList<string>? gitDirectories = null,
		IFileSystem? inner = null)
		: base(inner ?? new FileSystem(), BuildOptions(anchor, maxParents, gitDirectories))
	{
	}
#pragma warning restore IDE0290

	private static ScopedFileSystemOptions BuildOptions(
		IDirectoryInfo anchor,
		int maxParents,
		IReadOnlyList<string>? gitDirectories)
	{
		// Walk maxParents above the anchor to get the scope root.
		var root = anchor;
		for (var i = 0; i < maxParents; i++)
			root = root.Parent ?? root;

#if DEBUG
		// anchor may come from a ScopedFileSystem (e.g. DocsetScanFileSystem scoped to
		// .artifacts/migrated) whose scope blocks upward traversal. Use the real filesystem
		// to call FindGitRoot so the DEBUG .slnx escape hatch in that method can actually fire.
		var realFs = new FileSystem();
		var realStart = realFs.DirectoryInfo.New(anchor.FullName);
		var gitRoot = Configuration.Paths.FindGitRoot(realStart, maxParents: 20);
		if (gitRoot is not null)
			root = anchor.FileSystem.DirectoryInfo.New(gitRoot.FullName);
#endif

		// ScopedFileSystem normalises scope roots via TrimEnd(separator). On Unix, "/" trimmed is "".
		// An empty scope root makes every path fail the IsWithinRoot check, so guard against it:
		// if the computed root IS the filesystem root, fall back to the anchor itself.
		var fs = anchor.FileSystem;
		var normalised = root.FullName.TrimEnd(fs.Path.DirectorySeparatorChar, fs.Path.AltDirectorySeparatorChar);
		var rootPath = string.IsNullOrEmpty(normalised) ? anchor.FullName : root.FullName;
		var roots = new List<string> { rootPath };

		if (gitDirectories is { Count: > 0 })
		{
			foreach (var gitDir in gitDirectories)
			{
				if (!IDirectoryInfoExtensions.IsSubPath(gitDir, rootPath, fs))
					roots.Add(gitDir);
			}
		}

		return new ScopedFileSystemOptions([.. roots])
		{
			AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".artifacts" },
			AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" }
		};
	}
}
