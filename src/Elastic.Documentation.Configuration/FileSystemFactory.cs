// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Configuration;

public static class FileSystemFactory
{
	// Workspace options: covers working directory root + per-user app data.
	// Only hidden names with confirmed IFileSystem access are allowed.
	private static readonly ScopedFileSystemOptions WorkspaceOptions = new(
		[Paths.WorkingDirectoryRoot.FullName, Paths.ApplicationData.FullName])
	{
		AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".artifacts" },
		AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".doc.state" }
	};

	// AppData-only options: for components that only access caches/state files.
	private static readonly ScopedFileSystemOptions AppDataOptions = new(
		[Paths.ApplicationData.FullName])
	{
		// .git needed for codex-link-index clone directory inside ApplicationData
		AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" }
	};

	/// <summary>
	/// A pre-allocated <see cref="ScopedFileSystem"/> wrapping a real <see cref="FileSystem"/>,
	/// scoped to the working directory root and the per-user <c>elastic/docs-builder</c>
	/// application data folder. Use for all normal file system operations.
	/// </summary>
	public static IFileSystem Real { get; } = new ScopedFileSystem(new FileSystem(), WorkspaceOptions);

	/// <summary>
	/// A pre-allocated <see cref="ScopedFileSystem"/> scoped only to the per-user
	/// <c>elastic/docs-builder</c> application data folder. Use for components that
	/// access caches or state and have no need for workspace files
	/// (e.g. <c>CrossLinkFetcher</c>, <c>CheckForUpdatesFilter</c>, <c>GitLinkIndexReader</c>).
	/// </summary>
	public static IFileSystem AppData { get; } = new ScopedFileSystem(new FileSystem(), AppDataOptions);

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
	/// with additional scope roots declared by extensions (e.g. detection-rules folders outside the workspace).
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
			AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".artifacts" },
			AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".doc.state" }
		});
	}
}
