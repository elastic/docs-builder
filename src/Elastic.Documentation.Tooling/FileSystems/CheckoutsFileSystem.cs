// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Extensions;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.FileSystems;

/// <summary>
/// Scope over a directory containing cloned documentation checkouts. No docset anchoring, no git
/// information — the assembler reads configuration and per-clone output, not a documentation set.
/// <para>
/// Use this for the assembler and changelog commands which work with a tree of clones.
/// Use <see cref="DocumentationFileSystem"/> when you have a single documentation set with a docset anchor.
/// </para>
/// </summary>
public class CheckoutsFileSystem : ScopedFileSystem
{
	private static readonly FileSystem Physical = new();

	private readonly IFileSystem _inner;

	public CheckoutsFileSystem(
		IDirectoryInfo root,
		IDirectoryInfo? output = null,
		IEnumerable<string>? extraRoots = null,
		IFileSystem? inner = null)
		: base(inner ?? Physical, BuildReadOptions(root, extraRoots))
	{
		_inner = inner ?? Physical;
		Write = new DocumentationWriteFileSystem(root, output, _inner);
	}

	/// <summary>
	/// This instance as a read scope. Always prefer <c>.Read</c> at call sites over passing the
	/// instance directly — a slot that wants a read scope should say so, symmetrically with <c>.Write</c>.
	/// </summary>
	public CheckoutsFileSystem Read => this;

	/// <summary>Write scope for this checkout tree.</summary>
	public DocumentationWriteFileSystem Write { get; }

	private static ScopedFileSystemOptions BuildReadOptions(
		IDirectoryInfo root,
		IEnumerable<string>? extraRoots)
	{
		var fs = root.FileSystem;
		var rootPath = root.FullName;
		var roots = new List<string> { rootPath };

		// AppData is disjointness-filtered too: on CI the checkouts directory lives inside AppData
		// (/home/runner/.local/share/elastic/docs-builder/checkouts/...), so AppData would subsume
		// root and the ScopedFileSystem constructor would throw.
		var appData = Paths.ApplicationData.FullName;
		if (!IDirectoryInfoExtensions.IsSubPath(appData, rootPath, fs)
			&& !IDirectoryInfoExtensions.IsSubPath(rootPath, appData, fs))
		{
			roots.Add(appData);
		}

		if (extraRoots is not null)
		{
			foreach (var extra in extraRoots)
			{
				if (string.IsNullOrEmpty(extra))
					continue;
				// Drop descendants of root (already covered) and ancestors (would subsume root, causing overlap).
				if (!IDirectoryInfoExtensions.IsSubPath(extra, rootPath, fs)
					&& !IDirectoryInfoExtensions.IsSubPath(rootPath, extra, fs)
					&& !roots.Contains(extra, StringComparer.OrdinalIgnoreCase))
				{
					roots.Add(extra);
				}
			}
		}

		return new ScopedFileSystemOptions([.. roots])
		{
			AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".artifacts" },
			AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".doc.state", ".pagefind-net-frontend-version" }
		};
	}

	/// <summary>
	/// Creates a scope over the current working directory root. Suitable for assembler and navigation
	/// commands that operate on the local checkout tree without a specific docset anchor.
	/// </summary>
	/// <param name="inner">The underlying filesystem. Defaults to a new <see cref="FileSystem"/> when <see langword="null"/>.</param>
	public static CheckoutsFileSystem FromWorkingDirectory(IFileSystem? inner = null) =>
		new(
			(inner ?? Physical).DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName),
			inner: inner);
}
