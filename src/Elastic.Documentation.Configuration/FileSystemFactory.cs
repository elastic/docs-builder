// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Configuration;

public static class FileSystemFactory
{
	// Read options: workspace + app data, all confirmed hidden names allowed.
	// Includes .git (GitCheckoutInformation reads it) and .artifacts/.doc.state
	// (incremental build reads existing output state).
	private static readonly ScopedFileSystemOptions ReadOptions = new(
		[Paths.WorkingDirectoryRoot.FullName, Paths.ApplicationData.FullName])
	{
		AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".artifacts" },
		AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".doc.state" }
	};

	// Write options: same scope roots but no .git — nothing in the build output
	// pipeline should ever write into the git repository metadata.
	private static readonly ScopedFileSystemOptions WriteOptions = new(
		[Paths.WorkingDirectoryRoot.FullName, Paths.ApplicationData.FullName])
	{
		AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".artifacts" },
		AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".doc.state" }
	};

	// AppData-only options: for components that only access caches/state files.
	private static readonly ScopedFileSystemOptions AppDataOptions = new(
		[Paths.ApplicationData.FullName])
	{
		// .git needed for codex-link-index clone directory inside ApplicationData
		AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" }
	};

	/// <summary>
	/// A pre-allocated <see cref="ScopedFileSystem"/> for reading workspace files.
	/// Scoped to the working directory root and per-user app data; allows <c>.git</c>
	/// (read by <c>GitCheckoutInformation</c>), <c>.artifacts</c> and <c>.doc.state</c>
	/// (read for incremental build state).
	/// </summary>
	public static ScopedFileSystem RealRead { get; } = new ScopedFileSystem(new FileSystem(), ReadOptions);

	/// <summary>
	/// A pre-allocated <see cref="ScopedFileSystem"/> for writing build output.
	/// Same scope as <see cref="RealRead"/> but without <c>.git</c> access —
	/// nothing in the output pipeline should write into git repository metadata.
	/// </summary>
	public static ScopedFileSystem RealWrite { get; } = new ScopedFileSystem(new FileSystem(), WriteOptions);

	/// <summary>
	/// A pre-allocated <see cref="ScopedFileSystem"/> scoped only to the per-user
	/// <c>elastic/docs-builder</c> application data folder. Use for components that
	/// access caches or state and have no need for workspace files
	/// (e.g. <c>CrossLinkFetcher</c>, <c>CheckForUpdatesFilter</c>, <c>GitLinkIndexReader</c>).
	/// </summary>
	public static ScopedFileSystem AppData { get; } = new ScopedFileSystem(new FileSystem(), AppDataOptions);

	/// <summary>
	/// Creates a new <see cref="ScopedFileSystem"/> wrapping a fresh <see cref="MockFileSystem"/>,
	/// using the read workspace options. Each call returns a new independent in-memory file system.
	/// </summary>
	public static ScopedFileSystem InMemory() => new(new MockFileSystem(), ReadOptions);

	/// <summary>Wraps <paramref name="inner"/> with read workspace options (.git allowed).</summary>
	public static ScopedFileSystem WrapToRead(IFileSystem inner) =>
		new(inner, ReadOptions);

	/// <summary>
	/// Wraps <paramref name="inner"/> with read workspace options extended by
	/// <paramref name="extensionRoots"/> (e.g. detection-rules folders declared via
	/// <see cref="IDocsBuilderExtension.ExternalScopeRoots"/>).
	/// </summary>
	public static ScopedFileSystem WrapToRead(IFileSystem inner, IEnumerable<string>? extensionRoots)
	{
		if (extensionRoots is null)
			return WrapToRead(inner);

		var roots = new[] { Paths.WorkingDirectoryRoot.FullName, Paths.ApplicationData.FullName }
			.Concat(extensionRoots)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
		if (roots.Length == 2)
			return WrapToRead(inner);

		return new ScopedFileSystem(inner, new ScopedFileSystemOptions(roots)
		{
			AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".artifacts" },
			AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".doc.state" }
		});
	}

	/// <summary>Wraps <paramref name="inner"/> with write workspace options (.git not allowed).</summary>
	public static ScopedFileSystem WrapToWrite(IFileSystem inner) =>
		new(inner, WriteOptions);
}
