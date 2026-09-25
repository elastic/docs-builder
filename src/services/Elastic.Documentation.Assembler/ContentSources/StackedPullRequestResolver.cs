// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.ContentSources;

/// <summary>A minimal view of an open pull request, enough to walk a stack.</summary>
/// <param name="Number">The pull request number.</param>
/// <param name="HeadRepository">Full name (<c>owner/repo</c>) of the repository the head branch lives in.</param>
/// <param name="HeadBranch">The head branch name.</param>
/// <param name="BaseBranch">The base branch name.</param>
public sealed record PullRequestSummary(int Number, string HeadRepository, string HeadBranch, string BaseBranch);

/// <summary>Looks up open pull requests by their head branch.</summary>
public interface IPullRequestLookup
{
	/// <summary>
	/// Returns the open pull requests in <paramref name="repository"/> whose head branch is
	/// <paramref name="headBranch"/> and whose head lives in the same repository.
	/// </summary>
	Task<IReadOnlyList<PullRequestSummary>> ListOpenPullRequestsByHead(string repository, string headBranch, Cancel ctx);
}

/// <summary>Why a stack walk stopped.</summary>
public enum StackResolutionOutcome
{
	/// <summary>A branch in the chain is a content source.</summary>
	Matched,
	/// <summary>The chain ended on a branch that is not a content source and is not the head of any open pull request.</summary>
	NoParent,
	/// <summary>More than one open pull request uses the same head branch, so the parent is ambiguous.</summary>
	Ambiguous,
	/// <summary>The chain of pull requests loops back on itself.</summary>
	Cycle,
	/// <summary>The chain is deeper than <see cref="StackedPullRequestResolver.MaxDepth"/>.</summary>
	DepthExceeded
}

/// <summary>The result of walking a stack of pull requests.</summary>
/// <param name="Outcome">Why the walk stopped.</param>
/// <param name="ResolvedBranch">The last branch visited. When <see cref="Outcome"/> is <see cref="StackResolutionOutcome.Matched"/>, this is the content-source branch.</param>
/// <param name="ParentPullRequests">The pull requests walked through, nearest parent first. Empty when the base branch itself is a content source.</param>
public sealed record StackResolution(StackResolutionOutcome Outcome, string ResolvedBranch, IReadOnlyList<int> ParentPullRequests)
{
	/// <summary>Whether the walk found a content-source branch.</summary>
	public bool Matched => Outcome == StackResolutionOutcome.Matched;
}

/// <summary>
/// Resolves the content-source branch of a stacked pull request. A stacked pull request targets the
/// head branch of another open pull request instead of a content-source branch. Starting from a base
/// branch, the resolver follows the chain of open pull requests until it reaches a branch for which
/// <c>isContentSource</c> returns <c>true</c>. Because each hop is tested against the real content-source
/// configuration, a branch that is itself a content source is never walked past, so an unrelated open
/// pull request such as <c>main -&gt; 9.4</c> can never redirect a pull request that targets <c>main</c>.
/// </summary>
public sealed class StackedPullRequestResolver(ILoggerFactory logFactory, IPullRequestLookup lookup)
{
	/// <summary>The maximum number of pull requests a stack may contain.</summary>
	public const int MaxDepth = 50;

	private readonly ILogger _logger = logFactory.CreateLogger<StackedPullRequestResolver>();

	/// <summary>
	/// Walks the stack from <paramref name="baseBranch"/> upward.
	/// </summary>
	/// <param name="repository">Full repository name (<c>owner/repo</c>).</param>
	/// <param name="baseBranch">The branch the pull request under evaluation targets.</param>
	/// <param name="isContentSource">Returns whether a branch is a configured content source.</param>
	/// <param name="ctx">Cancellation token.</param>
	public async Task<StackResolution> Resolve(string repository, string baseBranch, Func<string, bool> isContentSource, Cancel ctx)
	{
		var branch = baseBranch;
		var parents = new List<int>();
		var visited = new HashSet<string>(StringComparer.Ordinal);

		while (true)
		{
			if (isContentSource(branch))
			{
				if (parents.Count > 0)
				{
					_logger.LogInformation(
						"Resolved stacked pull request through {Parents} to content-source branch '{Branch}'",
						string.Join(", ", parents.Select(number => $"#{number}")),
						branch
					);
				}
				return new StackResolution(StackResolutionOutcome.Matched, branch, parents);
			}

			if (!visited.Add(branch))
			{
				_logger.LogWarning("Cycle detected while resolving stacked pull request at branch '{Branch}'", branch);
				return new StackResolution(StackResolutionOutcome.Cycle, branch, parents);
			}

			if (parents.Count >= MaxDepth)
			{
				_logger.LogWarning("Stacked pull request chain exceeds the maximum depth of {MaxDepth}", MaxDepth);
				return new StackResolution(StackResolutionOutcome.DepthExceeded, branch, parents);
			}

			var candidates = await lookup.ListOpenPullRequestsByHead(repository, branch, ctx);
			var sameRepository = candidates.Where(
				pr => string.Equals(pr.HeadRepository, repository, StringComparison.OrdinalIgnoreCase) && pr.HeadBranch == branch
			).ToList();

			if (sameRepository.Count == 0)
			{
				_logger.LogInformation("Branch '{Branch}' is not a content source and is not the head of an open pull request", branch);
				return new StackResolution(StackResolutionOutcome.NoParent, branch, parents);
			}

			if (sameRepository.Count > 1)
			{
				_logger.LogWarning(
					"Cannot resolve stacked pull request: {Count} open pull requests share the head branch '{Branch}' ({Numbers})",
					sameRepository.Count,
					branch,
					string.Join(", ", sameRepository.Select(pr => $"#{pr.Number}"))
				);
				return new StackResolution(StackResolutionOutcome.Ambiguous, branch, parents);
			}

			var parent = sameRepository[0];
			_logger.LogInformation("Stack parent #{Number}: {Head} -> {Base}", parent.Number, parent.HeadBranch, parent.BaseBranch);
			parents.Add(parent.Number);
			branch = parent.BaseBranch;
		}
	}
}
