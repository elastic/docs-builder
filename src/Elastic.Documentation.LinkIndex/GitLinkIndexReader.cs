// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.IO.Abstractions;
using Elastic.Documentation.Links;

namespace Elastic.Documentation.LinkIndex;

/// <summary>
/// Reads the link index from a cloned git repository (elastic/codex-link-index).
/// Uses local SSH credentials for cloning, enabling private access without S3.
/// </summary>
public class GitLinkIndexReader : ILinkIndexReader, IDisposable
{
	private const string LinkIndexOrigin = "elastic/codex-link-index";
	private static readonly string CloneDirectory = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
		".docs-builder",
		"codex-link-index");

	private readonly string _environment;
	private readonly IFileSystem _fileSystem;
	private readonly SemaphoreSlim _cloneLock = new(1, 1);
	private bool _ensuredClone;

	public GitLinkIndexReader(string environment, IFileSystem? fileSystem = null)
	{
		if (string.IsNullOrWhiteSpace(environment))
			throw new ArgumentException("Environment must be specified in the codex configuration (e.g., 'engineering', 'security').", nameof(environment));

		_environment = environment;
		_fileSystem = fileSystem ?? new FileSystem();
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_cloneLock.Dispose();
		GC.SuppressFinalize(this);
	}

	public string RegistryUrl => "https://github.com/elastic/codex-link-index";

	/// <inheritdoc />
	public async Task<LinkRegistry> GetRegistry(Cancel cancellationToken = default)
	{
		await EnsureCloneAsync(cancellationToken);
		if (Path.IsPathRooted(_environment))
			throw new ArgumentException($"Environment '{_environment}' must be a relative path segment.");
		var registryPath = Path.Combine(CloneDirectory, _environment, "link-index.json");
		if (!_fileSystem.File.Exists(registryPath))
			throw new FileNotFoundException($"Link index registry not found at {registryPath}. Ensure the codex-link-index repository has {_environment}/link-index.json.");

		var json = await _fileSystem.File.ReadAllTextAsync(registryPath, cancellationToken);
		return LinkRegistry.Deserialize(json);
	}

	/// <inheritdoc />
	public async Task<RepositoryLinks> GetRepositoryLinks(string key, Cancel cancellationToken = default)
	{
		await EnsureCloneAsync(cancellationToken);
		if (Path.IsPathRooted(key))
			throw new ArgumentException($"Repository key '{key}' must be a relative path.", nameof(key));
		var linksPath = Path.Combine(CloneDirectory, key);
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

			var cloneDir = _fileSystem.DirectoryInfo.New(CloneDirectory);
			var gitDir = Path.Combine(CloneDirectory, ".git");
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

	private static void RunGit(string workingDirectory, params string[] args)
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
			throw new InvalidOperationException($"Git command failed (exit {process.ExitCode}): {stderr.Trim()}");
	}
}
