// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Documentation.Generator.Domain;

public static class Paths
{
	private static DirectoryInfo RootDirectoryInfo()
	{
		var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
		while (directory != null &&
		       (directory.GetFiles("*.sln").Length == 0 || directory.GetDirectories(".git").Length == 0))
			directory = directory.Parent;
		return directory ?? new DirectoryInfo(Directory.GetCurrentDirectory());
	}

	public static readonly DirectoryInfo Root = RootDirectoryInfo();
}

public record FolderName
{
	public required string Folder { get; init; }
}

public record Folder
{
	public required string Path { get; init; }
	public required MarkdownFile[] Files { get; init; }
}
