// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;

namespace Elastic.Documentation.Configuration;

public static class Paths
{
	public static readonly DirectoryInfo WorkingDirectoryRoot = DetermineWorkingDirectoryRoot();

	public static readonly DirectoryInfo GitCommonRoot = InitGitCommonRoot();

	public static readonly DirectoryInfo ApplicationData = GetApplicationFolder();

	private static DirectoryInfo DetermineWorkingDirectoryRoot()
	{
		var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
		while (directory != null)
		{
			if (directory.GetFiles("*.slnx").Length > 0)
				break;
			if (directory.GetDirectories(".git").Length > 0)
				break;
			// support for git worktrees
			if (directory.GetFiles(".git").Length > 0)
				break;
			directory = directory.Parent;
		}
		return directory ?? new DirectoryInfo(Directory.GetCurrentDirectory());
	}

	public static IDirectoryInfo? DetermineSourceDirectoryRoot(IDirectoryInfo sourceDirectory)
	{
		IDirectoryInfo? sourceRoot = null;
		var directory = sourceDirectory;
		while (directory != null && directory.GetDirectories(".git").Length == 0)
		{
			if (directory.GetDirectories(".git").Length > 0)
				break;
			// support for git worktrees
			if (directory.GetFiles(".git").Length > 0)
				break;

			directory = directory.Parent;
		}
		sourceRoot ??= directory;
		return sourceRoot;
	}

	/// <summary>Resolves the root of the main git repository, following worktree links when present. Disabled on CI.</summary>
	public static IDirectoryInfo ResolveGitCommonRoot(IFileSystem fileSystem, IDirectoryInfo workingDirectoryRoot, bool? isCI = null)
	{
		if (isCI ?? !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
			return workingDirectoryRoot;

		var gitPath = Path.Combine(workingDirectoryRoot.FullName, ".git");

		if (fileSystem.Directory.Exists(gitPath))
			return workingDirectoryRoot;

		if (!fileSystem.File.Exists(gitPath))
			return workingDirectoryRoot;

		var content = fileSystem.File.ReadAllText(gitPath).Trim();
		if (!content.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase))
			return workingDirectoryRoot;

		var gitDirPath = content["gitdir:".Length..].Trim();
		if (!Path.IsPathRooted(gitDirPath))
			gitDirPath = Path.GetFullPath(gitDirPath, workingDirectoryRoot.FullName);

		var dir = fileSystem.DirectoryInfo.New(gitDirPath);
		while (dir != null && dir.Name != ".git")
			dir = dir.Parent;

		return dir?.Parent ?? workingDirectoryRoot;
	}

	private static DirectoryInfo InitGitCommonRoot()
	{
		var fs = new FileSystem();
		var root = fs.DirectoryInfo.New(WorkingDirectoryRoot.FullName);
		return new DirectoryInfo(ResolveGitCommonRoot(fs, root).FullName);
	}

	/// Used in debug to locate static folder, so we can change js/css files while the server is running
	public static DirectoryInfo? GetSolutionDirectory()
	{
		var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
		while (directory != null && directory.GetFiles("*.slnx").Length == 0)
			directory = directory.Parent;
		return directory;
	}

	// ~/Library/Application\ Support/ on osx
	// XDG_DATA_HOME or home/.local/share on linux
	// %LOCAL_APPLICATION_DATA% windows
	private static DirectoryInfo GetApplicationFolder()
	{
		var localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		var elasticPath = Path.Combine(localPath, "elastic", "docs-builder");
		return new DirectoryInfo(elasticPath);
	}

	/// <summary>
	/// Checks only the four known locations for docset.yml (root and docs/). No recursive search. Use when a fast, non-blocking check is needed (e.g. changelog init).
	/// </summary>
	public static bool TryFindDocsFolderFromKnownLocationsOnly(
		IFileSystem readFileSystem,
		IDirectoryInfo rootPath,
		[NotNullWhen(true)] out IDirectoryInfo? docDirectory,
		[NotNullWhen(true)] out IFileInfo? configurationPath
	)
	{
		docDirectory = null;
		configurationPath = null;
		var knownConfigPath = GetDocsetPathFromKnownLocations(readFileSystem, rootPath);
		if (knownConfigPath is null)
			return false;

		configurationPath = readFileSystem.FileInfo.New(knownConfigPath);
		docDirectory = configurationPath.Directory!;
		return true;
	}

	private static string? GetDocsetPathFromKnownLocations(IFileSystem readFileSystem, IDirectoryInfo rootPath)
	{
		string[] files = ["docset.yml", "_docset.yml"];
		string[] knownFolders = [rootPath.FullName, Path.Combine(rootPath.FullName, "docs")];
		var mostLikelyTargets =
			from file in files
			from folder in knownFolders
			select Path.Combine(folder, file);

		return mostLikelyTargets.FirstOrDefault(readFileSystem.File.Exists);
	}

	public static (IDirectoryInfo, IFileInfo) FindDocsFolderFromRoot(IFileSystem readFileSystem, IDirectoryInfo rootPath)
	{
		var knownConfigPath = GetDocsetPathFromKnownLocations(readFileSystem, rootPath);
		var configurationPath = knownConfigPath is null ? null : readFileSystem.FileInfo.New(knownConfigPath);
		if (configurationPath is not null)
			return (configurationPath.Directory!, configurationPath);

		configurationPath = rootPath
			.EnumerateFiles("*docset.yml", SearchOption.AllDirectories)
			.FirstOrDefault()
		?? throw new Exception($"Can not locate docset.yml file in '{rootPath}'");

		var docsFolder = configurationPath.Directory ?? throw new Exception($"Can not locate docset.yml file in '{rootPath}'");

		return (docsFolder, configurationPath);
	}

	public static bool TryFindDocsFolderFromRoot(
		IFileSystem readFileSystem,
		IDirectoryInfo rootPath,
		[NotNullWhen(true)] out IDirectoryInfo? docDirectory,
		[NotNullWhen(true)] out IFileInfo? configurationPath
	)
	{
		docDirectory = null;
		configurationPath = null;
		try
		{
			(docDirectory, configurationPath) = FindDocsFolderFromRoot(readFileSystem, rootPath);
			return true;
		}
		catch
		{
			return false;
		}
	}
}
