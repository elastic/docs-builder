// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.ReleaseNotes;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// A pool holds hundreds of entries, so the fetcher downloads them in parallel. The result order and the
/// reported failure must not depend on which download finishes first.
/// </summary>
public class CdnChangelogEntryFetcherParallelTests
{
	private static readonly Uri BaseUri = new("https://cdn.example.test");

	private static string Registry(IEnumerable<string> files) =>
		$$"""{ "schema_version": 1, "product": "widget", "bundles": [{{string.Join(",", files.Select(f => $$"""{ "file": "{{f}}" }"""))}}] }""";

	private static CdnChangelogEntryFetcher Fetcher(HttpMessageHandler handler) =>
		new(new TestLoggerFactory(), handler, maxAttempts: 1, sleep: (_, _) => Task.CompletedTask);

	[Test]
	public async Task Fetch_ManyEntries_RunsInParallelAndKeepsRegistryOrder()
	{
		var files = Enumerable.Range(1, 60).Select(i => $"{i}.yaml").ToList();
		var handler = new ConcurrencyHandler(
			path => path.EndsWith("registry.json", StringComparison.Ordinal) ? Registry(files) : $"title: {Path.GetFileName(path)}"
		);
		var errors = new List<string>();

		var entries = await Fetcher(handler).FetchAsync(
			BaseUri,
			"elastic",
			"widget",
			"main",
			errors.Add,
			_ => { },
			TestContext.Current!.Execution.CancellationToken
		);

		errors.Should().BeEmpty();
		entries.Select(e => e.FileName).Should().Equal(files);
		handler.MaxConcurrent.Should().BeGreaterThan(1).And.BeLessThanOrEqualTo(32);
	}

	[Test]
	public async Task Fetch_SeveralEntriesMissing_ReportsTheFirstInRegistryOrder()
	{
		var files = Enumerable.Range(1, 30).Select(i => $"{i}.yaml").ToList();
		var handler = new ConcurrencyHandler(
			path => path.EndsWith("registry.json", StringComparison.Ordinal) ? Registry(files) : $"title: {Path.GetFileName(path)}",
			missing: ["7.yaml", "3.yaml", "22.yaml"]
		);
		var errors = new List<string>();

		var entries = await Fetcher(handler).FetchAsync(
			BaseUri,
			"elastic",
			"widget",
			"main",
			errors.Add,
			_ => { },
			TestContext.Current!.Execution.CancellationToken
		);

		entries.Should().BeEmpty();
		errors.Should().ContainSingle().Which.Should().Contain("'3.yaml'");
	}

	private sealed class ConcurrencyHandler(Func<string, string> body, string[]? missing = null) : HttpMessageHandler
	{
		private int _current;
		private int _max;

		public int MaxConcurrent => _max;

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var path = request.RequestUri!.AbsolutePath;
			var now = Interlocked.Increment(ref _current);
			int seen;
			while (now > (seen = Volatile.Read(ref _max)))
			{
				if (Interlocked.CompareExchange(ref _max, now, seen) == seen)
					break;
			}

			try
			{
				await Task.Delay(20, cancellationToken);
				if (missing?.Any(m => path.EndsWith(m, StringComparison.Ordinal)) == true)
					return new HttpResponseMessage(HttpStatusCode.NotFound);

				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(body(path), Encoding.UTF8, "application/json")
				};
			}
			finally
			{
				_ = Interlocked.Decrement(ref _current);
			}
		}
	}
}
