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
	/// <c>true</c> on success or when the API responds with a transient non-fatal error;
	/// <c>false</c> only when the operation is definitively known to have failed.
	/// </returns>
	Task<bool> UpsertStickyCommentAsync(string owner, string repo, int prNumber, string body, Cancel ctx = default);
}
