// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace Elastic.Markdown.Tests;

public static class MockFileSystemExtensions
{
	public static void GenerateDocSetYaml(this MockFileSystem fileSystem, IDirectoryInfo root)
	{
		// language=yaml
		var yaml = new StringWriter();
		yaml.WriteLine("toc:");
		var markdownFiles = fileSystem.Directory
			.EnumerateFiles(root.FullName, "*.md", SearchOption.AllDirectories);
		foreach (var markdownFile in markdownFiles)
		{
			var relative = fileSystem.Path.GetRelativePath(root.FullName, markdownFile);
			yaml.WriteLine($" - file: {relative}");
		}
		fileSystem.AddFile(Path.Combine(root.FullName, "docset.yml"), new MockFileData(yaml.ToString()));
	}

}
