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
		_ = sb.Append("project: ").AppendLine(project);
		_ = sb.AppendLine("toc:");
		foreach (var tocRef in tocRefs)
			_ = sb.Append("  - toc: ").AppendLine(tocRef);

		EnsureDirectoryAndWrite(path, sb.ToString());
	}

	public static void WriteTocYaml(string path, List<TocEntry> entries)
	{
		var sb = new StringBuilder();
		_ = sb.AppendLine("toc:");
		foreach (var entry in entries)
		{
			if (entry.File is not null)
				_ = sb.Append("  - file: ").AppendLine(entry.File);
			else if (entry.Folder is not null)
				_ = sb.Append("  - folder: ").AppendLine(entry.Folder);
			else if (entry.Toc is not null)
				_ = sb.Append("  - toc: ").AppendLine(entry.Toc);
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
