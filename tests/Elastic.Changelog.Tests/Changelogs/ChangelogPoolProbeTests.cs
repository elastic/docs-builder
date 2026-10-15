// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using AwesomeAssertions;
using Elastic.Changelog.Bundling;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// <see cref="ChangelogPoolProbe"/> separates a repository that is not onboarded (404) from one whose state
/// is unknown, so the bundler can skip the first and still report the second.
/// </summary>
public class ChangelogPoolProbeTests
{
	private static readonly Uri BaseUri = new("https://cdn.example.test");

	private static async Task<(bool? Result, string? Path)> ProbeAsync(Func<HttpResponseMessage> respond, string repo = "kibana")
	{
		string? path = null;
		using var http = new HttpClient(new StubHandler(req =>
		{
			path = req.RequestUri!.AbsolutePath;
			return respond();
		}));
		var result = await ChangelogPoolProbe.ExistsAsync(
			http,
			BaseUri,
			"elastic",
			repo,
			"main",
			TestContext.Current!.Execution.CancellationToken
		);
		return (result, path);
	}

	[Test]
	public async Task Exists_RegistryAnswers200_ReturnsTrueAndAsksForTheRegistry()
	{
		var (result, path) = await ProbeAsync(() => new HttpResponseMessage(HttpStatusCode.OK));

		result.Should().BeTrue();
		path.Should().Be("/changelog/elastic/kibana/main/registry.json");
	}

	[Test]
	public async Task Exists_RegistryAnswers404_ReturnsFalse()
	{
		var (result, _) = await ProbeAsync(() => new HttpResponseMessage(HttpStatusCode.NotFound));

		result.Should().BeFalse();
	}

	[Test]
	[Arguments(HttpStatusCode.Forbidden)]
	[Arguments(HttpStatusCode.InternalServerError)]
	public async Task Exists_OtherStatus_IsUnknown(HttpStatusCode status)
	{
		var (result, _) = await ProbeAsync(() => new HttpResponseMessage(status));

		result.Should().BeNull();
	}

	[Test]
	public async Task Exists_NetworkError_IsUnknown()
	{
		var (result, _) = await ProbeAsync(() => throw new HttpRequestException("boom"));

		result.Should().BeNull();
	}

	[Test]
	public async Task Exists_InvalidRepositoryName_IsUnknownWithoutARequest()
	{
		var (result, path) = await ProbeAsync(() => new HttpResponseMessage(HttpStatusCode.NotFound), repo: "../etc");

		result.Should().BeNull();
		path.Should().BeNull();
	}

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) => responder(request);

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
