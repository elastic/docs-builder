// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using AwesomeAssertions;
using Elastic.Documentation.Journeys.Tests.Targets;

namespace Elastic.Documentation.Journeys.Tests;

[ClassDataSource<JourneyTarget>(Shared = SharedType.PerTestSession)]
public class DocsJourneysCliTests(JourneyTarget target)
{
	[Test]
	public async Task Run_PassingJourney_ExitsZeroAndReportsIt()
	{
		using var folder = new TempJourneys("landing/home", "open /\nexpect visible body");

		var run = await RunCliAsync(folder, "run", "--url", target.BaseUrl.ToString(), "--journeys", folder.Path, "--junit", folder.JUnit);

		run.ShouldHaveExited(0);
		_ = run.Output.Should().Contain("1 passed, 0 failed");
		_ = File.ReadAllText(folder.JUnit).Should().Contain("failures=\"0\"");
	}

	[Test]
	public async Task Run_FailingJourney_ExitsOneAndWritesTheFailureToJUnit()
	{
		using var folder = new TempJourneys("landing/broken", "open /\nexpect title \"no page has this title\" --timeout 500");

		var run = await RunCliAsync(folder, "run", "--url", target.BaseUrl.ToString(), "--journeys", folder.Path, "--junit", folder.JUnit);

		run.ShouldHaveExited(1);
		_ = run.Output.Should().Contain("FAILED  landing/broken");
		var junit = File.ReadAllText(folder.JUnit);
		_ = junit.Should().Contain("failures=\"1\"").And.Contain("<failure");
	}

	[Test]
	public async Task Run_FailingJourney_ShowsTheStepTheErrorAndHowToRerunItAfterTheResults()
	{
		using var folder = new TempJourneys("landing/broken", "open /\nexpect title \"no page has this title\" --timeout 500");

		var run = await RunCliAsync(folder, "run", "--url", target.BaseUrl.ToString(), "--journeys", folder.Path);

		run.ShouldHaveExited(1);
		var lines = run.Output.ReplaceLineEndings("\n").Split('\n');
		var stopped = lines.Single(l => l.StartsWith("Journey stopped at ", StringComparison.Ordinal));
		_ = stopped.Should().EndWith("broken.journey:2: expect title \"no page has this title\" --timeout 500");
		_ = run.Output.Should().Contain("Rerun:").And.Contain($"--journeys {folder.Path}").And.Contain("--only landing/broken");
		_ = Array.IndexOf(lines, "FAILED  landing/broken").Should().BeLessThan(Array.IndexOf(lines, stopped));
	}

	[Test]
	public async Task Run_FastJourneyFinishes_ItsResultIsWrittenWhileASlowOneIsStillRunning()
	{
		using var folder = new TempJourneys("landing/fast", "open /\nexpect visible body");
		File.WriteAllText(
			System.IO.Path.Combine(folder.Path, "landing", "slow.journey"),
			"open /\nexpect title \"no page has this title\" --timeout 15000"
		);
		var info = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
		foreach (var argument in new[] { FindCliDll(), "run", "--url", target.BaseUrl.ToString(), "--journeys", folder.Path })
			info.ArgumentList.Add(argument);
		using var process = Process.Start(info) ?? throw new InvalidOperationException("docs-journeys did not start.");
		var error = process.StandardError.ReadToEndAsync();

		var sawFast = false;
		var stillRunning = false;
		while (await process.StandardOutput.ReadLineAsync().ConfigureAwait(false) is { } line)
		{
			if (!line.StartsWith("ok      landing/fast", StringComparison.Ordinal))
				continue;
			sawFast = true;
			stillRunning = !process.HasExited;
			break;
		}
		if (!process.HasExited)
			process.Kill(entireProcessTree: true);
		await process.WaitForExitAsync().ConfigureAwait(false);
		_ = await error.ConfigureAwait(false);

		_ = sawFast.Should().BeTrue();
		_ = stillRunning.Should().BeTrue("the fast journey's result must not wait for the slow one");
	}

	[Test]
	public async Task Run_Only_RunsJustThatJourney()
	{
		using var folder = new TempJourneys("landing/home", "open /\nexpect visible body");
		File.WriteAllText(System.IO.Path.Combine(folder.Path, "landing", "other.journey"), "open /\nexpect visible body");

		var run = await RunCliAsync(
			folder,
			"run",
			"--url",
			target.BaseUrl.ToString(),
			"--journeys",
			folder.Path,
			"--only",
			"landing/other"
		);

		run.ShouldHaveExited(0);
		_ = run.Output.Should().Contain("ok      landing/other").And.NotContain("landing/home").And.Contain("1 passed");
	}

	[Test]
	public async Task Run_BadInput_IsOneErrorLineAndNotAStackTrace()
	{
		using var folder = new TempJourneys("landing/home", "open /");

		var unknownId = await RunCliAsync(folder, "run", "--url", target.BaseUrl.ToString(), "--journeys", folder.Path, "--only", "nope/x");
		var badUrl = await RunCliAsync(folder, "run", "--url", "nonsense", "--journeys", folder.Path);

		unknownId.ShouldHaveExited(1);
		badUrl.ShouldHaveExited(1);
		_ = unknownId.Output.Trim().Should().StartWith("Error: No journey with id nope/x").And.NotContain("   at ");
		_ = badUrl.Output.Trim().Should().StartWith("Error: --url must be an http or https origin or one of prod, edge, demo, local");
	}

