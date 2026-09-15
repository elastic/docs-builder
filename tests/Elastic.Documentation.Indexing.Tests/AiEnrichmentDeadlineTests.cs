// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Documentation.Indexing.Tests;

public class AiEnrichmentDeadlineTests
{
	[Fact]
	public void Create_NoWallClock_ReturnsOriginalToken()
	{
		using var cts = new CancellationTokenSource();

		using var deadline = AiEnrichmentDeadline.Create(null, cts.Token);

		deadline.Token.Should().Be(cts.Token);
		deadline.TimedOut.Should().BeFalse();
	}

	[Fact]
	public void Create_AmbientTokenCancelled_LinkedTokenCancelledButNotTimedOut()
	{
		using var cts = new CancellationTokenSource();

		using var deadline = AiEnrichmentDeadline.Create(TimeSpan.FromMinutes(5), cts.Token);
		cts.Cancel();

		deadline.Token.IsCancellationRequested.Should().BeTrue();
		deadline.TimedOut.Should().BeFalse();
	}

	[Fact]
	public async Task Create_WallClockElapses_LinkedTokenCancelledAndTimedOut()
	{
		using var deadline = AiEnrichmentDeadline.Create(TimeSpan.FromMilliseconds(1), CancellationToken.None);

		// Give the internal CancellationTokenSource timer a moment to fire.
		await Task.Delay(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken);

		deadline.Token.IsCancellationRequested.Should().BeTrue();
		deadline.TimedOut.Should().BeTrue();
	}
}
