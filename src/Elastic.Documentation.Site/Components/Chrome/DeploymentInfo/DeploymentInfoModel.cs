// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Chrome.DeploymentInfo;

public sealed record DeploymentRow(string Label, string Value, string Icon, string? Href);

/// <summary>Where a preview build came from: its branch, commit, repository and, for a pull request, its number.</summary>
public sealed record DeploymentInfoModel
{
	private const string GitHub = "https://github.com";
	private const string PullPrefix = "refs/pull/";
	private const string PullSuffix = "/merge";
	private const string BranchPrefix = "refs/heads/";
	private const string TagPrefix = "refs/tags/";

	public required string Branch { get; init; }
	public string? Commit { get; init; }
	public string? Repository { get; init; }

	/// <summary>The full ref from GitHub Actions, for example <c>refs/pull/123/merge</c>.</summary>
	public string? GitHubRef { get; init; }

	private string? PullNumber
	{
		get
		{
			if (
				GitHubRef is not { } gitRef
				|| !gitRef.StartsWith(PullPrefix, StringComparison.Ordinal)
				|| !gitRef.EndsWith(PullSuffix, StringComparison.Ordinal)
			)
				return null;
			var number = gitRef[PullPrefix.Length..^PullSuffix.Length];
			return number.Length > 0 && number.All(char.IsAsciiDigit) ? number : null;
		}
	}

	private string? RefName
	{
		get
		{
			if (GitHubRef is not { } gitRef)
				return null;
			if (PullNumber is { } number)
				return $"#{number}";
			if (gitRef.StartsWith(BranchPrefix, StringComparison.Ordinal))
				return gitRef[BranchPrefix.Length..];
			return gitRef.StartsWith(TagPrefix, StringComparison.Ordinal) ? gitRef[TagPrefix.Length..] : gitRef;
		}
	}

	public string Subtitle
	{
		get
		{
			if (GitHubRef is not { } gitRef)
				return "Preview build";
			if (PullNumber is { } number)
				return $"Preview build from pull request #{number}";
			if (gitRef.StartsWith(BranchPrefix, StringComparison.Ordinal))
				return $"Preview build from branch {RefName}";
			return gitRef.StartsWith(TagPrefix, StringComparison.Ordinal) ? $"Preview build from tag {RefName}" : "Preview build";
		}
	}

	private string? Base => Repository is null ? null : $"{GitHub}/{(Repository.Contains('/') ? Repository : $"elastic/{Repository}")}";

	public IReadOnlyList<DeploymentRow> Rows
	{
		get
		{
			var rows = new List<DeploymentRow>();
			if (RefName is { } refName)
				rows.Add(
					new DeploymentRow(
						"PR",
						refName,
						"pull_request",
						PullNumber is { } number && Base is { } pullBase ? $"{pullBase}/pull/{number}" : null
					)
				);
			rows.Add(new DeploymentRow("Branch", Branch, "branch", Base is { } branchBase ? $"{branchBase}/tree/{Branch}" : null));
			if (Commit is { } commit)
				rows.Add(new DeploymentRow("Commit", commit, "commit", Base is { } commitBase ? $"{commitBase}/commit/{commit}" : null));
			if (Repository is { } repo)
				rows.Add(new DeploymentRow("Repository", repo, "logo_github", Base));
			return rows;
		}
	}
}
