// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Documentation.Journeys.Tests;

/// <summary>
/// <c>docs-journeys run --retries</c> as a process, against a page whose title is wrong the first time it is read: a journey that
/// recovers passes the run and is reported as flaky, one that keeps failing fails it, and without the option nothing is retried.
/// </summary>
public class DocsJourneysRetryTests
{
	// The title is read once the page has loaded, so a wrong title fails the step at once and not after the default five seconds.
	private const string Titled = "open /flaky\nexpect --timeout 200 title Page";

	[Test]
	public async Task Run_Retries_AJourneyThatFailsOnceThenPasses_PassesTheRunAndIsReportedAsFlaky()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("a/flaky", Titled);

		var run = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path, "--retries", "1", "--junit", folder.JUnit);

		run.ShouldHaveExited(0);
		_ = run
			.Output
			.Should()
			.Contain("retry   a/flaky (attempt 1 of 2 failed")
			.And
			.Contain("flaky   a/flaky")
			.And
			.Contain("1 passed (1 flaky), 0 failed");
		_ = File.ReadAllText(folder.JUnit).Should().Contain("<flakyFailure").And.NotContain("<failure");
	}

	[Test]
	public async Task Run_Retries_AJourneyThatKeepsFailing_FailsTheRunAfterTheLastRetry()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("a/broken", "open /broken\nexpect --timeout 200 title Page");

		var run = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path, "--retries", "1", "--junit", folder.JUnit);

		run.ShouldHaveExited(1);
		_ = run
			.Output
			.Should()
			.Contain("retry   a/broken (attempt 1 of 2 failed")
			.And
			.Contain("FAILED  a/broken")
			.And
			.Contain("0 passed, 1 failed");
		_ = File.ReadAllText(folder.JUnit).Should().Contain("<failure").And.Contain("<rerunFailure");
	}

	[Test]
	public async Task Run_WithoutRetries_AJourneyThatFailsOnce_FailsTheRunAndRetriesNothing()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("a/flaky", Titled);

		var run = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path);

		run.ShouldHaveExited(1);
		_ = run.Output.Should().Contain("FAILED  a/flaky").And.NotContain("retry").And.NotContain("flaky   ");
	}
}
