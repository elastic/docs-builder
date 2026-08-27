// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.ExternalCommands;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Links;
using Nullean.ScopedFileSystem;
using ProcNet;

namespace Elastic.Documentation.LinkIndex;

/// <summary>
/// Reads the link index from a cloned git repository (elastic/codex-link-index).
/// Uses local SSH credentials for cloning, enabling private access without S3.
/// </summary>
public class GitLinkIndexReader : ILinkIndexReader, IDisposable
{
	private const string LinkIndexOrigin = "elastic/codex-link-index";
	private static readonly string CloneDirectory = Path.Join(Paths.ApplicationData.FullName, "codex-link-index");

	private static readonly Dictionary<string, string> GitEnvironmentVars = new() { { "GIT_EDITOR", "true" } };

	// Fetch retries up to 3 times with exponential back-off, each bounded by the CI default timeout.
	private static readonly RetryPolicy FetchRetry = new(
		MaxAttempts: 3,
		BaseDelay: TimeSpan.FromSeconds(5),
		AttemptTimeout: GitTimeouts.CiDefault
	);

	private readonly string _environment;
	private readonly IFileSystem _fileSystem;
	private readonly bool _skipFetch;
	private readonly SemaphoreSlim _cloneLock = new(1, 1);
	private bool _ensuredClone;

	public GitLinkIndexReader(string environment, ApplicationDataFileSystem? fileSystem = null, bool skipFetch = false)
	{
		if (string.IsNullOrWhiteSpace(environment))
			throw new ArgumentException(
				"Environment must be specified in the codex configuration (e.g., 'internal', 'security').",
				nameof(environment)
			);

		_environment = environment;
		_fileSystem = fileSystem ?? new ApplicationDataFileSystem();
		_skipFetch = skipFetch;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_cloneLock.Dispose();
		GC.SuppressFinalize(this);
	}

	public string RegistryUrl => "https://github.com/elastic/codex-link-index";

	private static void EnsureSafeRelativePath(string value, string paramName)
	{
		if (Path.IsPathRooted(value))
			throw new ArgumentException($"'{paramName}' must be a relative path.", paramName);
		var root = Path.GetFullPath(CloneDirectory + Path.DirectorySeparatorChar);
		var normalized = Path.GetFullPath(Path.Join(CloneDirectory, value));
		if (!normalized.StartsWith(root, StringComparison.Ordinal))
			throw new ArgumentException($"'{paramName}' contains invalid traversal segments.", paramName);
	}

	/// <inheritdoc />
	public async Task<LinkRegistry> GetRegistry(Cancel cancellationToken = default)
	{
		await EnsureCloneAsync(cancellationToken);
		EnsureSafeRelativePath(_environment, nameof(_environment));
		var registryPath = Path.Join(CloneDirectory, _environment, "link-index.json");
		if (!_fileSystem.File.Exists(registryPath))
			throw new FileNotFoundException(
				$"Link index registry not found at {registryPath}. Ensure the codex-link-index repository has {_environment}/link-index.json."
			);

		var json = await _fileSystem.File.ReadAllTextAsync(registryPath, cancellationToken);
		return LinkRegistry.Deserialize(json);
	}

	/// <inheritdoc />
	public async Task<RepositoryLinks> GetRepositoryLinks(string key, Cancel cancellationToken = default)
	{
		await EnsureCloneAsync(cancellationToken);
		EnsureSafeRelativePath(key, nameof(key));
		var linksPath = Path.Join(CloneDirectory, key);
		if (!_fileSystem.File.Exists(linksPath))
			throw new FileNotFoundException($"Repository links not found at {linksPath}.");

		var json = await _fileSystem.File.ReadAllTextAsync(linksPath, cancellationToken);
		return RepositoryLinks.Deserialize(json);
	}

	private async Task EnsureCloneAsync(Cancel cancellationToken)
	{
		await _cloneLock.WaitAsync(cancellationToken);
		try
		{
			if (_ensuredClone)
				return;

			var gitDir = Path.Join(CloneDirectory, ".git");
			if (_skipFetch)
			{
				if (!_fileSystem.Directory.Exists(gitDir))
					throw new InvalidOperationException(
						$"Codex link index not found at {CloneDirectory}. Run 'docs-builder codex clone' first."
					);
				_ensuredClone = true;
				return;
			}

			var cloneDir = _fileSystem.DirectoryInfo.New(CloneDirectory);
			var gitUrl = GetCodexLinkIndexGitUrl();

			if (!_fileSystem.Directory.Exists(gitDir))
			{
				if (!cloneDir.Exists)
					cloneDir.Create();
				RunGit(CloneDirectory, "init");
				RunGit(CloneDirectory, "remote", "add", "origin", gitUrl);
			}

			RunGitWithRetry(CloneDirectory, FetchRetry, "fetch", "--no-tags", "--prune", "--depth", "1", "origin", "HEAD");
			RunGit(CloneDirectory, "checkout", "--force", "FETCH_HEAD");

			_ensuredClone = true;
		}
		finally
		{
			_ = _cloneLock.Release();
		}
	}

	private static string GetCodexLinkIndexGitUrl()
	{
		if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
		{
			var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
			return !string.IsNullOrEmpty(token)
				? $"https://oauth2:{token}@github.com/{LinkIndexOrigin}.git"
				: $"https://github.com/{LinkIndexOrigin}.git";
		}

		return $"git@github.com:{LinkIndexOrigin}.git";
	}

	/// <summary>
	/// Runs a git command with a retry policy. Throws <see cref="InvalidOperationException"/> on exhaustion.
	/// </summary>
	private static void RunGitWithRetry(string workingDirectory, RetryPolicy policy, params string[] args)
	{
		var failure = CommandRetry.Invoke(
			policy,
			invoke: () => ExecGit(workingDirectory, args, policy.AttemptTimeout),
			delay: d => Thread.Sleep(d),
			onRetry: f => Console.Error.WriteLine($"[git {string.Join(" ", args)}] {f}; retrying…")
		);

		if (failure is not null)
			throw new InvalidOperationException($"Git command failed after {policy.MaxAttempts} attempts (last: {failure.Value}).");
	}

	/// <summary>
	/// Runs a single git command with no retry. Throws <see cref="InvalidOperationException"/> on failure.
	/// </summary>
	private static void RunGit(string workingDirectory, params string[] args)
	{
		var exitCode = ExecGit(workingDirectory, args, timeout: null);
		if (exitCode != 0)
			throw new InvalidOperationException($"Git command failed (exit {exitCode}): git {string.Join(" ", args)}");
	}

	private static int ExecGit(string workingDirectory, string[] args, TimeSpan? timeout)
	{
		var arguments = new ExecArguments("git", args)
		{
			WorkingDirectory = workingDirectory,
			Environment = GitEnvironmentVars,
			Timeout = timeout
		};
		return Proc.Exec(arguments);
	}
}
