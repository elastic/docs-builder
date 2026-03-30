// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Configuration;

public static class FileSystemFactory
{
	private static readonly ScopedFileSystemOptions WorkspaceOptions = new(
		[Paths.WorkingDirectoryRoot.FullName, Paths.ApplicationData.FullName])
	{
		AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" },
		AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			".git", ".gitignore", ".gitmodules", ".gitattributes", ".editorconfig", ".nojekyll"
		},
		AllowedSpecialFolders = AllowedSpecialFolder.Temp
	};

	/// <summary>
	/// A pre-allocated <see cref="ScopedFileSystem"/> wrapping a real <see cref="FileSystem"/>,
	/// scoped to the working directory root and the per-user <c>elastic/docs-builder</c> application data folder.
	/// Use this for all normal file system operations.
	/// </summary>
	public static IFileSystem Real { get; } = new ScopedFileSystem(new FileSystem(), WorkspaceOptions);

	/// <summary>
	/// Creates a new <see cref="ScopedFileSystem"/> wrapping a fresh <see cref="MockFileSystem"/>,
	/// using the standard workspace options. Each call returns a new independent in-memory file system.
	/// </summary>
	public static IFileSystem InMemory() => CreateScoped(new MockFileSystem());

	/// <summary>
	/// Creates a <see cref="ScopedFileSystem"/> wrapping any <paramref name="inner"/> <see cref="IFileSystem"/>
	/// (e.g. <c>MockFileSystem</c>), using the standard workspace options.
	/// </summary>
	public static IFileSystem CreateScoped(IFileSystem inner) =>
		new ScopedFileSystem(inner, WorkspaceOptions);

	/// <summary>
	/// Creates a <see cref="ScopedFileSystem"/> wrapping any <paramref name="inner"/> <see cref="IFileSystem"/>
	/// with additional scope roots declared by extensions (e.g. detection rules folders outside the workspace).
	/// </summary>
	public static IFileSystem CreateScoped(IFileSystem inner, IEnumerable<string>? additionalRoots)
	{
		if (additionalRoots is null)
			return CreateScoped(inner);

		var roots = new[] { Paths.WorkingDirectoryRoot.FullName, Paths.ApplicationData.FullName }
			.Concat(additionalRoots)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
		if (roots.Length == 2)
			return CreateScoped(inner);

		return new ScopedFileSystem(inner, new ScopedFileSystemOptions(roots)
		{
			AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" },
			AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				".git", ".gitignore", ".gitmodules", ".gitattributes", ".editorconfig", ".nojekyll"
			},
			AllowedSpecialFolders = AllowedSpecialFolder.Temp
		});
	}

	/// <summary>
	/// Creates a <see cref="ScopedFileSystem"/> for user home data operations,
	/// scoped to the user profile directory with <c>.docs-builder</c> and <c>.git</c> allowed.
	/// Used by <c>GitLinkIndexReader</c> which caches to <c>~/.docs-builder/codex-link-index</c>.
	/// </summary>
	public static IFileSystem CreateForUserData() =>
		new ScopedFileSystem(
			new FileSystem(),
			new ScopedFileSystemOptions([Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)])
			{
				AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".docs-builder", ".git" },
				AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".gitignore" }
			}
		);
}
