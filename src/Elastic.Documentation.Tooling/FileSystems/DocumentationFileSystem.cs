// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.FileSystems;

/// <summary>
/// The read scope for a single documentation set. Anchored on a resolved <c>docset.yml</c> and its
/// checkout root. Exposes a matching <see cref="Write"/> scope derived from the same paths — so read and
/// write cannot disagree about the checkout.
/// <para>
/// Construction is via <see cref="Resolve"/> only. The constructor is private; it takes
/// already-resolved paths so that <see cref="DocumentationPathsResolver"/> can run its bootstrap
/// scopes before the final scope is built.
/// </para>
/// </summary>
public class DocumentationFileSystem : ScopedFileSystem
{
	private static readonly FileSystem Physical = new();

	private DocumentationFileSystem(ResolvedDocumentationPaths paths, IFileSystem inner, IFileSystem? innerWrite = null)
		: base(inner, BuildReadOptions(paths))
	{
		Paths = paths;
		Write = new DocumentationWriteFileSystem(paths.CheckoutDirectory, paths.OutputDirectory, innerWrite ?? inner);
	}

	/// <summary>Everything the anchoring resolved. Read and write scopes are derived from exactly this.</summary>
	public ResolvedDocumentationPaths Paths { get; }

	/// <summary>
	/// This instance as a read scope. Always prefer <c>.Read</c> at call sites over passing the
	/// instance directly — a slot that wants a read scope should say so, symmetrically with <c>.Write</c>.
	/// </summary>
	public DocumentationFileSystem Read => this;

	/// <summary>Write scope derived from the same resolved paths. Never wraps <c>this</c>.</summary>
	public DocumentationWriteFileSystem Write { get; }

	/// <summary>
	/// Anchor on the docset under <paramref name="path"/>, derive the checkout from it, and scope to
	/// the result. For build/serve commands where the user supplied <c>--path</c> (or nothing).
	/// </summary>
	/// <param name="path">
	/// The directory to start the docset scan from. When <see langword="null"/>, the current working
	/// directory is used.
	/// </param>
	/// <param name="options">
	/// Optional tuning: output directory, explicit <c>--git-dir</c>, pre-discovered configuration file,
	/// extra scope roots, max parents for the git walk, and the mock seam.
	/// </param>
	/// <exception cref="DocumentationPathException">
	/// No docset found under <paramref name="path"/>, or no <c>.git</c> within <c>MaxParents</c> of the
	/// anchor and no <c>--git-dir</c> override.
	/// </exception>
	public static DocumentationFileSystem Resolve(
		IDirectoryInfo? path = null,
		DocumentationScopeOptions? options = null)
	{
		var opts = options ?? new DocumentationScopeOptions();
		var inner = opts.Inner ?? Physical;
		var invocation = path ?? inner.DirectoryInfo.New(inner.Directory.GetCurrentDirectory());

		// Docset first (scoped to the invocation path), then git from the anchor it produced.
		// The resolver constructs its own bootstrap scopes in that order and discards them.
		var paths = DocumentationPathsResolver.Resolve(invocation, opts, inner);
		return new DocumentationFileSystem(paths, inner, opts.InnerWrite);
	}

	private static ScopedFileSystemOptions BuildReadOptions(ResolvedDocumentationPaths paths)
	{
		var checkoutPath = paths.CheckoutDirectory.FullName;
		var roots = new List<string> { checkoutPath };

		// AppData is disjointness-filtered: on CI each individual docset checkout lives inside AppData
		// (/home/runner/.local/share/elastic/docs-builder/checkouts/current/<repo>), so AppData would
		// subsume checkoutPath and trigger ValidateRootsAreDisjoint.
		var appData = Configuration.Paths.ApplicationData.FullName;
		if (!IsSubPath(appData, checkoutPath) && !IsSubPath(checkoutPath, appData))
			roots.Add(appData);

		foreach (var gitDir in paths.GitDirectories)
		{
			if (!IsSubPath(gitDir, checkoutPath))
				roots.Add(gitDir);
		}

		foreach (var extra in paths.ExtraRoots)
		{
			if (!string.IsNullOrEmpty(extra)
				&& !IsSubPath(extra, checkoutPath)
				&& !roots.Contains(extra, StringComparer.OrdinalIgnoreCase))
			{
				roots.Add(extra);
			}
		}

		return new ScopedFileSystemOptions([.. roots])
		{
			AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".artifacts" },
			AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".doc.state", ".pagefind-net-frontend-version" }
		};
	}

	/// <summary>Returns true if <paramref name="path"/> is a subdirectory of <paramref name="parent"/>
	/// (or equals it), using a case-insensitive separator-normalised comparison.</summary>
	private static bool IsSubPath(string path, string parent)
	{
		var sep = System.IO.Path.DirectorySeparatorChar;
		var normalised = path.TrimEnd(sep) + sep;
		var parentNormalised = parent.TrimEnd(sep) + sep;
		return normalised.StartsWith(parentNormalised, StringComparison.OrdinalIgnoreCase);
	}
}
