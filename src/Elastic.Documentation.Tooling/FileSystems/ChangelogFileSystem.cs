// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Extensions;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.FileSystems;

/// <summary>
/// Scope for changelog commands: the git root of the target repository, plus any output folders the
/// caller names. Allows reading <c>.git</c> metadata (remote URL, branch) and writing under
/// the conventional <c>.artifacts</c> CI staging directory. Does not include
/// AppData — changelog operates only within the repo working tree and the folders it was told to write to.
/// </summary>
/// <param name="root">The git root. Everything read comes from below it.</param>
/// <param name="inner">The underlying file system. Defaults to the real one.</param>
/// <param name="extraRoots">
/// Output folders outside <paramref name="root"/> that may be written to, like the output directory of
/// the documentation build. A folder inside <paramref name="root"/> adds nothing.
/// </param>
public class ChangelogFileSystem(IDirectoryInfo root, IFileSystem? inner = null, IEnumerable<string>? extraRoots = null) : ScopedFileSystem(
	inner ?? Physical,
	CreateOptions(root, inner ?? Physical, extraRoots)
), IChangelogFileSystem
{
	private static readonly FileSystem Physical = new();

	private static ScopedFileSystemOptions CreateOptions(IDirectoryInfo root, IFileSystem fileSystem, IEnumerable<string>? extraRoots)
	{
		var roots = new List<string> { root.FullName };
		foreach (var extra in extraRoots ?? [])
			roots.AddDisjointRoot(extra, fileSystem);

		return new ScopedFileSystemOptions([.. roots])
		{
			AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".artifacts" },
			AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" }
		};
	}

	/// <summary>
	/// Creates a scope anchored at the git root of the current working directory.
	/// Falls back to the working directory itself when no <c>.git</c> root is found —
	/// <c>changelog init</c> is designed to run before a git repository exists.
	/// </summary>
	/// <param name="inner">The underlying file system. Defaults to the real one.</param>
	/// <param name="extraRoots">Output folders outside the git root that may be written to.</param>
	public static ChangelogFileSystem FromWorkingDirectory(IFileSystem? inner = null, IEnumerable<string>? extraRoots = null)
	{
		var fs = inner ?? Physical;
		var workingRoot = fs.DirectoryInfo.New(fs.Directory.GetCurrentDirectory());
		var gitRoot = Paths.FindGitRoot(workingRoot) ?? workingRoot;
		return new ChangelogFileSystem(gitRoot, inner, extraRoots);
	}
}
