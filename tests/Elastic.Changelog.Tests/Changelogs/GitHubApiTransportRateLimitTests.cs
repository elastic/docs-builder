// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Net;
using AwesomeAssertions;
using Elastic.Changelog.GitHub;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Bulk reads run several GitHub requests at once, so a rate limit can hit mid-run. The transport waits it out for
/// reads, up to a cap, instead of returning a failure that would drop an entry from the bundle.
/// </summary>
public class GitHubApiTransportRateLimitTests
{
	private static HttpResponseMessage Limited(
		HttpStatusCode status,
		string? retryAfterSeconds = null,
		string? remaining = null,
		long? reset = null,
		string? body = null
	)
	{
		var response = new HttpResponseMessage(status) { Content = new StringContent(body ?? string.Empty) };
		if (retryAfterSeconds != null)
			response.Headers.TryAddWithoutValidation("Retry-After", retryAfterSeconds);
		if (remaining != null)
			response.Headers.TryAddWithoutValidation("x-ratelimit-remaining", remaining);
		if (reset != null)
			response.Headers.TryAddWithoutValidation("x-ratelimit-reset", reset.Value.ToString(CultureInfo.InvariantCulture));
		return response;
	}

	private static async Task<(HttpStatusCode Status, int Requests, List<TimeSpan> Waits)> GetAsync(
		params Func<HttpResponseMessage>[] responses
	)
	{
		var requests = 0;
		var waits = new List<TimeSpan>();
		using var transport = new GitHubApiTransport(new StubHandler(
			_ => responses[Math.Min(requests++, responses.Length - 1)]()
		), "token", (wait, _) =>
		{
			waits.Add(wait);
			return Task.CompletedTask;
		});

		using var response = await transport.GetAsync("https://api.github.com/x", TestContext.Current!.Execution.CancellationToken);
		return (response.StatusCode, requests, waits);
	}

	[Test]
	public async Task Get_SecondaryLimitWithRetryAfter_WaitsThenSucceeds()
	{
		var (status, requests, waits) = await GetAsync(
			() => Limited(HttpStatusCode.Forbidden, retryAfterSeconds: "7"),
			() => new HttpResponseMessage(HttpStatusCode.OK)
		);

		status.Should().Be(HttpStatusCode.OK);
		requests.Should().Be(2);
		waits.Should().Equal(TimeSpan.FromSeconds(7));
	}

	[Test]
	public async Task Get_TooManyRequests_Retries()
	{
		var (status, requests, _) = await GetAsync(
			() => Limited(HttpStatusCode.TooManyRequests),
			() => new HttpResponseMessage(HttpStatusCode.OK)
		);

		status.Should().Be(HttpStatusCode.OK);
		requests.Should().Be(2);
	}

	[Test]
	public async Task Get_RateLimitNeverLifts_GivesUpAfterFourAttempts()
	{
		var (status, requests, waits) = await GetAsync(() => Limited(HttpStatusCode.TooManyRequests, retryAfterSeconds: "1"));

		status.Should().Be(HttpStatusCode.TooManyRequests);
		requests.Should().Be(4);
		waits.Should().HaveCount(3);
	}

	[Test]
	public async Task Get_PrimaryLimitThatResetsLater_IsNotWaitedOut()
	{
		var resetInAnHour = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

		var (status, requests, waits) = await GetAsync(() => Limited(HttpStatusCode.Forbidden, remaining: "0", reset: resetInAnHour));

		status.Should().Be(HttpStatusCode.Forbidden);
		requests.Should().Be(1);
		waits.Should().BeEmpty();
	}

	[Test]
	public async Task Get_ForbiddenWithoutRateLimitSignal_IsNotRetried()
	{
		var (status, requests, _) = await GetAsync(() => Limited(HttpStatusCode.Forbidden, remaining: "4999"));

		status.Should().Be(HttpStatusCode.Forbidden);
		requests.Should().Be(1);
	}

	[Test]
	public async Task Get_SecondaryLimit403WithoutRetryAfter_IsRetriedAfterAMinute()
	{
		var (status, requests, waits) = await GetAsync(
			() => Limited(
				HttpStatusCode.Forbidden,
				remaining: "4000",
				body: """{ "message": "You have exceeded a secondary rate limit." }"""
			),
			() => new HttpResponseMessage(HttpStatusCode.OK)
		);

		status.Should().Be(HttpStatusCode.OK);
		requests.Should().Be(2);
		waits.Should().Equal(TimeSpan.FromSeconds(60));
	}

	[Test]
	public async Task Get_Forbidden_ForAMissingPermission_IsNotRetried()
	{
		var (status, requests, _) = await GetAsync(
			() => Limited(HttpStatusCode.Forbidden, remaining: "4000", body: """{ "message": "Resource not accessible by integration" }""")
		);

		status.Should().Be(HttpStatusCode.Forbidden);
		requests.Should().Be(1);
	}

	[Test]
	public async Task Get_SecondaryLimit429WithBudgetLeftAndFarReset_BacksOffInsteadOfUsingTheReset()
	{
		var resetInAnHour = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

		var (status, requests, waits) = await GetAsync(
			() => Limited(HttpStatusCode.TooManyRequests, remaining: "4000", reset: resetInAnHour),
			() => new HttpResponseMessage(HttpStatusCode.OK)
		);

		status.Should().Be(HttpStatusCode.OK);
		requests.Should().Be(2);
		waits.Should().Equal(TimeSpan.FromSeconds(60));
	}

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) => responder(request);

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
