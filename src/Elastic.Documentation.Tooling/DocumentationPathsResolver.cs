// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.FileSystems;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation;

/// <summary>
/// The resolved set of paths and git information for a documentation build or serve invocation.
/// Produced by <see cref="DocumentationPathsResolver"/> following the bootstrap first principles:
/// <list type="number">
///   <item><description><c>&lt;path&gt;</c> = <c>--path</c> argument, or the current working directory when omitted.</description></item>
///   <item><description><c>SourceDirectory</c> = docset scan from <c>&lt;path&gt;</c>:
///     <c>&lt;path&gt;/docset.yml</c>, <c>&lt;path&gt;/_docset.yml</c>, or any subfolder.
///     This is the <strong>anchor</strong> — all subsequent resolution is relative to it.</description></item>
///   <item><description><c>CheckoutDirectory</c> = <c>--git-dir?.Parent</c>
///     ?? <c>FindGitRoot(SourceDirectory, maxParents: N)</c>
///     ?? <strong>error</strong> (required; use <c>--git-dir</c> when the heuristic cannot find <c>.git</c>).</description></item>
///   <item><description><c>GitDirectories</c> = the real <c>.git</c> directory (or worktree <c>commondir</c>)
///     resolved from <c>CheckoutDirectory</c>. Needed to add the main repo's <c>.git</c> to the read scope
///     when the checkout is a worktree.</description></item>
///   <item><description><c>Git</c> = <see cref="GitCheckoutInformation"/> anchored exclusively on
///     <c>CheckoutDirectory</c> — resolved once here, never re-derived downstream.</description></item>
///   <item><description><c>OutputDirectory</c> = <c>--output</c> argument,
///     or <c>&lt;path&gt;/.artifacts/docs/html</c>.</description></item>
/// </list>
/// </summary>
public sealed record ResolvedDocumentationPaths
{
	/// <summary>
	/// The invocation path — the <c>--path</c> argument, or the process current directory when omitted.
	/// All other paths are derived from this starting point.
	/// </summary>
	public required IDirectoryInfo InvocationPath { get; init; }

	/// <summary>
	/// The docset anchor: the directory that contains <c>docset.yml</c> or <c>_docset.yml</c>.
	/// Found by scanning from <see cref="InvocationPath"/> via the known-location heuristic,
	/// then a bounded recursive fallback.
	/// </summary>
	public required IDirectoryInfo SourceDirectory { get; init; }

	/// <summary>The resolved docset configuration file (<c>docset.yml</c> or <c>_docset.yml</c>).</summary>
	public required IFileInfo ConfigurationPath { get; init; }

	/// <summary>
	/// The repository checkout root — the directory whose immediate child is <c>.git</c>
	/// (or whose immediate child is a worktree <c>.git</c> pointer file).
	/// Always non-<see langword="null"/>: the resolver emits a hard error when no <c>.git</c>
	/// is found within <c>maxParents</c> of <see cref="SourceDirectory"/> and no <c>--git-dir</c>
	/// override was supplied.
	/// </summary>
	public required IDirectoryInfo CheckoutDirectory { get; init; }

	/// <summary>
	/// The real <c>.git</c> directories that must be added to the read scope. For a regular
	/// checkout this is <c>[CheckoutDirectory/.git]</c>. For a worktree this also includes the
	/// main repo's <c>.git</c> (or the <c>commondir</c> target) so that git config and refs
	/// can be read without a scope violation.
	/// </summary>
	public IReadOnlyList<string> GitDirectories { get; init; } = [];

	/// <summary>
	/// Git checkout information (branch, ref, remote, repository name) resolved from
	/// <see cref="CheckoutDirectory"/>. Never re-derived downstream; always set from this record.
	/// </summary>
	public required GitCheckoutInformation Git { get; init; }

	/// <summary>
	/// The build output directory — the <c>--output</c> argument, or
	/// <c><see cref="InvocationPath"/>/.artifacts/docs/html</c>.
	/// </summary>
	public required IDirectoryInfo OutputDirectory { get; init; }

	/// <summary>
	/// Extra scope roots (e.g. <c>RUNNER_TEMP</c>, extension roots from
	/// <c>IDocsBuilderExtension.ExternalScopeRoots</c>) to include in the read scope.
	/// Disjointness-filtered: roots nested inside <see cref="CheckoutDirectory"/> are dropped.
	/// </summary>
	public IReadOnlyList<string> ExtraRoots { get; init; } = [];
}

