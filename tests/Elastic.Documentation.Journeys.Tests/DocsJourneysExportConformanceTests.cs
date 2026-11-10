// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel;
using System.Diagnostics;
using AwesomeAssertions;

namespace Elastic.Documentation.Journeys.Tests;

/// <summary>
/// The generator in docs-journeys is a port of <c>journeys export-synthetics</c> from <c>@nullean/journeys</c>, and a monitor
/// that differs from what that package writes would run differently in Synthetics. This test exports the same journeys with
/// both and requires the files to be byte for byte the same. It needs <c>npx</c> and the npm registry, so it skips without
/// them; the CI shard that runs it has both. docs-journeys itself starts no Node.
/// </summary>
public class DocsJourneysExportConformanceTests
{
	private const string Package = "@nullean/journeys@0.9.0";

	[Test]
	public async Task Export_WritesTheSameFilesAsTheNpmExporter()
	{
		if (!await NpmWorksAsync().ConfigureAwait(false))
			Skip.Test("npx or the npm registry is not available, so the npm exporter cannot run.");

		await using var site = new FakeSite(buildType: "assembler", features: "page-feedback navigation-preview");
		using var folder = Fixtures();

		// One export with paging off in an environment that is not prod, and one with paging on in prod.
		await AssertSameAsync(site, folder, env: "demo", paging: false).ConfigureAwait(false);
		await AssertSameAsync(site, folder, env: "prod", paging: true).ConfigureAwait(false);
	}

	private static async Task AssertSameAsync(FakeSite site, JourneyFolder folder, string env, bool paging)
	{
		var native = Path.Combine(folder.Path, "out", env, "native");
		var npm = Path.Combine(folder.Path, "out", env, "npm");
		var journeyBase = $"{site.Url}/docs/";

		var arguments = new List<string>
		{
			"export",
			"synthetics",
			"--url",
			site.Url,
			"--env",
			env,
			"--journeys",
			folder.Path,
			"--out",
			native
		};
		if (paging)
			arguments.Add("--paging");
		var run = await Cli.RunAsync(arguments.ToArray()).ConfigureAwait(false);
		run.ShouldHaveExited(0);

		var npx = new List<string>
		{
			"--yes",
			Package,
			"export-synthetics",
			folder.Path,
			"--root",
			folder.Path,
			"--out",
			Path.Combine(npm, "journeys"),
			"--base-url",
			journeyBase,
			"--features",
			"navigation-preview,page-feedback",
			"--build-type",
			"assembler",
			"--env",
			env
		};
		if (paging)
			npx.Add("--paging");
		var reference = await RunNpxAsync(npx).ConfigureAwait(false);
		if (reference.ExitCode != 0)
			throw new DocsJourneysRunException($"npx {Package} exited with code {reference.ExitCode}:\n{reference.Output}");

		var nativeFiles = Monitors(Path.Combine(native, "journeys"));
		var npmFiles = Monitors(Path.Combine(npm, "journeys"));
		_ = nativeFiles.Keys.Should().Equal(npmFiles.Keys, $"both exporters write the same monitors in {env}");
		_ = nativeFiles.Should().NotBeEmpty();
		foreach (var (name, content) in nativeFiles)
			_ = content.Should().Be(npmFiles[name], $"{name} ({env}) must be byte for byte what the npm exporter writes");
	}

	private static SortedDictionary<string, string> Monitors(string folder) =>
		new(
			Directory.EnumerateFiles(folder, "*.ts", SearchOption.AllDirectories).ToDictionary(
				f => Path.GetRelativePath(folder, f).Replace('\\', '/'),
				File.ReadAllText
			),
			StringComparer.Ordinal
		);

	// Every case the exporter has rules for: with and without @monitor, @requires met and unmet, feature blocks, paging,
	// @mutates, text that needs escaping in a template literal, and an .http monitor.
	private static JourneyFolder Fixtures() =>
		new JourneyFolder()
			.Add(
				"a/plain",
				"# @tags navigation smoke\n# @monitor schedule=10 locations=germany,us_east\nopen /docs\nexpect title \"Elastic Docs\"\n"
			)
			.Add("a/no-monitor", "open /docs\n")
			.Add("b/mutates", "# @monitor\n# @mutates\nopen /docs\n")
			.Add("c/needs-flag-met", "# @monitor schedule=15\n# @requires navigation-preview\nopen /docs\n")
			.Add("c/needs-flag-unmet", "# @monitor\n# @requires assembler-api-explorer\nopen /docs\n")
			.Add("c/needs-build-type", "# @monitor\n# @requires build-type:assembler,codex\nopen /docs\n")
			.Add("d/not-prod", "# @monitor\n# @requires !env:prod\nopen /docs\n")
			.Add(
				"e/blocks",
				"# @monitor schedule=10 locations=germany,us_east\nopen /docs\nfeature navigation-preview\n  expect visible #pages-nav\nend\nfeature !navigation-preview\n  expect visible #toc-nav\nend\n"
			)
			.Add("f/escaping", "# @monitor\nopen /docs\nexpect text #x \"a `tick` and ${not} and a back\\\\slash and 'quotes'\"\n")
			.Add("g/pages", "# @monitor schedule=10 locations=germany\n# @alert pagerduty\nopen /docs\n")
			.Add("g/slack", "# @monitor\n# @alert slack\nopen /docs\n")
			.Add("g/weird name", "# @monitor\nopen /docs\n")
			.AddHttp(
				"mcp/tools",
				"# @monitor schedule=15\n# @tags mcp\n# @requires !env:prod\n@host = https://docs.example\n\n# @readonly\n# @expect status 200\n# @expect json $.result.tools[*].name contains \"search_docs\"\nPOST {{host}}/docs/_mcp\nContent-Type: application/json\n\n{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}\n"
			)
			.AddHttp("mcp/health", "# @monitor\nGET https://docs.example/health\n")
			.AddHttp("mcp/no-monitor", "GET https://docs.example/health\n");

	private static async Task<bool> NpmWorksAsync()
	{
		var run = await RunNpmAsync("view", Package, "version").ConfigureAwait(false);
		return run.ExitCode == 0;
	}

	private static Task<(int ExitCode, string Output)> RunNpxAsync(IEnumerable<string> arguments) =>
		RunAsync(OperatingSystem.IsWindows() ? "npx.cmd" : "npx", arguments);

	private static Task<(int ExitCode, string Output)> RunNpmAsync(params string[] arguments) =>
		RunAsync(OperatingSystem.IsWindows() ? "npm.cmd" : "npm", arguments);

	private static async Task<(int ExitCode, string Output)> RunAsync(string command, IEnumerable<string> arguments)
	{
		var info = new ProcessStartInfo(command) { RedirectStandardOutput = true, RedirectStandardError = true };
		foreach (var argument in arguments)
			info.ArgumentList.Add(argument);
		try
		{
			using var process = Process.Start(info);
			if (process is null)
				return (-1, $"{command} did not start");
			var output = process.StandardOutput.ReadToEndAsync();
			var error = process.StandardError.ReadToEndAsync();
			await process.WaitForExitAsync().ConfigureAwait(false);
			return (process.ExitCode, await output.ConfigureAwait(false) + await error.ConfigureAwait(false));
		}
		catch (Win32Exception e)
		{
			return (-1, e.Message);
		}
	}
}
