// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Documentation.Journeys.Tests;

/// <summary>
/// <c>docs-journeys run --build</c> and <c>--wait-for</c> as a process: the run waits for the site to show what a deploy shipped, and
/// ends with exit code 4 and the reason, running no journey, when it does not.
/// </summary>
public class DocsJourneysWaitTests
{
	private const string Passing = "open /page\nexpect title Page";

	[Test]
	public async Task Run_Build_TheLandingPageNamesIt_WaitsThenRunsTheJourneys()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "", build: "1.74.1-canary.0.3");
		using var folder = new JourneyFolder().Add("a/one", Passing);

		var run = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path, "--build", "1.74.1-canary.0.3");

		run.ShouldHaveExited(0);
		_ = run
			.Output
			.Should()
			.Contain("waiting for the content attribute of meta[name='docs:build'] on ./ (10s)")
			.And
			.Contain("is \"1.74.1-canary.0.3\"")
			.And
			.Contain("ok      a/one");
	}

	[Test]
	public async Task Run_Build_TheLandingPageNamesAnotherVersion_EndsWithExitCode4AndRunsNothing()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "", build: "1.73.0");
		using var folder = new JourneyFolder().Add("a/one", Passing);

		var run = await Cli.RunAsync(
			"run",
			"--url",
			site.Url,
			"--journeys",
			folder.Path,
			"--build",
			"1.74.1",
			"--wait-timeout",
			"2s",
			"--wait-interval",
			"200ms",
			"--junit",
			folder.JUnit
		);

		run.ShouldHaveExited(4);
		_ = run.Output.Should().Contain("to equal \"1.74.1\": last saw \"1.73.0\"").And.NotContain("ok      a/one");
		_ = site.PageRequests.Should().BeEmpty("no journey ran");
		var junit = File.ReadAllText(folder.JUnit);
		_ = junit.Should().Contain("classname=\"preconditions\"").And.Contain("name=\"wait\"").And.Contain("<failure");
	}

	[Test]
	public async Task Run_Build_ASiteThatPredatesTheTag_SaysItSawNothing()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("a/one", Passing);

		var run = await Cli.RunAsync(
			"run",
			"--url",
			site.Url,
			"--journeys",
			folder.Path,
			"--build",
			"1.74.1",
			"--wait-timeout",
			"1s",
			"--wait-interval",
			"200ms"
		);

		run.ShouldHaveExited(4);
		_ = run.Output.Should().Contain("last saw nothing");
	}

	[Test]
	public async Task Run_WaitFor_AHeaderAndARegex_AreChecked()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "", build: "1.74.1");
		using var folder = new JourneyFolder().Add("a/one", Passing);

		var run = await Cli.RunAsync(
			"run",
			"--url",
			site.Url,
			"--journeys",
			folder.Path,
			"--wait-for",
			"header:x-docs-build==1.74.1",
			"--wait-for",
			"meta[name='docs:build']@content=~^1\\.74\\."
		);

		run.ShouldHaveExited(0);
		_ = run.Output.Should().Contain("The x-docs-build response header of ./ is \"1.74.1\"").And.Contain("ok      a/one");
	}

	[Test]
	public async Task Run_Build_ADeployThatChangesTheFlags_IsJudgedAgainstTheFlagsOfTheNewBuild()
	{
		// The first read of the landing page, before the wait, shows the old build with no flags. The deploy lands after it.
		await using var site = new FakeSite(
			buildType: "assembler",
			features: "",
			build: "1.73.0",
			redeploy: ("1.74.1", "navigation-preview")
		);
		using var folder = new JourneyFolder().Add("a/new-layout", $"# @requires navigation-preview\n{Passing}");

		var run = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path, "--build", "1.74.1");

		run.ShouldHaveExited(0);
		_ = run
			.Output
			.Should()
			.Contain("context: env=local, build-type=assembler, features=[]")
			.And
			.Contain("features=[navigation-preview]")
			.And
			.Contain("ok      a/new-layout")
			.And
			.NotContain("skipped a/new-layout");
	}

	[Test]
	[Arguments("nonsense", "--wait-for 'nonsense' is not a check")]
	[Arguments("header:==1", "--wait-for 'header:==1' names no header")]
	public async Task Run_WaitFor_ACheckThatIsNotOne_IsAUsageError(string check, string message)
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("a/one", Passing);

		var run = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path, "--wait-for", check);

		run.ShouldHaveFailed();
		_ = run.ExitCode.Should().NotBe(4);
		_ = run.Output.Should().Contain(message);
		_ = site.PageRequests.Should().BeEmpty();
	}

	[Test]
	public async Task Run_WaitOptionsWithoutACheck_AreAUsageError()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("a/one", Passing);

		var run = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path, "--wait-timeout", "5s");

		run.ShouldHaveFailed();
		_ = run.Output.Should().Contain("--wait-path, --wait-timeout, --wait-interval and --wait-retries describe a --wait-for; give one");
	}
}
