// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Links;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.LinkIndex;

/// <summary>
/// Reads the link index from a cloned git repository (elastic/codex-link-index).
/// Uses local SSH credentials for cloning, enabling private access without S3.
/// </summary>
public class GitLinkIndexReader : ILinkIndexReader, IDisposable
{
	private const string LinkIndexOrigin = "elastic/codex-link-index";
	private static readonly string CloneDirectory = Path.Join(
		Paths.ApplicationData.FullName,
		"codex-link-index");

	private readonly string _environment;
	private readonly IFileSystem _fileSystem;
	private readonly IEnvironmentVariables _environmentVariables;
	private readonly bool _skipFetch;
	private readonly SemaphoreSlim _cloneLock = new(1, 1);
	private bool _ensuredClone;

	public GitLinkIndexReader(
		string environment,
		ApplicationDataFileSystem? fileSystem = null,
		bool skipFetch = false,
		IEnvironmentVariables? environmentVariables = null)
	{
		if (string.IsNullOrWhiteSpace(environment))
			throw new ArgumentException("Environment must be specified in the codex configuration (e.g., 'internal', 'security').", nameof(environment));

		_environment = environment;
		_fileSystem = fileSystem ?? new ApplicationDataFileSystem();
		_skipFetch = skipFetch;
		_environmentVariables = environmentVariables ?? SystemEnvironmentVariables.Instance;
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
			throw new FileNotFoundException($"Link index registry not found at {registryPath}. Ensure the codex-link-index repository has {_environment}/link-index.json.");

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
						$"Codex link index not found at {CloneDirectory}. Run 'docs-builder codex clone' first.");
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

			RunGit(CloneDirectory, "fetch", "--no-tags", "--prune", "--depth", "1", "origin", "HEAD");
			RunGit(CloneDirectory, "checkout", "--force", "FETCH_HEAD");

			_ensuredClone = true;
		}
		finally
		{
			_ = _cloneLock.Release();
		}
	}

	private string GetCodexLinkIndexGitUrl()
	{
		if (_environmentVariables.IsRunningOnCI)
		{
			var token = _environmentVariables.GetEnvironmentVariable("GITHUB_TOKEN");
			return !string.IsNullOrEmpty(token)
				? $"https://oauth2:{token}@github.com/{LinkIndexOrigin}.git"
				: $"https://github.com/{LinkIndexOrigin}.git";
		}

		return $"git@github.com:{LinkIndexOrigin}.git";
	}

	private void RunGit(string workingDirectory, params string[] args)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = "git",
			WorkingDirectory = workingDirectory,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};
		foreach (var arg in args)
			startInfo.ArgumentList.Add(arg);
		startInfo.Environment["GIT_EDITOR"] = "true";

		using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start git process.");

		var stderr = process.StandardError.ReadToEnd();
		_ = process.StandardOutput.ReadToEnd();
		process.WaitForExit();

		if (process.ExitCode != 0)
			throw new InvalidOperationException(DescribeCloneFailure(
				stderr,
				_environmentVariables.IsRunningOnCI,
				!string.IsNullOrEmpty(_environmentVariables.GetEnvironmentVariable("GITHUB_TOKEN"))));
	}

	internal static string DescribeCloneFailure(string gitStderr, bool onActions, bool hasToken)
	{
		var message = $"Git clone failed: {gitStderr.Trim()}";

		if (onActions && !hasToken)
			return $"{message}{Environment.NewLine}{Environment.NewLine}"
				+ "GitHub Actions did not provide GITHUB_TOKEN for the private Elastic Internal Docs link index."
				+ $"{Environment.NewLine}Fork pull_request jobs do not receive the OIDC token needed to fetch this token. Push fork branches to the upstream repository."
				+ $"{Environment.NewLine}For same-repository jobs, confirm permissions.id-token: write and the catalog-info token policy.";

		return !onActions
			? $"{message}{Environment.NewLine}{Environment.NewLine}Run 'docs-builder codex clone' first, or ensure SSH access to github.com works for git@github.com:elastic/codex-link-index.git."
			: message;
	}
}
