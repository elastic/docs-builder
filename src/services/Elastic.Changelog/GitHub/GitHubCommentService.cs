// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.GitHub;

/// <summary>
/// Posts or updates the sticky changelog PR comment via the GitHub REST API.
/// <para>
/// Identity strategy: matches on the embedded HTML marker
/// <c>&lt;!-- docs-builder:changelog --&gt;</c> (present in all comments written by this service),
/// falling back to the legacy <c>### 📋 Changelog</c> title prefix used by the predecessor JS
/// scripts (<c>comment-helper.js</c> in <c>changelog/submit/apply/scripts/</c>). The fallback
/// ensures in-flight PRs whose comment was posted by the JS path get edited rather than duplicated
/// when this service takes over.
/// </para>
/// <para>
/// Pagination: GitHub returns at most 100 comments per page; this service fetches all pages before
/// deciding whether to create or update, avoiding the duplicate-comment bug present in
/// <c>docs-preview-local.yml:452-476</c> and <c>docs-deploy.yml</c>.
/// </para>
/// <para>
/// Failure policy: a transient error (rate-limit, 403, network blip) logs a warning and returns
/// <c>false</c> without calling <c>EmitError</c>. Never let a comment failure flip the verdict.
/// </para>
/// </summary>
public partial class GitHubCommentService(ILoggerFactory loggerFactory, GitHubApiTransport? transport = null) : IGitHubCommentService
{
	/// <summary>
	/// HTML marker embedded at the end of every comment body written by this service.
	/// Used as the primary identity key when searching for an existing sticky comment.
	/// </summary>
	private const string HtmlMarker = "<!-- docs-builder:changelog -->";

	/// <summary>
	/// Legacy title prefix used by the predecessor JS scripts in <c>comment-helper.js</c>.
	/// Matched as a fallback so in-flight PRs whose comment was posted by JS get updated,
	/// not duplicated.
	/// </summary>
	internal const string LegacyTitlePrefix = "### 📋 Changelog";

	private readonly ILogger<GitHubCommentService> _logger = loggerFactory.CreateLogger<GitHubCommentService>();
	private readonly GitHubApiTransport _transport = transport ?? new GitHubApiTransport();

	/// <inheritdoc />
	public async Task<bool> UpsertStickyCommentAsync(string owner, string repo, int prNumber, string body, Cancel ctx = default)
	{
		var markedBody = body.TrimEnd() + "\n" + HtmlMarker;

		try
		{
			var existingId = await FindExistingCommentIdAsync(owner, repo, prNumber, ctx);

			if (existingId.HasValue)
			{
				var updateUrl = $"https://api.github.com/repos/{owner}/{repo}/issues/comments/{existingId.Value}";
				var updatePayload = JsonSerializer.Serialize(new CommentBody { Body = markedBody }, CommentJsonContext.Default.CommentBody);
				using var updateResponse = await _transport.PatchAsync(updateUrl, updatePayload, ctx);
				if (!updateResponse.IsSuccessStatusCode)
				{
					_logger.LogWarning(
						"Failed to update comment {CommentId} on PR #{PrNumber}: {Status}",
						existingId.Value,
						prNumber,
						(int)updateResponse.StatusCode
					);
					return false;
				}

				_logger.LogInformation("Updated changelog comment {CommentId} on PR #{PrNumber}", existingId.Value, prNumber);
				return true;
			}

			var createUrl = $"https://api.github.com/repos/{owner}/{repo}/issues/{prNumber}/comments";
			var createPayload = JsonSerializer.Serialize(new CommentBody { Body = markedBody }, CommentJsonContext.Default.CommentBody);
			using var createResponse = await _transport.PostAsync(createUrl, createPayload, ctx);
			if (!createResponse.IsSuccessStatusCode)
			{
				_logger.LogWarning(
					"Failed to create changelog comment on PR #{PrNumber}: {Status}",
					prNumber,
					(int)createResponse.StatusCode
				);
				return false;
			}

			_logger.LogInformation("Created changelog comment on PR #{PrNumber}", prNumber);
			return true;
		}
		catch (HttpRequestException ex)
		{
			_logger.LogWarning(ex, "HTTP error posting changelog comment on PR #{PrNumber}", prNumber);
			return false;
		}
		catch (TaskCanceledException)
		{
			_logger.LogWarning("Timeout posting changelog comment on PR #{PrNumber}", prNumber);
			return false;
		}
		catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException or ThreadAbortException))
		{
			_logger.LogWarning(ex, "Unexpected error posting changelog comment on PR #{PrNumber}", prNumber);
			return false;
		}
	}

	/// <summary>
	/// Paginates through all PR comments and returns the ID of the first comment that matches
	/// either the HTML marker or the legacy title prefix; <c>null</c> when none is found.
	/// </summary>
	private async Task<long?> FindExistingCommentIdAsync(string owner, string repo, int prNumber, Cancel ctx)
	{
		var page = 1;
		const int perPage = 100;

		while (true)
		{
			var url = $"https://api.github.com/repos/{owner}/{repo}/issues/{prNumber}/comments?per_page={perPage}&page={page}";
			using var response = await _transport.GetAsync(url, ctx);

			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning(
					"Failed to list comments for PR #{PrNumber} (page {Page}): {Status}",
					prNumber,
					page,
					(int)response.StatusCode
				);
				return null;
			}

			var json = await response.Content.ReadAsStringAsync(ctx);
			var comments = JsonSerializer.Deserialize(json, CommentJsonContext.Default.ListCommentItem);

			if (comments is null or { Count: 0 })
				return null;

			foreach (var comment in comments)
			{
				if (comment.User?.Login != "github-actions[bot]")
					continue;

				var commentBody = comment.Body ?? string.Empty;
				if (
					commentBody.Contains(HtmlMarker, StringComparison.Ordinal)
					|| commentBody.StartsWith(LegacyTitlePrefix, StringComparison.Ordinal)
				)
					return comment.Id;
			}

			// GitHub omits the Link header or returns fewer than perPage items on the last page.
			if (comments.Count < perPage)
				return null;

			page++;
		}
	}

	private sealed class CommentItem
	{
		public long Id { get; set; }
		public string? Body { get; set; }
		public CommentUser? User { get; set; }
	}

	private sealed class CommentUser
	{
		public string? Login { get; set; }
	}

	private sealed class CommentBody
	{
		[JsonPropertyName("body")]
		public string Body { get; set; } = string.Empty;
	}

	[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
	[JsonSerializable(typeof(CommentItem))]
	[JsonSerializable(typeof(List<CommentItem>))]
	[JsonSerializable(typeof(CommentBody))]
	private sealed partial class CommentJsonContext : JsonSerializerContext;
}
