// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.LegacyDocs.Migration;

public record TocEntry
{
	public string? File { get; init; }
	public string? Folder { get; init; }
	public string? Toc { get; init; }
}

public static class YamlWriter
{
	public static void WriteDocsetYaml(string path, string project, List<string> tocRefs)
	{
		var sb = new StringBuilder();
		_ = sb.Append("project: ").Append(project).Append('\n');
		_ = sb.Append("toc:\n");
		foreach (var tocRef in tocRefs)
			_ = sb.Append("  - toc: ").Append(tocRef).Append('\n');

		EnsureDirectoryAndWrite(path, sb.ToString());
	}

	public static void WriteTocYaml(string path, List<TocEntry> entries)
	{
		var sb = new StringBuilder();
		_ = sb.Append("toc:\n");
		foreach (var entry in entries)
		{
			if (entry.File is not null)
				_ = sb.Append("  - file: ").Append(entry.File).Append('\n');
			else if (entry.Folder is not null)
				_ = sb.Append("  - folder: ").Append(entry.Folder).Append('\n');
			else if (entry.Toc is not null)
				_ = sb.Append("  - toc: ").Append(entry.Toc).Append('\n');
		}

		EnsureDirectoryAndWrite(path, sb.ToString());
	}

	private static void EnsureDirectoryAndWrite(string path, string content)
	{
		var directory = Path.GetDirectoryName(path);
		if (directory is not null)
			_ = Directory.CreateDirectory(directory);

		File.WriteAllText(path, content);
	}
}
