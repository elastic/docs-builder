// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Configuration;

public static class Paths
{
	public static readonly DirectoryInfo WorkingDirectoryRoot = DetermineWorkingDirectoryRoot();

	public static readonly DirectoryInfo ApplicationData = GetApplicationFolder();

	/// <summary>
	/// Walks up from <paramref name="startPath"/> until a <c>.git</c> directory or file
	/// (worktree pointer) is found and returns that ancestor. Returns <paramref name="startPath"/>
	/// itself when no git root is found within the allowed depth.
	/// </summary>
	/// <remarks>
	/// Depth protection: in release builds the <c>.git</c> anchor must be at most 1 directory
	/// above <paramref name="startPath"/> — documentation is not expected to live deep inside
	/// a repo. In debug builds a deeper <c>.git</c> is accepted when a <c>*.slnx</c> file is
	/// adjacent (developer running the binary from an IDE output directory).
	/// </remarks>
	public static string FindGitRoot(string startPath)
	{
		var resolved = Path.IsPathRooted(startPath) ? startPath : Path.GetFullPath(startPath);
		var dir = Directory.Exists(resolved)
			? new DirectoryInfo(resolved)
			: new DirectoryInfo(Path.GetDirectoryName(resolved) ?? resolved);
		var startDir = dir.FullName; // always a directory, used as fallback
		var depth = 0;
		while (dir != null)
		{
			var hasGit = dir.GetDirectories(".git").Length > 0 || dir.GetFiles(".git").Length > 0;
			if (hasGit)
			{
#if DEBUG
				if (depth <= 1 || dir.GetFiles("*.slnx").Length > 0)
					return dir.FullName;
#else
				if (depth <= 1)
					return dir.FullName;
#endif
				// .git found but too deep — stop searching
				return startDir;
			}
			depth++;
			dir = dir.Parent;
		}
		return startDir;
	}

	/// <summary>
	/// Walks up from <paramref name="startDirectory"/> via <see cref="IFileSystem"/> until
	/// a <c>.git</c> directory or file (worktree pointer) is found.
	/// Returns <see langword="null"/> if no git root is found within the allowed depth.
	/// </summary>
	/// <param name="startDirectory">Directory to start the upward search from.</param>
	/// <param name="ceiling">
	/// Optional upper bound for the search. When provided, the walk may reach <paramref name="ceiling"/>
	/// but never goes above it, replacing the fixed depth limit with a directory boundary.
	/// When <see langword="null"/>, the original depth-1 limit applies.
	/// </param>
	/// <remarks>
	/// Without a ceiling the same depth protection as <see cref="FindGitRoot(string)"/> applies.
	/// With a ceiling the caller guarantees the boundary is trustworthy (e.g. the working directory
	/// root), so any <c>.git</c> found at or below it is accepted regardless of depth.
	/// </remarks>
	public static IDirectoryInfo? FindGitRoot(IDirectoryInfo startDirectory, IDirectoryInfo? ceiling = null)
	{
		var directory = startDirectory;
		var depth = 0;
		while (directory != null)
		{
			if (ceiling is not null && !directory.IsSubPathOf(ceiling))
				return null;

			var hasGit = directory.GetDirectories(".git").Length > 0
					  || directory.GetFiles(".git").Length > 0;
			if (hasGit)
			{
				if (ceiling is not null)
					return directory;
#if DEBUG
				if (depth <= 1 || directory.GetFiles("*.slnx").Length > 0)
					return directory;
#else
				if (depth <= 1)
					return directory;
#endif
				// .git found but too deep
				return null;
			}
			depth++;
			directory = directory.Parent;
		}
		return null;
	}

	private static DirectoryInfo DetermineWorkingDirectoryRoot()
	{
		var cwd = new DirectoryInfo(Directory.GetCurrentDirectory());
		var directory = cwd;
		var depth = 0;
		while (directory != null)
		{
			// *.slnx is the primary anchor: always adopt it at any depth.
			// This covers both the local developer case (running from the IDE output directory
			// such as bin/Debug/net10.0/) and CI (Aspire starts the binary from the project
			// directory, which is several levels below the solution root).
			if (directory.GetFiles("*.slnx").Length > 0)
				return directory;
			var hasGit = directory.GetDirectories(".git").Length > 0
					  || directory.GetFiles(".git").Length > 0;
			if (hasGit)
			{
				// Only accept .git beyond 1 level up in debug when a *.slnx is adjacent
				// (developer running from IDE output directory such as bin/Debug/net10.0/).
#if DEBUG
				if (depth <= 1 || directory.GetFiles("*.slnx").Length > 0)
					return directory;
#else
				if (depth <= 1)
					return directory;
#endif
				// .git found but too deep — stop without adopting it
				return cwd;
			}
			depth++;
			directory = directory.Parent;
		}
		return cwd;
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
		if (string.IsNullOrEmpty(localPath))
		{
			// Docker / CI containers often have no XDG_DATA_HOME or HOME configured,
			// causing LocalApplicationData to return "". Path.Join("", ...) produces a
			// relative path that resolves under CWD, becoming a subdirectory of
			// WorkingDirectoryRoot and breaking the disjoint-scope-roots requirement.
			localPath = Path.GetTempPath();
		}
		var elasticPath = Path.Join(localPath, "elastic", "docs-builder");
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
		string[] knownFolders = [rootPath.FullName, Path.Join(rootPath.FullName, "docs")];
		var mostLikelyTargets =
			from file in files
			from folder in knownFolders
			select Path.Join(folder, file);

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

	/// <summary>Validates that <paramref name="value"/> is a single path segment with no separators or traversal components.
	/// Throws <see cref="ArgumentException"/> when the value is blank, contains separators, or equals "." / "..".</summary>
	public static void ValidateSinglePathSegment(string value, string paramName)
	{
		if (string.IsNullOrWhiteSpace(value) || Path.GetFileName(value) != value || value == "." || value == "..")
			throw new ArgumentException($"'{paramName}' must be a single relative path segment.", paramName);
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
