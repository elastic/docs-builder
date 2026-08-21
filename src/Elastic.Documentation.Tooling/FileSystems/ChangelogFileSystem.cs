// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.FileSystems;

/// <summary>
/// Scope for changelog commands: the git root of the target repository.
/// Allows reading <c>.git</c> metadata (remote URL, branch) and writing under
/// the conventional <c>.artifacts</c> CI staging directory. Does not include
/// AppData — changelog operates only within the repo working tree.
/// </summary>
public class ChangelogFileSystem(IDirectoryInfo root, IFileSystem? inner = null)
	: ScopedFileSystem(inner ?? Physical, new ScopedFileSystemOptions([root.FullName])
	{
		AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".artifacts" },
		AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" }
	}),
	IChangelogFileSystem
{
	private static readonly FileSystem Physical = new();

	/// <summary>
	/// Creates a scope anchored at the git root of the current working directory.
	/// Falls back to the working directory itself when no <c>.git</c> root is found —
	/// <c>changelog init</c> is designed to run before a git repository exists.
	/// </summary>
	public static ChangelogFileSystem FromWorkingDirectory(IFileSystem? inner = null)
	{
		var fs = inner ?? Physical;
		var workingRoot = fs.DirectoryInfo.New(fs.Directory.GetCurrentDirectory());
		var gitRoot = Paths.FindGitRoot(workingRoot) ?? workingRoot;
		return new ChangelogFileSystem(gitRoot, inner);
	}
}
