// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.GitHub;

/// <summary>Posts or updates a sticky changelog comment on a GitHub pull request.</summary>
public interface IGitHubCommentService
{
	/// <summary>
	/// Creates or updates the sticky changelog comment on the given pull request.
	/// If an existing comment is found (by the legacy <c>### 📋 Changelog</c> prefix or the
	/// embedded <c>&lt;!-- docs-builder:changelog --&gt;</c> marker), it is edited in-place;
	/// otherwise a new comment is created.
	/// </summary>
	/// <returns>
	/// The comment's GraphQL <c>node_id</c> on success; <c>null</c> when the operation failed.
	/// </returns>
	Task<string?> UpsertStickyCommentAsync(string owner, string repo, int prNumber, string body, Cancel ctx = default);

	/// <summary>
	/// Minimizes (hides) the given comment on GitHub using the <c>RESOLVED</c> classifier.
	/// Failures are logged as warnings and do not affect the command exit code.
	/// </summary>
	Task<bool> MinimizeCommentAsync(string nodeId, Cancel ctx = default);

	/// <summary>
	/// Un-minimizes (reveals) the given comment on GitHub so it is visible to the author again.
	/// Failures are logged as warnings and do not affect the command exit code.
	/// </summary>
	Task<bool> UnminimizeCommentAsync(string nodeId, Cancel ctx = default);
}
