// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Evaluation;

/// <summary>
/// Service implementing the hidden <c>changelog github-decision</c> command.
/// Reads the decision metadata file, amends it with the commit-step outcome, and writes it back.
/// Deliberately trivial — all steering logic lives in <see cref="ChangelogGithubCommentService"/>.
/// </summary>
public class ChangelogGithubDecisionService(ILoggerFactory logFactory, IRunnerTempFileSystem fileSystem) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogGithubDecisionService>();
	private readonly GithubDecisionMetadataWriter _writer = new(logFactory, fileSystem);

	/// <summary>
	/// Reads the metadata at <paramref name="input"/>.<see cref="GithubDecisionArguments.MetadataPath"/>,
	/// sets <see cref="GithubDecisionMetadata.CommitOutcome"/> and
	/// <see cref="GithubDecisionMetadata.CommittedFile"/>, then writes it back.
	/// </summary>
	public async Task<bool> RecordDecision(GithubDecisionArguments input, Cancel ctx)
	{
		var existing = await _writer.ReadAsync(input.MetadataPath, ctx);
		if (existing is null)
		{
			_logger.LogWarning("Decision metadata not found at {Path} — nothing to amend", input.MetadataPath);
			return true;
		}

		var updated = existing with { CommitOutcome = input.CommitOutcome, CommittedFile = input.CommittedFile };

		await _writer.WriteAsync(updated, ctx);
		_logger.LogInformation("Recorded commit outcome {Outcome} for PR #{PrNumber}", input.CommitOutcome, updated.PrNumber);
		return true;
	}
}

/// <summary>Arguments for the hidden <c>changelog github-decision</c> command.</summary>
public record GithubDecisionArguments
{
	public required string MetadataPath { get; init; }
	public required CommitOutcome CommitOutcome { get; init; }
	public string? CommittedFile { get; init; }
}
