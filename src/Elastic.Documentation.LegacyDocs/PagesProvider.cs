// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.LegacyDocs;

public interface IPagesProvider
{
	IEnumerable<string> GetPages();
}

/// <summary>
/// Gets pages from a local checked-out elastic/built-docs repository
/// </summary>
/// <param name="gitRepositoryPath"></param>
public class LocalPagesProvider(string gitRepositoryPath) : IPagesProvider
{
	public IEnumerable<string> GetPages() =>
		Directory.EnumerateFiles(Path.Combine(gitRepositoryPath, "html", "en"), "*.html", SearchOption.AllDirectories)
			.Select(i =>
			{
				var relativePath = Path.GetRelativePath(Path.Combine(gitRepositoryPath, "html", "en"), i).Replace('\\', '/');
				return relativePath;
			});
}
