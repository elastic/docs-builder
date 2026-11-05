// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;

namespace Elastic.Documentation.Testing;

public static class MockFileSystemDump
{
	/// <summary>
	/// Every file under <paramref name="directory"/>, sorted by path, each under an <c>=== relative/path ===</c>
	/// header. Meant for tests that read generated files back from a mock file system.
	/// </summary>
	public static string ToText(IFileSystem fileSystem, string directory, string searchPattern = "*")
	{
		var root = fileSystem.Path.GetFullPath(directory);
		var sb = new StringBuilder();
		var files = fileSystem
			.Directory
			.EnumerateFiles(root, searchPattern, SearchOption.AllDirectories)
			.Select(f => (Full: f, Relative: fileSystem.Path.GetRelativePath(root, f).Replace('\\', '/')))
			.OrderBy(f => f.Relative, StringComparer.Ordinal);
		foreach (var (full, relative) in files)
		{
			sb.Append("=== ").Append(relative).Append(" ===\n");
			var content = fileSystem.File.ReadAllText(full).Replace("\r\n", "\n");
			sb.Append(content);
			if (!content.EndsWith('\n'))
				sb.Append('\n');
		}
		return sb.ToString();
	}
}
