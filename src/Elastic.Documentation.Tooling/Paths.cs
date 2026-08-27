// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Security;
using Elastic.Documentation.Extensions;

// ReSharper disable once CheckNamespace — intentionally preserving the original namespace so consumers need no using changes
#pragma warning disable IDE0130
namespace Elastic.Documentation.Configuration;

public static class Paths
{
	public static readonly DirectoryInfo WorkingDirectoryRoot = DetermineWorkingDirectoryRoot();

	public static readonly DirectoryInfo ApplicationData = GetApplicationFolder();

	/// <summary>
	/// Walks up from <paramref name="startDirectory"/> via <see cref="IFileSystem"/> until
	/// a <c>.git</c> directory or file (worktree pointer) is found.
	/// Returns <see langword="null"/> if no git root is found within the allowed depth.
	/// </summary>
	/// <param name="startDirectory">Directory to start the upward search from (typically the docset anchor).</param>
	/// <param name="maxParents">
	/// Maximum number of parent directories to walk above <paramref name="startDirectory"/>
	/// (default: 1, i.e. self or one parent). The depth is 0-based: at depth 0 we check
	/// <paramref name="startDirectory"/> itself; at depth 1 its immediate parent, and so on.
	/// </param>
	/// <remarks>
	/// In DEBUG builds a <c>.git</c> found beyond <paramref name="maxParents"/> is still accepted
	/// when it has an adjacent <c>*.slnx</c> file — this covers the developer case of running a
	/// binary from an IDE output directory (e.g. <c>bin/Debug/net10.0/</c>) where the solution
	/// root is several levels up.
	/// </remarks>
	public static IDirectoryInfo? FindGitRoot(IDirectoryInfo startDirectory, int maxParents = 1)
	{
		var directory = startDirectory;
		var depth = 0;
		while (directory != null)
		{
			bool hasGit;
			try
			{
				hasGit = directory.GetDirectories(".git").Length > 0 || directory.GetFiles(".git").Length > 0;
			}
			catch (DirectoryNotFoundException)
			{
				// Directory does not exist in the (mock) filesystem — no .git here.
				// Continue up the tree so the caller can decide.
				hasGit = false;
			}
			catch (SecurityException)
			{
				// A ScopedFileSystem is blocking access to this directory (e.g. the scope root
				// is the anchor itself so the parent is outside scope). Stop searching.
				return null;
			}

			if (hasGit)
			{
#if DEBUG
				if (depth <= maxParents || directory.GetFiles("*.slnx").Length > 0)
					return directory;
#else
				if (depth <= maxParents)
					return directory;
#endif
				// .git found but too deep — stop searching
				return null;
			}
			if (depth >= maxParents)
			{
#if DEBUG
				// In DEBUG we keep walking so the acceptance check above can fire for a .git
				// that has an adjacent .slnx (solution root). Cap at a sane depth to avoid
				// walking the entire filesystem on pathological inputs.
				if (depth > 20)
					return null;
#else
				return null;
#endif
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
			var hasGit = directory.GetDirectories(".git").Length > 0 || directory.GetFiles(".git").Length > 0;
			if (hasGit)
			{
				if (depth <= 1)
					return directory;
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
		var mostLikelyTargets = from file in files
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

		configurationPath =
			rootPath.EnumerateFiles("*docset.yml", SearchOption.AllDirectories).FirstOrDefault()
				?? throw new Exception($"Can not locate docset.yml file in '{rootPath}'");

		var docsFolder = configurationPath.Directory ?? throw new Exception($"Can not locate docset.yml file in '{rootPath}'");

		return (docsFolder, configurationPath);
	}

	/// <summary>
	/// Resolves the real git directory from a worktree pointer (<c>.git</c> file containing
	/// <c>gitdir: &lt;path&gt;</c>). Handles both absolute and relative gitdir paths, and follows
	/// <c>commondir</c> to the shared object store when present (linked/nested worktree).
	/// </summary>
	/// <param name="fileSystem">The filesystem to read through.</param>
	/// <param name="gitFile">The <c>.git</c> file (worktree pointer) to read.</param>
	/// <param name="gitDir">
	/// On success, the resolved git directory (<c>.git/</c> or the worktrees subdirectory's
	/// parent when a <c>commondir</c> is present).
	/// </param>
	/// <returns><see langword="true"/> when the pointer was read and resolved; <see langword="false"/>
	/// when the file is absent, malformed, or the resolved path does not exist.</returns>
	public static bool TryReadGitDirPointer(IFileSystem fileSystem, IFileInfo gitFile, out IDirectoryInfo? gitDir)
	{
		gitDir = null;
		if (!fileSystem.File.Exists(gitFile.FullName))
			return false;

		var text = fileSystem.File.ReadAllText(gitFile.FullName);
		var firstLineBreak = text.IndexOfAny(['\r', '\n']);
		var firstLine = (firstLineBreak >= 0 ? text[..firstLineBreak] : text).Trim();
		if (!firstLine.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase))
			return false;

		var rawGitDir = firstLine["gitdir:".Length..].Trim();
		if (string.IsNullOrEmpty(rawGitDir))
			return false;

		// Resolve relative paths against the directory that contains the .git file
		var containingDir = gitFile.Directory?.FullName ?? string.Empty;
		var resolvedGitDir = fileSystem.Path.IsPathFullyQualified(rawGitDir)
			? rawGitDir
			: fileSystem.Path.GetFullPath(fileSystem.Path.Combine(containingDir, rawGitDir));

		if (!fileSystem.Directory.Exists(resolvedGitDir))
			return false;

		// Follow commondir to reach the shared .git root (linked/nested worktrees)
		var commonDirFile = fileSystem.Path.Combine(resolvedGitDir, "commondir");
		if (fileSystem.File.Exists(commonDirFile))
		{
			var commonDirRelative = fileSystem.File.ReadAllText(commonDirFile).Trim();
			var commonDir = fileSystem.Path.IsPathFullyQualified(commonDirRelative)
				? commonDirRelative
				: fileSystem.Path.GetFullPath(fileSystem.Path.Combine(resolvedGitDir, commonDirRelative));

			if (fileSystem.Directory.Exists(commonDir))
				resolvedGitDir = commonDir;
		}

		gitDir = fileSystem.DirectoryInfo.New(resolvedGitDir);
		return true;
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
