// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

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

	/// Checks if <paramref name="file"/> has parent directory <paramref name="parentName"/>
	public static bool HasParent(this IFileInfo file, string parentName)
	{
		var parent = file.Directory;
		return parent is not null && parent.HasParent(parentName);
	}
}

public static class IDirectoryInfoExtensions
{
	/// Validates <paramref name="directory"/> is subdirectory of <paramref name="parentDirectory"/>
	public static bool IsSubPathOf(this IDirectoryInfo directory, IDirectoryInfo parentDirectory)
	{
		var parent = directory;
		do
		{
			if (parent.FullName == parentDirectory.FullName)
				return true;
			parent = parent.Parent;
		} while (parent != null);

		return false;
	}

	/// Checks if <paramref name="directory"/> has parent directory <paramref name="parentName"/>
	public static bool HasParent(this IDirectoryInfo directory, string parentName)
	{
		if (directory.Name == parentName)
			return true;
		var parent = directory;
		do
		{
			if (parent.Name == parentName)
				return true;
			parent = parent.Parent;
		} while (parent != null);

		return false;
	}
}
