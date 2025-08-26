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

	/// Checks if <paramref name="file"/> has parent directory <paramref name="parentName"/>, defaults to OrdinalIgnoreCase comparison
	public static bool HasParent(this IFileInfo file, string parentName)
	{
		var parent = file.Directory;
		return parent is not null && parent.HasParent(parentName);
	}
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
}
