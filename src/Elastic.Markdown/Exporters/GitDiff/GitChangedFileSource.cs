// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.IO.Abstractions;
using Elastic.Documentation;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters.GitDiff;

internal sealed class GitChangedFileSource(
	ILoggerFactory logFactory,
	IDirectoryInfo checkoutDirectory,
	string docsetPrefix,
	IEnvironmentVariables environment,
	Func<string[], string>? gitCommand = null
)
{
	private readonly ILogger _logger = logFactory.CreateLogger<GitChangedFileSource>();

	public ChangedFileSourceResult GetChanges()
	{
		if (IntegrationChangedFileSource.HasFileList(environment))
		{
			var listedBase = environment.GetEnvironmentVariable("DOCS_DIFF_BASE")
				?? environment.GetEnvironmentVariable("GITHUB_BASE_REF")
				?? "ci";
			return IntegrationChangedFileSource.GetChanges(docsetPrefix, environment, listedBase);
		}

		var diffBase = ResolveDiffBase();
		return new ChangedFileSourceResult(diffBase, RunGitDiff(diffBase));
	}

	private string ResolveDiffBase()
	{
		var explicitBase = environment.GetEnvironmentVariable("DOCS_DIFF_BASE");
		if (!string.IsNullOrWhiteSpace(explicitBase))
			return explicitBase.Trim();

		var githubBaseRef = environment.GetEnvironmentVariable("GITHUB_BASE_REF");
		if (!string.IsNullOrWhiteSpace(githubBaseRef))
			return $"origin/{githubBaseRef.Trim()}";

		foreach (var candidate in new[] { "main", "master" })
		{
			var output = GitCommand("merge-base", "-a", "HEAD", candidate);
			if (IsUsableGitOutput(output))
				return candidate;
		}

		var originHead = GitCommand("symbolic-ref", "refs/remotes/origin/HEAD");
		if (IsUsableGitOutput(originHead))
		{
			var parts = originHead.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (parts.Length >= 2)
				return $"{parts[^2]}/{parts[^1]}";
		}

		var headParent = GitCommand("rev-parse", "--verify", "HEAD^1");
		if (IsUsableGitOutput(headParent))
			return "HEAD^1";

		return "main";
	}

	private static bool IsUsableGitOutput(string output) =>
		output.Length > 0 && !output.StartsWith("fatal", StringComparison.Ordinal);

	private IReadOnlyList<SourceFileChange> RunGitDiff(string diffBase)
	{
		var lookupPath = GitDiffPathNormalization.Normalize(docsetPrefix);
		var args = new List<string> { "diff", "--name-status", "-z", diffBase, "HEAD" };
		if (!string.IsNullOrEmpty(lookupPath))
			args.AddRange(["--", $"./{lookupPath}"]);

		var output = GitCommand([.. args]);
		return output.Length == 0 ? [] : ParseNameStatus(output);
	}

	internal static IReadOnlyList<SourceFileChange> ParseNameStatus(string output)
	{
		var changes = new List<SourceFileChange>();
		var parts = output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
		for (var i = 0; i < parts.Length;)
		{
			var status = parts[i++];
			if (status.Length == 0)
				continue;

			if (status[0] is 'R' or 'C')
			{
				if (i + 1 >= parts.Length)
					break;

				var oldPath = parts[i++];
				var newPath = parts[i++];
				changes.Add(new SourceFileChange(oldPath, SourceFileChangeType.Renamed, newPath));
				continue;
			}

			if (i >= parts.Length)
				break;

			var path = parts[i++];
			var changeType = status[0] switch
			{
				'A' => SourceFileChangeType.Added,
				'M' => SourceFileChangeType.Modified,
				'D' => SourceFileChangeType.Deleted,
				_ => SourceFileChangeType.Modified
			};
			changes.Add(new SourceFileChange(path, changeType));
		}

		return changes;
	}

	private string GitCommand(params string[] args) =>
		gitCommand is not null ? gitCommand(args) : RunGitProcess(args);

	private string RunGitProcess(string[] args)
	{
		try
		{
			var startInfo = new ProcessStartInfo("git")
			{
				WorkingDirectory = checkoutDirectory.FullName,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};
			foreach (var arg in args)
				startInfo.ArgumentList.Add(arg);

			using var process = Process.Start(startInfo);
			if (process is null)
				return string.Empty;

			var stdout = process.StandardOutput.ReadToEndAsync();
			var stderr = process.StandardError.ReadToEndAsync();
			if (!process.WaitForExit(30_000))
			{
				process.Kill(entireProcessTree: true);
				_logger.LogWarning("git {Args} timed out after 30s", string.Join(' ', args));
				return string.Empty;
			}

			_ = stderr.GetAwaiter().GetResult();
			if (process.ExitCode != 0)
			{
				_logger.LogWarning("git {Args} failed with exit code {ExitCode}", string.Join(' ', args), process.ExitCode);
				return string.Empty;
			}

			return stdout.GetAwaiter().GetResult();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to run git {Args}", string.Join(' ', args));
			return string.Empty;
		}
	}
}
