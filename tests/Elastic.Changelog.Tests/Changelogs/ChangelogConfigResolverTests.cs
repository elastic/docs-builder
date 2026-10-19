// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// The unified release command bundles with the <c>changelog.yml</c> of each product. A missing file and a
/// failed request must not look alike, and a prototype must not hide a real repository file unnoticed.
/// </summary>
public class ChangelogConfigResolverTests() : ChangelogTestBase()
{
	private const string Commit = "0123456789abcdef";
	private const string DocsUrl = $"https://raw.githubusercontent.com/elastic/kibana/{Commit}/docs/changelog.yml";
	private const string RootUrl = $"https://raw.githubusercontent.com/elastic/kibana/{Commit}/changelog.yml";

	private sealed class StubHandler(Func<string, HttpResponseMessage> respond) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(respond(request.RequestUri!.ToString()));
	}

	private static HttpResponseMessage Ok(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body) };

	private static HttpResponseMessage Status(HttpStatusCode code) => new(code);

	private ChangelogConfigResolver Resolver(Func<string, HttpResponseMessage> respond) =>
		new(new HttpClient(new StubHandler(respond)), FileSystem, LoggerFactory.CreateLogger("resolver"), Collector);

	private string WritePrototype(string content)
	{
		var directory = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, "prototypes");
		_ = FileSystem.Directory.CreateDirectory(directory);
		FileSystem.File.WriteAllText(FileSystem.Path.Join(directory, "kibana.changelog.yml"), content);
		return directory;
	}

	[Test]
	public async Task ResolveAsync_DocsChangelogExists_ReturnsItsContent()
	{
		var resolver = Resolver(url => url == DocsUrl ? Ok("docs: true") : Status(HttpStatusCode.NotFound));

		var result = await resolver.ResolveAsync("kibana", "kibana", Commit, null, TestContext.Current!.Execution.CancellationToken);

		result.Should().Be(new ChangelogConfigResult("docs: true", false));
	}

	[Test]
	public async Task ResolveAsync_OnlyRootChangelogExists_FallsBackToTheRootFile()
	{
		var resolver = Resolver(url => url == RootUrl ? Ok("root: true") : Status(HttpStatusCode.NotFound));

		var result = await resolver.ResolveAsync("kibana", "kibana", Commit, null, TestContext.Current!.Execution.CancellationToken);

		result.Should().Be(new ChangelogConfigResult("root: true", false));
	}

	[Test]
	public async Task ResolveAsync_BothFilesMissing_ReturnsNoContentWithoutFailing()
	{
		var resolver = Resolver(_ => Status(HttpStatusCode.NotFound));

		var result = await resolver.ResolveAsync("kibana", "kibana", Commit, null, TestContext.Current!.Execution.CancellationToken);

		result.Should().Be(new ChangelogConfigResult(null, false));
		Collector.Errors.Should().Be(0);
	}

	[Test]
	public async Task ResolveAsync_ServerError_FailsInsteadOfReportingAMissingFile()
	{
		var resolver = Resolver(_ => Status(HttpStatusCode.InternalServerError));

		var result = await resolver.ResolveAsync("kibana", "kibana", Commit, null, TestContext.Current!.Execution.CancellationToken);

		result.Should().Be(new ChangelogConfigResult(null, true));
		Collector.Errors.Should().Be(1);
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("returned 500"));
	}

	[Test]
	public async Task ResolveAsync_NetworkError_FailsInsteadOfReportingAMissingFile()
	{
		var resolver = Resolver(_ => throw new HttpRequestException("connection reset"));

		var result = await resolver.ResolveAsync("kibana", "kibana", Commit, null, TestContext.Current!.Execution.CancellationToken);

		result.Should().Be(new ChangelogConfigResult(null, true));
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("connection reset"));
	}

	[Test]
	public async Task ResolveAsync_PrototypeAndRepositoryFile_UsesThePrototypeAndWarnsAboutTheShadow()
	{
		var directory = WritePrototype("prototype: true");
		var resolver = Resolver(url => url == DocsUrl ? Ok("docs: true") : Status(HttpStatusCode.NotFound));

		var result = await resolver.ResolveAsync("kibana", "kibana", Commit, directory, TestContext.Current!.Execution.CancellationToken);

		result.Should().Be(new ChangelogConfigResult("prototype: true", false));
		Collector.Warnings.Should().Be(1);
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Warning && d.Message.Contains("shadows the changelog.yml"));
	}

	[Test]
	public async Task ResolveAsync_PrototypeWithoutRepositoryFile_UsesThePrototypeSilently()
	{
		var directory = WritePrototype("prototype: true");
		var resolver = Resolver(_ => Status(HttpStatusCode.NotFound));

		var result = await resolver.ResolveAsync("kibana", "kibana", Commit, directory, TestContext.Current!.Execution.CancellationToken);

		result.Should().Be(new ChangelogConfigResult("prototype: true", false));
		Collector.Warnings.Should().Be(0);
	}

	[Test]
	public async Task ResolveAsync_PrototypeAndFailedRepositoryProbe_StillUsesThePrototype()
	{
		var directory = WritePrototype("prototype: true");
		var resolver = Resolver(_ => Status(HttpStatusCode.BadGateway));

		var result = await resolver.ResolveAsync("kibana", "kibana", Commit, directory, TestContext.Current!.Execution.CancellationToken);

		result.Should().Be(new ChangelogConfigResult("prototype: true", false));
		Collector.Errors.Should().Be(0);
	}
}
