// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using static System.StringComparison;

namespace Elastic.Documentation.Extensions;

public static class IFileInfoExtensions
{
	public static string ReadToEnd(this IFileInfo fileInfo)
	{
		fileInfo.Refresh();
		if (!fileInfo.Exists)
			return string.Empty;

		using var stream = fileInfo.OpenRead();
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	/// Validates <paramref name="file"/> is in a subdirectory of <paramref name="parentDirectory"/>
	public static bool IsSubPathOf(this IFileInfo file, IDirectoryInfo parentDirectory)
	{
		var parent = file.Directory;
		return parent is not null && parent.IsSubPathOf(parentDirectory);
	}

	public static IFileInfo EnsureSubPathOf(this IFileInfo file, IDirectoryInfo parentDirectory, string relativePath)
	{
		var fs = file.FileSystem;
		var path = Path.GetFullPath(fs.Path.Combine(parentDirectory.FullName, relativePath));
		var pathInfo = fs.FileInfo.New(path);
		List<string> intermediaryDirectories = ["x"];
		while (!pathInfo.IsSubPathOf(parentDirectory))
		{
			path = Path.GetFullPath(fs.Path.Combine([parentDirectory.FullName, .. intermediaryDirectories, relativePath]));
			pathInfo = fs.FileInfo.New(path);
			intermediaryDirectories.Add("x");
		}

		return pathInfo;
	}

	/// Checks if <paramref name="file"/> has parent directory <paramref name="parentName"/>, defaults to OrdinalIgnoreCase comparison
	public static bool HasParent(this IFileInfo file, string parentName)
	{
		var parent = file.Directory;
		return parent is not null && parent.HasParent(parentName);
	}

	public static IFileInfo NewCombine(this IFileInfoFactory fileInfo, params string[] paths)
	{
		paths = paths.Select(f => f.OptionalWindowsReplace()).ToArray();
		var fi = fileInfo.New(Path.Combine(paths));
		return fi;
	}
}

public static class IFileSystemExtensions
{
	public static IDirectoryInfo NewDirInfo(this IFileSystem fs, string path) => fs.DirectoryInfo.New(path);

	public static IDirectoryInfo NewDirInfo(this IFileSystem fs, params string[] paths) => fs.DirectoryInfo.New(Path.Combine(paths));

	public static IFileInfo NewFileInfo(this IFileSystem fs, string path) => fs.FileInfo.NewCombine(path);

	public static IFileInfo NewFileInfo(this IFileSystem fs, params string[] paths) => fs.FileInfo.NewCombine(paths);
}

public static class IDirectoryInfoExtensions
{
	private static bool? CaseSensitiveOsCheck;
	public static bool IsCaseSensitiveFileSystem
	{

		get
		{
			// heuristic to determine if the OS is case-sensitive
			try
			{
				var tmp = Path.GetTempPath();
				if (CaseSensitiveOsCheck.HasValue)
					return CaseSensitiveOsCheck.Value;
				var culture = CultureInfo.CurrentCulture;
				CaseSensitiveOsCheck = !Directory.Exists(tmp.ToUpper(culture)) || !Directory.Exists(tmp.ToLower(culture));
				return CaseSensitiveOsCheck ?? false;

			}
			catch
			{
				// fallback to case-insensitive unless it's linux
				CaseSensitiveOsCheck = Environment.OSVersion.Platform == PlatformID.Unix;
				return false;
			}
		}
	}


	/// Validates <paramref name="directory"/> is subdirectory of <paramref name="parentDirectory"/>
	public static bool IsSubPathOf(this IDirectoryInfo directory, IDirectoryInfo parentDirectory)
	{
		var cmp = IsCaseSensitiveFileSystem ? Ordinal : OrdinalIgnoreCase;
		var parent = directory;
		do
		{
			if (string.Equals(parent.FullName, parentDirectory.FullName, cmp))
				return true;
			parent = parent.Parent;
		} while (parent != null);

		return false;
	}

	/// Checks if <paramref name="directory"/> has parent directory <paramref name="parentName"/>, defaults to OrdinalIgnoreCase comparison
	public static bool HasParent(this IDirectoryInfo directory, string parentName, StringComparison comparison = OrdinalIgnoreCase)
	{
		if (string.Equals(directory.Name, parentName, comparison))
			return true;
		var parent = directory;
		do
		{
			if (string.Equals(parent.Name, parentName, comparison))
				return true;
			parent = parent.Parent;
		} while (parent != null);

		return false;
	}

	/// Gets the first  <paramref name="parentName"/>, parent of <paramref name="directory"/>
	public static IDirectoryInfo? GetParent(this IDirectoryInfo directory, string parentName, StringComparison comparison = OrdinalIgnoreCase)
	{
		if (string.Equals(directory.Name, parentName, comparison))
			return directory;
		var parent = directory;
		do
		{
			if (string.Equals(parent.Name, parentName, comparison))
				return parent;
			parent = parent.Parent;
		} while (parent != null);

		return null;
	}
}
