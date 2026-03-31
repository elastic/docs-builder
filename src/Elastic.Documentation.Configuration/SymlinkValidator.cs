// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Security;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Configuration;

/// <summary>
/// Provides validation to ensure control files (docset.yml, toc.yml, redirects.yml)
/// are not symlinks. This prevents potential path traversal or content injection attacks
/// where a malicious symlink could point to arbitrary content outside the documentation directory.
/// </summary>
public static class SymlinkValidator
{
	/// <summary>
	/// Validates that a file is not a symlink. Throws SecurityException if it is.
	/// This prevents path traversal attacks via symlinked control files.
	/// </summary>
	/// <param name="file">The file to validate.</param>
	/// <exception cref="SecurityException">Thrown if the file is a symlink.</exception>
	public static void EnsureNotSymlink(IFileInfo file)
	{
		if (file.LinkTarget != null)
			throw new SecurityException($"Control file '{file.FullName}' is a symlink, which is not allowed for security reasons. Symlinked control files could be used for path traversal attacks.");
	}

	/// <summary>
	/// Validates that a file path does not point to a symlink. Throws SecurityException if it does.
	/// </summary>
	/// <param name="fileSystem">The file system abstraction.</param>
	/// <param name="filePath">The path to the file to validate.</param>
	/// <exception cref="SecurityException">Thrown if the file is a symlink.</exception>
	public static void EnsureNotSymlink(IFileSystem fileSystem, string filePath)
	{
		var fileInfo = fileSystem.FileInfo.New(filePath);
		if (fileInfo.Exists)
			EnsureNotSymlink(fileInfo);
	}

	/// <summary>
	/// Validates that a file and its ancestor directories (up to <paramref name="docRoot"/>) are not symlinks
	/// and that no ancestor directory is hidden (starts with <c>.</c>).
	/// </summary>
	/// <returns>An error message if validation fails, or <c>null</c> if the file is safe to read.</returns>
	public static string? ValidateFileAccess(IFileInfo file, IDirectoryInfo docRoot)
	{
		if (file.LinkTarget != null)
			return "Path must not point to a symlink.";

		var cmp = IDirectoryInfoExtensions.IsCaseSensitiveFileSystem
			? StringComparison.Ordinal
			: StringComparison.OrdinalIgnoreCase;

		var dir = file.Directory;
		while (dir != null && !string.Equals(dir.FullName, docRoot.FullName, cmp))
		{
			if (dir.Name.StartsWith('.'))
				return "Path must not traverse hidden directories.";

			if (dir.LinkTarget != null)
				return "Path must not traverse symlinked directories.";

			dir = dir.Parent;
		}

		return null;
	}

	/// <summary>
	/// Validates that a directory and its ancestor directories (up to <paramref name="docRoot"/>) are not symlinks
	/// and that no ancestor directory is hidden (starts with <c>.</c>).
	/// </summary>
	/// <returns>An error message if validation fails, or <c>null</c> if the directory is safe to access.</returns>
	public static string? ValidateDirectoryAccess(IDirectoryInfo directory, IDirectoryInfo docRoot)
	{
		if (directory.LinkTarget != null)
			return "Path must not point to a symlinked directory.";

		var cmp = IDirectoryInfoExtensions.IsCaseSensitiveFileSystem
			? StringComparison.Ordinal
			: StringComparison.OrdinalIgnoreCase;

		var dir = directory.Parent;
		while (dir != null && !string.Equals(dir.FullName, docRoot.FullName, cmp))
		{
			if (dir.Name.StartsWith('.'))
				return "Path must not traverse hidden directories.";

			if (dir.LinkTarget != null)
				return "Path must not traverse symlinked directories.";

			dir = dir.Parent;
		}

		return null;
	}
}
