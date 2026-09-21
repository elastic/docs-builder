// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Net;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

public class ReleaseNotesFetcherTests
{
	[Fact]
	public async Task FetchAsync_RequiredProductWithoutPublishedBundles_EmitsHint()
	{
		await using var collector = new DiagnosticsCollector([]);
		using var handler = new NotFoundHandler();
		var fetcher = new ReleaseNotesFetcher(NullLoggerFactory.Instance, new MockFileSystem(), handler);

		var result = await fetcher.FetchAsync(collector, ["docs-builder"], ctx: TestContext.Current.CancellationToken);

		collector.Errors.Should().Be(0);
		collector.Warnings.Should().Be(0);
		collector.Hints.Should().Be(1);
		result.NotFoundDeclaredProducts.Should().ContainSingle().Which.Should().Be("docs-builder");
	}

	private sealed class NotFoundHandler : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
	}
}