	[Test]
	public async Task Run_ManyJourneysSkippedForOneReason_CollapsesThemUnlessVerbose()
	{
		using var folder = new TempJourneys("a/j0", "# @requires build-type:isolated\nopen /");
		for (var i = 1; i < 5; i++)
			File.WriteAllText(System.IO.Path.Combine(folder.Path, "a", $"j{i}.journey"), "# @requires build-type:isolated\nopen /");
		File.WriteAllText(System.IO.Path.Combine(folder.Path, "a", "runs.journey"), "open /\nexpect visible body");
		string[] common = ["run", "--url", target.BaseUrl.ToString(), "--journeys", folder.Path, "--build-type", "assembler"];

		var collapsed = await RunCliAsync(folder, common);
		var verbose = await RunCliAsync(folder, [.. common, "--verbose"]);

		_ = collapsed.Output.Should().Contain("skipped 5 journeys (requires build-type:isolated)").And.NotContain("skipped a/j0");
		_ = verbose
			.Output
			.Should()
			.Contain("skipped a/j0 (requires build-type:isolated)")
			.And
			.Contain("skipped a/j4 (requires build-type:isolated)");
	}

	[Test]
	public async Task Run_UnknownArea_ExitsOneWithoutRunningAnything()
	{
		using var folder = new TempJourneys("landing/home", "open /");

		var run = await RunCliAsync(folder, "run", "--url", target.BaseUrl.ToString(), "--journeys", folder.Path, "--area", "missing");

		run.ShouldHaveExited(1);
		_ = run.Output.Should().Contain("No journeys in area missing");
	}

	[Test]
	public async Task Run_NoJourneyFiles_ExitsOneInsteadOfPassingVacuously()
	{
		using var folder = new TempJourneys("landing/home", "open /");
		var empty = System.IO.Path.Combine(folder.Path, "empty");
		_ = Directory.CreateDirectory(empty);

		var run = await RunCliAsync(folder, "run", "--url", target.BaseUrl.ToString(), "--journeys", empty);

		run.ShouldHaveExited(1);
		_ = run.Output.Should().Contain("No .journey files found");
	}

	[Test]
	public async Task Run_UnparsableJourney_FailsThatJourneyAndStillReportsTheOthers()
	{
		using var folder = new TempJourneys("landing/home", "open /\nexpect visible body");
		File.WriteAllText(System.IO.Path.Combine(folder.Path, "landing", "broken.journey"), "this is not a command");

		var run = await RunCliAsync(folder, "run", "--url", target.BaseUrl.ToString(), "--journeys", folder.Path, "--junit", folder.JUnit);

		run.ShouldHaveExited(1);
		_ = run.Output.Should().Contain("ok      landing/home").And.Contain("FAILED  landing/broken").And.Contain("1 passed, 1 failed");
		_ = File.ReadAllText(folder.JUnit).Should().Contain("tests=\"2\"").And.Contain("failures=\"1\"");
	}

	[Test]
	public async Task List_Journeys_PrintsAreaAndName()
	{
		using var folder = new TempJourneys("content/tabs", "open /");

		var run = await RunCliAsync(folder, "list", "--journeys", folder.Path);

		run.ShouldHaveExited(0);
		_ = run.Output.Trim().Should().Be("content/tabs");
	}

	private static async Task<CliRun> RunCliAsync(TempJourneys _, params string[] arguments)
	{
		var info = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
		info.ArgumentList.Add(FindCliDll());
		foreach (var argument in arguments)
			info.ArgumentList.Add(argument);
		using var process = Process.Start(info) ?? throw new InvalidOperationException("docs-journeys did not start.");
		var output = process.StandardOutput.ReadToEndAsync();
		var error = process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync().ConfigureAwait(false);
		return new CliRun(process.ExitCode, await output.ConfigureAwait(false) + await error.ConfigureAwait(false));
	}

	private static string FindCliDll()
	{
		var configuration = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(System.IO.Path.DirectorySeparatorChar))
			.Name
			.ToLowerInvariant();
		for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
		{
			var dll = System.IO.Path.Combine(directory.FullName, ".artifacts", "bin", "docs-journeys", configuration, "docs-journeys.dll");
			if (File.Exists(dll))
				return dll;
		}
		throw new FileNotFoundException("docs-journeys.dll was not built; the test project references it.");
	}

	private sealed class TempJourneys : IDisposable
	{
		public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "docs-journeys-" + Guid.NewGuid().ToString("N"));
		public string JUnit => System.IO.Path.Combine(Path, "junit.xml");

		public TempJourneys(string id, string journey)
		{
			var file = System.IO.Path.Combine(Path, id + ".journey");
			_ = Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file)!);
			File.WriteAllText(file, journey);
		}

		public void Dispose() => Directory.Delete(Path, recursive: true);
	}
}
