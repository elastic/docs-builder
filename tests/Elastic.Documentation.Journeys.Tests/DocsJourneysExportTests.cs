// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using AwesomeAssertions;

namespace Elastic.Documentation.Journeys.Tests;

/// <summary>
/// <c>docs-journeys export synthetics</c> and <c>push</c> as a process. The commands are checked through <c>--dry-run</c>,
/// which prints the exact <c>npx</c> command and the project config without Node or a network. One test runs the real
/// export, which needs <c>npx</c> and the npm registry, and skips without them. Nothing here reaches a Kibana.
/// </summary>
public class DocsJourneysExportTests
{
	private const string Monitor = "# @tags smoke\n# @monitor schedule=10 locations=germany\nopen /page\nexpect title Page";
	private static readonly IReadOnlyDictionary<string, string?> NoKey = new Dictionary<string, string?> { ["SYNTHETICS_API_KEY"] = null };

	[Test]
	public async Task Export_DryRun_PrintsTheContextTheProjectConfigAndThePinnedCommandAndWritesNothing()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "page-feedback navigation-preview");
		using var folder = new JourneyFolder().Add("landing/home", Monitor);
		var outDir = Path.Combine(folder.Path, "out");

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
			outDir,
			"--dry-run"
		);

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run.Output.Should().Contain("context: env=demo (--env), build-type=assembler, features=[navigation-preview page-feedback]");
		_ = run.Output.Should().Contain("project: elastic-co-docs-demo");
		_ = run.Output.Should().Contain("id: 'elastic-co-docs-demo',").And.Contain("space: 'default',").And.Contain("schedule: 15,");
		_ = run.Output.Should().Contain("url: 'https://elastic-docs-v3-prod-e6053a.kb.us-east-1.aws.elastic.cloud/',");
		_ = run
			.Output
			.Should()
			.Contain($"{NpxName} --yes @nullean/journeys@0.9.0 export-synthetics")
			.And
			.Contain("--features navigation-preview,page-feedback")
			.And
			.Contain("--build-type assembler")
			.And
			.Contain("--env demo");
		_ = run.Output.Should().NotContain("--paging");
		_ = Directory.Exists(outDir).Should().BeFalse("a dry run writes nothing");
	}

	[Test]
	public async Task Export_WhenTheSiteContextCannotBeRead_FailsClosed()
	{
		using var folder = new JourneyFolder().Add("landing/home", Monitor);

		var run = await Cli.RunAsync(
			"export",
			"synthetics",
			"--url",
			"http://127.0.0.1:1",
			"--env",
			"demo",
			"--journeys",
			folder.Path,
			"--out",
			Path.Combine(folder.Path, "out")
		);

		_ = run.ExitCode.Should().NotBe(0, run.Output);
		_ = run.Output.Should().Contain("could not be read");
	}

	[Test]
	public async Task Push_ForAnotherEnvironmentThanExported_IsRefused()
	{
		using var folder = ExportedFolder();

		var run = await Cli.RunWithEnvironmentAsync(NoKey, "push", "--out", folder.Out, "--env", "prod", "--dry-run");

		_ = run.ExitCode.Should().NotBe(0, run.Output);
		_ = run.Output.Should().Contain("was exported for the demo environment, not prod");
	}

	[Test]
	public async Task Push_ForAnotherProjectThanExported_IsRefused()
	{
		using var folder = ExportedFolder();

		var run = await Cli.RunWithEnvironmentAsync(NoKey, "push", "--out", folder.Out, "--env", "demo", "--project", "other", "--dry-run");

		_ = run.ExitCode.Should().NotBe(0, run.Output);
		_ = run.Output.Should().Contain("was exported for project elastic-co-docs-demo, not other");
	}

	[Test]
	public async Task Export_KibanaUrlWithAnApostrophe_IsEscapedInTheConfig()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("landing/home", Monitor);

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
			"--kibana-url",
			"https://kibana.example/it's-kibana",
			"--dry-run"
		);

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run.Output.Should().Contain("url: 'https://kibana.example/it\\'s-kibana',");
	}

	[Test]
	[Arguments("edge")]
	[Arguments("demo")]
	[Arguments("staging")]
	public async Task Export_PagingOutsideProd_IsRefused(string env)
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("landing/home", Monitor);

		var run = await Cli.RunAsync(
			"export",
			"synthetics",
			"--url",
			site.Url,
			"--env",
			env,
			"--journeys",
			folder.Path,
			"--out",
			Path.Combine(folder.Path, "out"),
			"--paging",
			"--dry-run"
		);

		_ = run.ExitCode.Should().NotBe(0, run.Output);
		_ = run.Output.Should().Contain($"--paging is refused for the {env} environment: only prod pages");
		_ = run.Output.Should().NotContain("command:");
	}

	[Test]
	public async Task Export_PagingInProd_IsPassedToTheExporter()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("landing/home", Monitor);

		var run = await Cli.RunAsync(
			"export",
			"synthetics",
			"--url",
			site.Url,
			"--env",
			"prod",
			"--journeys",
			folder.Path,
			"--out",
			Path.Combine(folder.Path, "out"),
			"--paging",
			"--dry-run"
		);

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run.Output.Should().Contain("project: elastic-co-docs-prod").And.Contain(" --paging");
	}

	[Test]
	public async Task Export_AnEnvironmentThatCannotBeNamed_AsksForOne()
	{
		using var folder = new JourneyFolder().Add("landing/home", Monitor);

		var run = await Cli.RunAsync(
			"export",
			"synthetics",
			"--url",
			"https://unknown.example.test",
			"--journeys",
			folder.Path,
			"--out",
			Path.Combine(folder.Path, "out"),
			"--dry-run"
		);

		_ = run.ExitCode.Should().NotBe(0, run.Output);
		_ = run.Output.Should().Contain("The environment of unknown.example.test is unknown").And.Contain("Pass --env");
	}

	[Test]
	[Arguments("has space")]
	[Arguments("a'b")]
	public async Task Export_AProjectThatCouldBreakTheGeneratedConfig_IsRefused(string project)
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("landing/home", Monitor);

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
			"--project",
			project,
			"--dry-run"
		);

		_ = run.ExitCode.Should().NotBe(0, run.Output);
		_ = run.Output.Should().Contain("--project may only contain letters, digits");
	}

	[Test]
	public async Task Push_DryRun_ShowsTheCommandWithoutTagsAndNeedsNoKey()
	{
		using var folder = ExportedFolder();

		var run = await Cli.RunWithEnvironmentAsync(NoKey, "push", "--out", folder.Out, "--env", "demo", "--dry-run");

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run.Output.Should().Contain("would push 1 monitor(s) to project elastic-co-docs-demo (demo)");
		_ = run
			.Output
			.Should()
			.Contain(
				$"command: {NpxName} --yes @elastic/synthetics@1.27.0 push journeys --config synthetics.config.ts --id elastic-co-docs-demo --yes"
			);
		_ = run.Output.Should().Contain("SYNTHETICS_API_KEY: not set");
		_ = run.Output.Should().NotContain("--tags", "push --tags filters the monitors to push, it does not tag them");
	}

	[Test]
	public async Task Push_DryRun_NeverPrintsTheApiKey()
	{
		using var folder = ExportedFolder();
		var environment = new Dictionary<string, string?> { ["SYNTHETICS_API_KEY"] = "s3cret-key-value" };

		var run = await Cli.RunWithEnvironmentAsync(environment, "push", "--out", folder.Out, "--env", "demo", "--dry-run");

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run.Output.Should().Contain("SYNTHETICS_API_KEY: set");
		_ = run.Output.Should().NotContain("s3cret-key-value");
	}

	[Test]
	public async Task Push_WithoutAnApiKey_FailsBeforeRunningAnything()
	{
		using var folder = ExportedFolder();

		var run = await Cli.RunWithEnvironmentAsync(NoKey, "push", "--out", folder.Out, "--env", "demo");

		_ = run.ExitCode.Should().Be(1, run.Output);
		_ = run.Output.Should().Contain("SYNTHETICS_API_KEY is not set. Nothing was pushed.");
		_ = run.Output.Should().NotContain("pushing");
	}

	[Test]
	public async Task Push_OfNoMonitors_IsRefusedBecauseItWouldDeleteTheProjectsMonitors()
	{
		using var folder = ExportedFolder(monitors: 0);

		var run = await Cli.RunWithEnvironmentAsync(NoKey, "push", "--out", folder.Out, "--env", "demo", "--dry-run");

		_ = run.ExitCode.Should().Be(1, run.Output);
		_ = run.Output.Should().Contain("holds no monitors").And.Contain("would delete every monitor of elastic-co-docs-demo");
	}

	[Test]
	public async Task Push_WithoutTheInstalledPackages_SaysToExportFirst()
	{
		using var folder = ExportedFolder(install: false);

		var run = await Cli.RunWithEnvironmentAsync(NoKey, "push", "--out", folder.Out, "--env", "demo", "--dry-run");

		_ = run.ExitCode.Should().NotBe(0, run.Output);
		_ = run.Output.Should().Contain("node_modules does not exist").And.Contain("export synthetics");
	}

	[Test]
	public async Task Push_AnEnvThatCouldBreakTheProjectId_IsRefused()
	{
		using var folder = ExportedFolder();

		var run = await Cli.RunWithEnvironmentAsync(NoKey, "push", "--out", folder.Out, "--env", "de mo", "--dry-run");

		_ = run.ExitCode.Should().NotBe(0, run.Output);
		_ = run.Output.Should().Contain("--env may only contain letters, digits");
	}

	[Test]
	public async Task Export_ToAReusedOutFolder_RemovesStaleMonitorFiles()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "page-feedback");
		using var folder = new JourneyFolder().Add("landing/home", Monitor);
		var outDir = Path.Combine(folder.Path, "out");
		var stale = Path.Combine(outDir, "journeys", "deleted.journey.ts");
		_ = Directory.CreateDirectory(Path.GetDirectoryName(stale)!);
		File.WriteAllText(stale, "stale");

		_ = await Cli.RunAsync("export", "synthetics", "--url", site.Url, "--env", "demo", "--journeys", folder.Path, "--out", outDir);

		_ = File.Exists(stale).Should().BeFalse("the journeys folder is cleared before the export");
	}

	[Test]
	public async Task Export_WithAnOutThatOverlapsTheJourneys_IsRefusedBeforeAnythingIsDeleted()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "page-feedback");
		var root = Path.Combine(Path.GetTempPath(), "docs-journeys-" + Guid.NewGuid().ToString("N"));
		var journeys = Path.Combine(root, "journeys");
		_ = Directory.CreateDirectory(journeys);
		var journey = Path.Combine(journeys, "home.journey");
		File.WriteAllText(journey, Monitor);
		try
		{
			// Same folder, different case: only when this filesystem resolves it (case-insensitive volume).
			var upperCased = Path.Combine(root, "JOURNEYS");
			var differentCase = Directory.Exists(upperCased);
			foreach (var journeysArgument in differentCase ? new[] { journeys, upperCased } : [journeys])
			{
				var run = await Cli.RunAsync(
					"export",
					"synthetics",
					"--url",
					site.Url,
					"--env",
					"demo",
					"--journeys",
					journeysArgument,
					"--out",
					root
				);

				_ = run.ExitCode.Should().NotBe(0, run.Output);
				_ = run.Output.Should().Contain("would overwrite the journeys folder");
				_ = File.Exists(journey).Should().BeTrue("nothing is deleted when the folders overlap");
			}
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[Test]
	public async Task Export_ForReal_WritesTheMonitorsTheConfigAndInstallsThePackages()
	{
		if (!await NpxWorksAsync())
			Skip.Test("npx (Node.js) is not available, or cannot reach the npm registry.");
		await using var site = new FakeSite(buildType: "assembler", features: "page-feedback");
		using var folder = new JourneyFolder().Add("landing/home", Monitor).Add(
			"landing/isolated",
			"# @requires build-type:isolated\n# @monitor\nopen /page"
		);
		var outDir = Path.Combine(folder.Path, "out");

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
			outDir
		);

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run.Output.Should().Contain("1 monitor file(s)");
		var generated = File.ReadAllText(Path.Combine(outDir, "journeys", "landing", "home.journey.ts"));
		_ = generated.Should().Contain("monitor.use({ id: 'landing-home', schedule: 10, locations: ['germany'], tags: ['smoke'] })");
		_ = generated.Should().Contain("features: ['page-feedback']").And.Contain("env: 'demo'");
		_ = File
			.Exists(Path.Combine(outDir, "journeys", "landing", "isolated.journey.ts"))
			.Should()
			.BeFalse("its @requires does not hold on an assembler site");
		_ = File.ReadAllText(Path.Combine(outDir, "package.json")).Should().Contain("\"@nullean/journeys\": \"0.9.0\"");
		_ = Directory.Exists(Path.Combine(outDir, "node_modules", "@nullean", "journeys")).Should().BeTrue();
	}

	[Test]
	public async Task Export_OfNoApplicableMonitor_FailsUnlessEmptyIsAllowed()
	{
		if (!await NpxWorksAsync())
			Skip.Test("npx (Node.js) is not available, or cannot reach the npm registry.");
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("landing/isolated", "# @requires build-type:isolated\n# @monitor\nopen /page");
		var outDir = Path.Combine(folder.Path, "out");

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
			outDir
		);
		var allowed = await Cli.RunAsync(
			"export",
			"synthetics",
			"--url",
			site.Url,
			"--env",
			"demo",
			"--journeys",
			folder.Path,
			"--out",
			Path.Combine(folder.Path, "out2"),
			"--allow-empty"
		);

		_ = run.ExitCode.Should().Be(1, run.Output);
		_ = run.Output.Should().Contain("No journey was exported").And.Contain("would delete every monitor");
		_ = allowed.ExitCode.Should().Be(0, allowed.Output);
	}

	private static string NpxName => OperatingSystem.IsWindows() ? "npx.cmd" : "npx";

	private static ExportedOutput ExportedFolder(int monitors = 1, bool install = true)
	{
		var output = new ExportedOutput();
		File.WriteAllText(
			Path.Combine(output.Out, "synthetics.config.ts"),
			"export default () => ({\n    params: {\n        environment: 'demo',\n    },\n    project: {\n        id: 'elastic-co-docs-demo',\n    },\n})"
		);
		_ = Directory.CreateDirectory(Path.Combine(output.Out, "journeys"));
		for (var i = 0; i < monitors; i++)
			File.WriteAllText(Path.Combine(output.Out, "journeys", $"m{i}.journey.ts"), "// generated");
		if (install)
			_ = Directory.CreateDirectory(Path.Combine(output.Out, "node_modules"));
		return output;
	}

	private static async Task<bool> NpxWorksAsync()
	{
		try
		{
			var info = new ProcessStartInfo(OperatingSystem.IsWindows() ? "npm.cmd" : "npm")
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			info.ArgumentList.Add("view");
			info.ArgumentList.Add("@nullean/journeys@0.9.0");
			info.ArgumentList.Add("version");
			using var process = Process.Start(info);
			if (process is null)
				return false;
			_ = process.StandardOutput.ReadToEndAsync();
			_ = process.StandardError.ReadToEndAsync();
			await process.WaitForExitAsync().ConfigureAwait(false);
			return process.ExitCode == 0;
		}
		catch (System.ComponentModel.Win32Exception)
		{
			return false;
		}
	}

	private sealed class ExportedOutput : IDisposable
	{
		private readonly string _root = Path.Combine(Path.GetTempPath(), "docs-journeys-export-" + Guid.NewGuid().ToString("N"));

		public ExportedOutput() => Directory.CreateDirectory(Out);

		public string Out => Path.Combine(_root, "out");

		public void Dispose() => Directory.Delete(_root, recursive: true);
	}
}
