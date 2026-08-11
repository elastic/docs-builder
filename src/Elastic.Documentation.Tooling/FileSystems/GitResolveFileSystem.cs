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

		var rootPath = root.FullName;
		var roots = new List<string> { rootPath };

		var fs = anchor.FileSystem;
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
			AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" },
			AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" }
		};
	}
}
