// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;

namespace Elastic.Documentation.Navigation.Tests.Isolation;

public abstract class DocumentationSetNavigationTestBase(ITestOutputHelper output)
{
	protected TestDocumentationSetContext CreateContext(MockFileSystem? fileSystem = null)
	{
		fileSystem ??= new MockFileSystem();
		var sourceDir = fileSystem.DirectoryInfo.New("/docs");
		var outputDir = fileSystem.DirectoryInfo.New("/output");
		var configPath = fileSystem.FileInfo.New("/docs/docset.yml");

		return new TestDocumentationSetContext(fileSystem, sourceDir, outputDir, configPath, output, "docs-builder");
	}
}
