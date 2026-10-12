// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Site.FileProviders;
using FakeItEasy;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// Vite names the entry bundle <c>main-[hash].js</c> and its chunks import that exact name. The page has to
/// reference the same URL, or the browser evaluates the module twice and every delegated handler runs twice.
/// </summary>
public class StaticFileContentHashProviderTests
{
	[Test]
	public void GetUrl_HashedSibling_ResolvesToExactNameWithoutQuery()
	{
		using var docs = new TempDocs();
		docs.WriteStatic("widget-abc12345.js", "console.log(1)");

		var url = docs.Provider.GetUrl("widget.js", "/prefix");

		url.Should().Be("/prefix/_static/widget-abc12345.js");
	}

	[Test]
	public void GetUrl_PlainFile_AppendsContentHash()
	{
		using var docs = new TempDocs();
		docs.WriteStatic("plain.css", "body{}");

		var url = docs.Provider.GetUrl("plain.css", string.Empty);

		url.Should().MatchRegex(@"^/_static/plain\.css\?v=[0-9a-f]{16}$");
	}

	[Test]
	public void GetUrl_MissingFile_ReturnsPathWithoutQuery()
	{
		using var docs = new TempDocs();

		var url = docs.Provider.GetUrl("missing.js", string.Empty);

		url.Should().Be("/_static/missing.js");
	}

	[Test]
	public void GetUrl_MainEntry_ResolvesThroughEmbeddedBundleNextToDocSetStatic()
	{
		using var docs = new TempDocs();
		docs.WriteStatic("logo.png", "png");

		var url = docs.Provider.GetUrl("main.js", string.Empty);

		url.Should().MatchRegex(@"^/_static/main-[A-Za-z0-9_-]+\.js$");
	}

	private sealed class TempDocs : IDisposable
	{
		private readonly string _root = Path.Join(Path.GetTempPath(), $"static-url-{Guid.NewGuid():N}");

		public TempDocs()
		{
			_ = Directory.CreateDirectory(Path.Join(_root, "_static"));
			var fs = new FileSystem();
			var readFs = A.Fake<IDocumentationFileSystem>();
			A.CallTo(() => readFs.Directory.Exists(A<string>._)).Returns(true);
			var context = A.Fake<IDocumentationSetContext>();
			A.CallTo(() => context.DocumentationSourceDirectory).Returns(fs.DirectoryInfo.New(_root));
			A.CallTo(() => context.ReadFileSystem).Returns(readFs);
			Provider = new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context));
		}

		public StaticFileContentHashProvider Provider { get; }

		public void WriteStatic(string name, string content) => File.WriteAllText(Path.Join(_root, "_static", name), content);

		public void Dispose() => Directory.Delete(_root, recursive: true);
	}
}
