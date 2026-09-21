// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Http;

/// <summary>Resolves a generated API CommonMark file from a preview or static request slug.</summary>
public static class ApiMarkdownRequest
{
	public static string ResolveFile(string apiRoot, string slug)
	{
		var trimmed = slug.Trim('/');
		if (trimmed.Length == 0 || trimmed.Equals("api.md", StringComparison.OrdinalIgnoreCase))
			return Path.GetFullPath(Path.Join(Directory.GetParent(apiRoot)!.FullName, "api.md"));

		if (trimmed.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
			return Path.GetFullPath(Path.Join(apiRoot, trimmed.Replace('/', Path.DirectorySeparatorChar)));

		return Path.GetFullPath(Path.Join(apiRoot, trimmed.Replace('/', Path.DirectorySeparatorChar) + ".md"));
	}

	public static string SiblingOfDirectory(string directoryPath)
	{
		var directory = new DirectoryInfo(directoryPath);
		return Path.GetFullPath(Path.Join(directory.Parent!.FullName, directory.Name + ".md"));
	}
}
