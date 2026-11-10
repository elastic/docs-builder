// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Journeys.Tests.Targets;

namespace Elastic.Documentation.Journeys.Tests;

[ClassDataSource<JourneyTarget>(Shared = SharedType.PerTestSession)]
public class DocsJourneysEmbeddedTests(JourneyTarget target)
{
	[Test]
	public async Task List_WithoutJourneys_ListsTheJourneysCompiledIntoTheBinary()
	{
		var run = await Cli.RunAsync("list");

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run.Output.Should().Contain("landing/home").And.Contain("navigation/sidebar");
	}

	[Test]
	public async Task Run_ChangedOnlyWithoutJourneys_IsRefusedBecauseTheEmbeddedSetHasNoGitHistory()
	{
		var run = await Cli.RunAsync("run", "--url", target.BaseUrl.ToString(), "--changed-only", "origin/main");

		_ = run.ExitCode.Should().Be(1, run.Output);
		_ = run.Output.Should().Contain("--changed-only").And.Contain("--journeys");
	}

	[Test]
	public async Task Run_WithoutJourneys_RunsTheEmbeddedJourneysThatApplyToTheSite()
	{
		if (target.Name != "isolated")
			Skip.Test("The embedded journeys include the isolated-site ones; this target is not the isolated site.");

		// The build type is named because a site built before the docs:build-type tag shipped would be taken for an assembler site.
		var run = await Cli.RunAsync(
			"run",
			"--url",
			target.BaseUrl.ToString(),
			"--env",
			"local",
			"--build-type",
			"isolated",
			"--allow-writes"
		);

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run.Output.Should().Contain("build-type=isolated").And.Contain("0 failed");
		_ = run.Output.Should().MatchRegex(@"[1-9]\d* passed");
	}

	[Test]
	public async Task Run_SiteWithoutTheBuildTypeTag_IsAssumedToBeAnAssemblerSite()
	{
		await using var site = new FakeSite(buildType: null, features: null);
		using var folder = new JourneyFolder().Add("chrome/live", "# @requires build-type:assembler\nopen /docs\nexpect visible body");

		var run = await Cli.RunAsync("run", "--url", site.Url, "--env", "prod", "--journeys", folder.Path);

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run.Output.Should().Contain("build-type=assembler").And.Contain("assuming an assembler site").And.Contain("1 passed");
	}

	[Test]
	public async Task Run_SiteWithTheBuildTypeBodyClass_UsesItInsteadOfAssumingAssembler()
	{
		await using var site = new FakeSite(buildType: null, features: null, bodyClass: "build-type-isolated");
		using var folder = new JourneyFolder().Add("chrome/live", "# @requires build-type:assembler\nopen /docs\nexpect visible body");

		var run = await Cli.RunAsync("run", "--url", site.Url, "--env", "prod", "--journeys", folder.Path);

		_ = run.Output.Should().Contain("build-type=isolated").And.NotContain("assuming an assembler site");
	}

	[Test]
	public async Task Run_SiteThatDoesNotAnswer_LeavesTheBuildTypeUnknown()
	{
		using var folder = new JourneyFolder().Add("chrome/live", "# @requires build-type:assembler\nopen /docs\nexpect visible body");

		var run = await Cli.RunAsync("run", "--url", "http://127.0.0.1:9", "--env", "prod", "--journeys", folder.Path);

		_ = run.Output.Should().Contain("build-type=unknown").And.NotContain("assuming an assembler site");
	}

	[Test]
	public async Task Run_AssemblerSiteGivenAsAnOrigin_OpensRelativePathsUnderDocs()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("navigation/live", "# @requires build-type:assembler\nopen get-started\nopen /absolute");

		var run = await Cli.RunAsync("run", "--url", site.Url, "--env", "demo", "--journeys", folder.Path);

		_ = run.ExitCode.Should().Be(0, run.Output);
		lock (site.Paths)
		{
			_ = site.Paths.Should().Contain("/docs/get-started").And.Contain("/absolute").And.NotContain("/get-started");
		}
	}

	[Test]
	public async Task Run_EveryJourneySkipped_ExitsOneUnlessAllowAllSkippedIsGiven()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("a/isolated", "# @requires build-type:isolated\nopen /");

		var strict = await Cli.RunAsync("run", "--url", site.Url, "--env", "demo", "--journeys", folder.Path);
		var lenient = await Cli.RunAsync("run", "--url", site.Url, "--env", "demo", "--journeys", folder.Path, "--allow-all-skipped");

		_ = strict.ExitCode.Should().Be(1, strict.Output);
		_ = strict.Output.Should().Contain("nothing was checked");
		_ = lenient.ExitCode.Should().Be(0, lenient.Output);
		_ = lenient.Output.Should().Contain("none applies to this site");
	}

	[Test]
	public async Task Run_RootPageThatIsNotADocsSite_IsNotAcceptedAsTheProbeFallback()
	{
		await using var site = new FakeSite(buildType: null, features: null, servedFromRoot: true);
		using var folder = new JourneyFolder().Add("a/live", "# @requires build-type:assembler\nopen /");

		var run = await Cli.RunAsync("run", "--url", site.Url, "--env", "prod", "--journeys", folder.Path);

		_ = run
			.Output
			.Should()
			.Contain("is not a docs site")
			.And
			.Contain("build-type=unknown")
			.And
			.NotContain("assuming an assembler site");
	}

	[Test]
	public async Task Run_ProbeThatFailsWithAServerError_DoesNotFallBackToTheRoot()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "page-feedback");
		using var folder = new JourneyFolder().Add("a/live", "# @requires build-type:assembler\nopen /");

		var run = await Cli.RunAsync("run", "--url", site.Url, "--env", "prod", "--probe", "/error", "--journeys", folder.Path);

		_ = run.Output.Should().Contain("HTTP 500").And.Contain("build-type=unknown").And.NotContain("assuming an assembler site");
	}

	[Test]
	public async Task Export_SiteWithAFlagNameThatIsNotPlain_FailsTheProbeInsteadOfPassingItToNpx()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "page-feedback calc.exe&whoami");
		using var folder = new JourneyFolder().Add("a/live", "# @requires build-type:assembler\n# @monitor\nopen /");

		var run = await Cli.RunAsync(
			"export",
			"synthetics",
			"--url",
			site.Url,
			"--env",
			"demo",
			"--journeys",
			folder.Path,
			"--out",
			Path.Combine(folder.Path, "out"),
			"--dry-run"
		);

		_ = run.ExitCode.Should().NotBe(0, run.Output);
		_ = run.Output.Should().Contain("not a known build type or flag name").And.NotContain("calc.exe");
	}
}
