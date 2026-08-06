// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

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
}