/// <summary>
/// Arguments that tune <see cref="DocumentationPathsResolver.Resolve"/>.
/// </summary>
public sealed record DocumentationScopeOptions
{
	/// <summary>Explicit output directory (<c>--output</c>).</summary>
	public IDirectoryInfo? Output { get; init; }

	/// <summary>
	/// Explicit <c>--git-dir</c> override — the <c>.git</c> directory; its <c>.Parent</c> is the checkout.
	/// Worktrees are handled automatically through the <c>commondir</c> path; do not point this at a
	/// worktree's internal gitdir (<c>.git/worktrees/&lt;name&gt;</c>).
	/// </summary>
	public IDirectoryInfo? GitDir { get; init; }

	/// <summary>Pre-discovered docset configuration file. When set, the docset scan is skipped.</summary>
	public IFileInfo? ConfigurationFile { get; init; }

	/// <summary>
	/// Git checkout information override (for tests). Replaces the <c>GitCheckoutInformationFactory</c>
	/// call; goes through resolution rather than bypassing it.
	/// </summary>
	public GitCheckoutInformation? Git { get; init; }

	/// <summary>
	/// Extra scope roots (e.g. <c>RUNNER_TEMP</c>, extension roots). Disjointness-filtered: roots
	/// nested inside the resolved checkout are dropped instead of throwing.
	/// </summary>
	public IEnumerable<string>? ExtraRoots { get; init; }

	/// <summary>
	/// Maximum number of parent directories to walk above the docset anchor when searching for
	/// <c>.git</c> (default: 1).
	/// </summary>
	public int MaxParents { get; init; } = 1;

	/// <summary>The underlying filesystem for reads. Defaults to the real filesystem. Pass a mock in tests.</summary>
	public IFileSystem? Inner { get; init; }

	/// <summary>
	/// Override the inner filesystem used for writes. When <see langword="null"/> (the default),
	/// <see cref="Inner"/> is used for both read and write scopes.
	/// Use this to wire a mock write scope against a real read scope (e.g. navigation tests that read
	/// the real docs tree but write output to an in-memory filesystem).
	/// </summary>
	public IFileSystem? InnerWrite { get; init; }
}

/// <summary>
/// Thrown when <see cref="DocumentationPathsResolver.Resolve"/> cannot find a docset or checkout.
/// </summary>
public sealed class DocumentationPathException(string message) : Exception(message);

/// <summary>
/// Orchestrates the six ordered steps documented on <see cref="ResolvedDocumentationPaths"/>.
/// Each step's bootstrap scope is created from what the previous step resolved, then discarded.
/// </summary>
public static class DocumentationPathsResolver
{
	/// <summary>
	/// Resolve all paths for a documentation build/serve invocation.
	/// </summary>
	/// <exception cref="DocumentationPathException">
	/// No docset found, or no <c>.git</c> within <c>MaxParents</c> of the anchor and no
	/// <c>--git-dir</c> override.
	/// </exception>
	public static ResolvedDocumentationPaths Resolve(
		IDirectoryInfo invocation,
		DocumentationScopeOptions options,
		IFileSystem inner)
	{
		// 1-2. Anchor. Scoped to the invocation path only; skipped when the docset is already known.
		var (source, configuration) = options.ConfigurationFile is { } known
			? (known.Directory!, known)
			: ScanForDocset(invocation, inner);

		// 3. Checkout, derived from the anchor — never from the invocation.
		var gitScope = new GitResolveFileSystem(source, options.MaxParents, inner: inner);
		var checkout = ResolveCheckout(gitScope, source, options, inner);

		// 4. Real git directories (the .git pointer path + resolved target for worktrees).
		//    inner (unscoped) is used for worktree resolution: the resolved gitdir lives outside the
		//    anchor's ancestry by design, so a scoped FS would block the commondir traversal.
		//    When --git-dir is explicit the checkout is gitDir.Parent, so gitDir itself must be
		//    carried forward — ResolveGitDirectories can't find it via the gitScope (out-of-tree).
		var gitDirectories = options.GitDir is { } explicitGitDir
			? [explicitGitDir.FullName]
			: ResolveGitDirectories(gitScope, checkout, inner);

		// 5. Git information, through a scope widened by step 4.
		//    TryCreate reads config/HEAD from the resolved gitdir, which for a worktree lies outside
		//    the anchor's ancestry — so this is a second instance rather than the same one.
		//    This step uses a GitResolveFileSystem (for .git-aware scoping) because it reads FILES
		//    inside .git/ rather than listing directories at the scope root.
		var git = options.Git ?? GitCheckoutInformationFactory.Create(checkout,
			new GitResolveFileSystem(source, options.MaxParents, gitDirectories, inner));

		// 6. Output. Default is relative to the checkout, not the invocation.
		//    --path repo/docs and --path repo/ must both write to repo/.artifacts, not repo/docs/.artifacts.
		var output = options.Output ?? inner.DirectoryInfo.New(
			inner.Path.Join(checkout.FullName, ".artifacts", "docs", "html"));

		// 7. Disjointness-filter the extra roots.
		var extraRoots = FilterExtraRoots(options.ExtraRoots, checkout);

		return new ResolvedDocumentationPaths
		{
			InvocationPath = invocation,
			SourceDirectory = source,
			ConfigurationPath = configuration,
			CheckoutDirectory = checkout,
			GitDirectories = gitDirectories,
			Git = git,
			OutputDirectory = output,
			ExtraRoots = extraRoots
		};
	}

