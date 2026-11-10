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

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run.Output.Should().Contain("1 passed, 0 failed");
		_ = File.ReadAllText(folder.JUnit).Should().Contain("failures=\"0\"");
	}

	[Test]
	public async Task Run_FailingJourney_ExitsOneAndWritesTheFailureToJUnit()
	{
		using var folder = new TempJourneys("landing/broken", "open /\nexpect title \"no page has this title\" --timeout 500");

		var run = await RunCliAsync(folder, "run", "--url", target.BaseUrl.ToString(), "--journeys", folder.Path, "--junit", folder.JUnit);

		_ = run.ExitCode.Should().Be(1, run.Output);
		_ = run.Output.Should().Contain("FAILED  landing/broken");
		var junit = File.ReadAllText(folder.JUnit);
		_ = junit.Should().Contain("failures=\"1\"").And.Contain("<failure");
	}

	[Test]
	public async Task Run_UnknownArea_ExitsOneWithoutRunningAnything()
	{
		using var folder = new TempJourneys("landing/home", "open /");

		var run = await RunCliAsync(folder, "run", "--url", target.BaseUrl.ToString(), "--journeys", folder.Path, "--area", "missing");

		_ = run.ExitCode.Should().Be(1, run.Output);
		_ = run.Output.Should().Contain("No journeys in area missing");
	}

	[Test]
	public async Task List_Journeys_PrintsAreaAndName()
	{
		using var folder = new TempJourneys("content/tabs", "open /");

		var run = await RunCliAsync(folder, "list", "--journeys", folder.Path);

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run.Output.Trim().Should().Be("content/tabs");
	}

	private static async Task<(int ExitCode, string Output)> RunCliAsync(TempJourneys _, params string[] arguments)
	{
		var info = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
		info.ArgumentList.Add(FindCliDll());
		foreach (var argument in arguments)
			info.ArgumentList.Add(argument);
		using var process = Process.Start(info) ?? throw new InvalidOperationException("docs-journeys did not start.");
		var output = process.StandardOutput.ReadToEndAsync();
		var error = process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync().ConfigureAwait(false);
		return (process.ExitCode, await output.ConfigureAwait(false) + await error.ConfigureAwait(false));
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