	private static (IDirectoryInfo, IFileInfo) ScanForDocset(IDirectoryInfo invocation, IFileSystem inner)
	{
		var scan = new DocsetScanFileSystem(invocation, inner);
		if (!Paths.TryFindDocsFolderFromRoot(scan, scan.DirectoryInfo.New(invocation.FullName), out var dir, out var file))
			throw new DocumentationPathException(
				$"No docset.yml or _docset.yml found in '{invocation.FullName}' or any subfolder.");
		return (dir, file);
	}

	private static IDirectoryInfo ResolveCheckout(
		IFileSystem gitScope,
		IDirectoryInfo source,
		DocumentationScopeOptions options,
		IFileSystem inner)
	{
		if (options.GitDir is { } explicitGitDir)
		{
			if (!inner.Directory.Exists(explicitGitDir.FullName))
				throw new DocumentationPathException(
					$"--git-dir '{explicitGitDir.FullName}' does not exist.");
			if (!inner.File.Exists(inner.Path.Join(explicitGitDir.FullName, "HEAD")))
				throw new DocumentationPathException(
					$"--git-dir '{explicitGitDir.FullName}' does not appear to be a valid .git directory (no HEAD file found).");
			return explicitGitDir.Parent
				?? throw new DocumentationPathException(
					$"--git-dir '{explicitGitDir.FullName}' has no parent directory.");
		}

		var gitRoot = Paths.FindGitRoot(gitScope.DirectoryInfo.New(source.FullName), options.MaxParents);
		if (gitRoot is not null)
			return gitRoot;

		// Graceful fallback for mock filesystems without a .git layout (pure-in-memory test scenarios
		// that do not need a real checkout boundary). Real filesystems always require an explicit checkout.
		var innerType = inner is ScopedFileSystem sf ? sf.InnerType : inner.GetType();
		if (innerType.Name.Contains("Mock", StringComparison.OrdinalIgnoreCase))
			return source;

		throw new DocumentationPathException(
			$"No .git found at '{source.FullName}' or within {options.MaxParents} parent directory(ies). "
			+ "Pass --git-dir to point at the repository's .git directory explicitly.");
	}

	private static IReadOnlyList<string> ResolveGitDirectories(IFileSystem gitScope, IDirectoryInfo checkout, IFileSystem inner)
	{
		var gitPath = gitScope.Path.Join(checkout.FullName, ".git");
		if (gitScope.Directory.Exists(gitPath))
			return [gitPath];

		// Worktree: .git is a pointer file. Use inner (unscoped) for TryReadGitDirPointer so that
		// the commondir traversal can reach the main .git directory, which lies outside the gitScope
		// root by design (the worktree gitdir is inside the main repo's .git tree).
		return Paths.TryReadGitDirPointer(inner, inner.FileInfo.New(gitPath), out var resolved) && resolved is not null
			? [gitPath, resolved.FullName]
			: [];
	}

	private static IReadOnlyList<string> FilterExtraRoots(
		IEnumerable<string>? extraRoots,
		IDirectoryInfo checkout)
	{
		if (extraRoots is null)
			return [];

		var fs = checkout.FileSystem;
		var checkoutPath = checkout.FullName;
		var result = new List<string>();
		foreach (var root in extraRoots)
		{
			if (string.IsNullOrEmpty(root))
				continue;
			// Drop descendants of checkout (already in scope) and ancestors (would subsume checkout).
			if (!IDirectoryInfoExtensions.IsSubPath(root, checkoutPath, fs)
				&& !IDirectoryInfoExtensions.IsSubPath(checkoutPath, root, fs)
				&& !result.Contains(root, StringComparer.OrdinalIgnoreCase))
			{
				result.Add(root);
			}
		}
		return result;
	}
}
