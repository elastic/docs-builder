// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;

namespace Elastic.Documentation.Journeys.Tests;

/// <summary>
/// <c>docs-journeys export synthetics</c> as a process. It runs no Node: it writes a project that the caller validates and
/// pushes with npm, so everything here runs without Node, a network or a Kibana. The files are compared with what the npm
/// exporter writes in <see cref="DocsJourneysExportConformanceTests"/>.
/// </summary>
public class DocsJourneysExportTests
{
	private const string Monitor = "# @tags smoke\n# @monitor schedule=10 locations=germany\nopen /page\nexpect title Page";

	[Test]
	public async Task Export_DryRun_PrintsTheContextTheConfigTheFilesAndTheManifestAndWritesNothing()
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

		run.ShouldHaveExited(0);
		_ = run.Output.Should().Contain("context: env=demo (--env), build-type=assembler, features=[navigation-preview page-feedback]");
		_ = run.Output.Should().Contain("project: elastic-co-docs-demo");
		_ = run.Output.Should().Contain("id: 'elastic-co-docs-demo',").And.Contain("space: 'default',").And.Contain("schedule: 15,");
		_ = run.Output.Should().Contain("url: 'https://elastic-docs-v3-prod-e6053a.kb.us-east-1.aws.elastic.cloud/',");
		_ = run.Output.Should().Contain(Path.Combine("journeys", "landing", "home.journey.ts"));
		_ = run.Output.Should().Contain("\"file\": \"journeys/landing/home.journey.ts\"");
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

		run.ShouldHaveFailed();
		_ = run.Output.Should().Contain("could not be read");
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

		run.ShouldHaveExited(0);
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
			"--paging"
		);

		run.ShouldHaveFailed();
		_ = run.Output.Should().Contain("--paging is refused").And.Contain("only prod pages");
		_ = Directory.Exists(Path.Combine(folder.Path, "out")).Should().BeFalse();
	}

	[Test]
	public async Task Export_PagingInProd_AddsTheAlertOnlyToMonitorsThatAskForPagerDuty()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add(
			"a/pages",
			"# @tags smoke\n# @monitor schedule=10 locations=germany\n# @alert pagerduty\nopen /page\nexpect title Page"
		).Add("a/slack", "# @monitor\n# @alert slack\nopen /page");
		var withPaging = Path.Combine(folder.Path, "with");
		var without = Path.Combine(folder.Path, "without");

		var on = await Cli.RunAsync(
			"export",
			"synthetics",
			"--url",
			site.Url,
			"--env",
			"prod",
			"--journeys",
			folder.Path,
			"--out",
			withPaging,
			"--paging"
		);
		var off = await Cli.RunAsync(
			"export",
			"synthetics",
			"--url",
			site.Url,
			"--env",
			"prod",
			"--journeys",
			folder.Path,
			"--out",
			without
		);

		on.ShouldHaveExited(0);
		off.ShouldHaveExited(0);
		var paged = File.ReadAllText(Path.Combine(withPaging, "journeys", "a", "pages.journey.ts"));
		_ = paged.Should().Contain("alert: { status: { enabled: true } }").And.Contain("'alert:pagerduty'");
		_ = File.ReadAllText(Path.Combine(withPaging, "journeys", "a", "slack.journey.ts")).Should().NotContain("alert:");
		_ = File.ReadAllText(Path.Combine(without, "journeys", "a", "pages.journey.ts")).Should().NotContain("alert:");
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

		run.ShouldHaveFailed();
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

		run.ShouldHaveFailed();
		_ = run.Output.Should().Contain("--project may only contain letters, digits");
	}

	[Test]
	public async Task Export_WritesTheMonitorsTheConfigThePackagesTheLockfileAndTheManifest()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "page-feedback");
		using var folder = new JourneyFolder().Add("landing/home", Monitor).Add("navigation/main", "# @monitor schedule=15\nopen /page");
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

		run.ShouldHaveExited(0);
		_ = File
			.ReadAllText(Path.Combine(outDir, "manifest.json"))
			.Should()
			.Be(
				"{\n" + "  \"project\": \"elastic-co-docs-demo\",\n" + "  \"env\": \"demo\",\n" + "  \"monitors\": [\n" + "    {\n" +
					"      \"name\": \"landing/home\",\n" + "      \"file\": \"journeys/landing/home.journey.ts\"\n" + "    },\n" +
					"    {\n" + "      \"name\": \"navigation/main\",\n" + "      \"file\": \"journeys/navigation/main.journey.ts\"\n" +
					"    }\n" + "  ]\n" + "}\n"
			);
		_ = File.Exists(Path.Combine(outDir, "journeys", "landing", "home.journey.ts")).Should().BeTrue();
		_ = File.ReadAllText(Path.Combine(outDir, "synthetics.config.ts")).Should().Contain("id: 'elastic-co-docs-demo',");
		_ = File
			.ReadAllText(Path.Combine(outDir, "journeys", "landing", "home.journey.ts"))
			.Should()
			.StartWith("// Generated by `journeys export-synthetics` from landing/home.journey.")
			.And
			.Contain($"baseUrl: '{site.Url}/docs/'")
			.And
			.Contain("features: ['page-feedback']")
			.And
			.Contain("buildType: 'assembler'")
			.And
			.Contain("env: 'demo'")
			.And
			.Contain("readOnly: true");
	}

	[Test]
	public async Task Export_Lockfile_AgreesWithThePinsInPackageJson()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
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
			outDir
		);

		run.ShouldHaveExited(0);
		using var package = JsonDocument.Parse(File.ReadAllText(Path.Combine(outDir, "package.json")));
		using var lockfile = JsonDocument.Parse(File.ReadAllText(Path.Combine(outDir, "package-lock.json")));
		var pins = package.RootElement.GetProperty("dependencies").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString());
		var packages = lockfile.RootElement.GetProperty("packages");
		var root = packages.GetProperty("");
		_ = root.GetProperty("name").GetString().Should().Be(package.RootElement.GetProperty("name").GetString());
		_ = pins.Should().NotBeEmpty();
		foreach (var (name, version) in pins)
		{
			_ = root.GetProperty("dependencies").GetProperty(name).GetString().Should().Be(version, $"the lockfile asks for {name}");
			_ = packages
				.GetProperty("node_modules/" + name)
				.GetProperty("version")
				.GetString()
				.Should()
				.Be(version, $"the lockfile installs {name}");
		}
	}

	[Test]
	public async Task Export_SaysWhyAJourneyIsNotAMonitorAndLeavesItOutOfTheManifest()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "page-feedback");
		using var folder = new JourneyFolder()
			.Add("a/plain", "open /page")
			.Add("b/mutates", "# @monitor\n# @mutates\nopen /page")
			.Add("c/needs-flag", "# @monitor\n# @requires navigation-preview\nopen /page")
			.Add("d/applies", "# @monitor\n# @requires page-feedback\nopen /page");
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

		run.ShouldHaveExited(0);
		_ = run.Output.Should().Contain("skipped a/plain: no @monitor");
		_ = run.Output.Should().Contain("skipped b/mutates: @mutates: a monitor runs against a live site");
		_ = run.Output.Should().Contain("skipped c/needs-flag: @requires navigation-preview does not hold");
		_ = run.Output.Should().Contain("exported 1 of 4 for project elastic-co-docs-demo");
		using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(outDir, "manifest.json")));
		_ = manifest
			.RootElement
			.GetProperty("monitors")
			.EnumerateArray()
			.Select(m => m.GetProperty("name").GetString())
			.Should()
			.Equal("d/applies");
	}

	[Test]
	public async Task Export_AnHttpFileThatIsAMonitor_BecomesAnHttpStepsFileInTheManifest()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().AddHttp(
			"mcp/tools",
			"# @monitor schedule=15\n# @tags mcp\n@host = https://docs.example\n\n# @readonly\n# @expect status 200\nPOST {{host}}/docs/_mcp\nContent-Type: application/json\n\n{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}\n"
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

		run.ShouldHaveExited(0);
		var content = File.ReadAllText(Path.Combine(outDir, "journeys", "mcp", "tools.http.journey.ts"));
		_ = content.Should().Contain("import { httpSteps } from '@nullean/journeys/synthetics';").And.NotContain("expect,");
		_ = File.ReadAllText(Path.Combine(outDir, "manifest.json")).Should().Contain("\"file\": \"journeys/mcp/tools.http.journey.ts\"");
	}

	[Test]
	public async Task Export_OfNoApplicableMonitor_FailsUnlessEmptyIsAllowed()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("a/plain", "open /page");
		var strictOut = Path.Combine(folder.Path, "strict");
		var emptyOut = Path.Combine(folder.Path, "empty");

		var strict = await Cli.RunAsync(
			"export",
			"synthetics",
			"--url",
			site.Url,
			"--env",
			"demo",
			"--journeys",
			folder.Path,
			"--out",
			strictOut
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
			emptyOut,
			"--allow-empty"
		);

		strict.ShouldHaveExited(1);
		_ = strict.Output.Should().Contain("No journey was exported").And.Contain("--allow-empty");
		_ = Directory.Exists(strictOut).Should().BeFalse("nothing is written for a failed export");
		allowed.ShouldHaveExited(0);
		_ = File.ReadAllText(Path.Combine(emptyOut, "manifest.json")).Should().Contain("\"monitors\": []");
	}

	[Test]
	public async Task Export_ToAReusedOutFolder_RemovesOnlyGeneratedMonitorFilesThatAreNoLongerProduced()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "page-feedback");
		using var folder = new JourneyFolder().Add("landing/home", Monitor);
		var outDir = Path.Combine(folder.Path, "out");
		var generated = Path.Combine(outDir, "journeys", "deleted.journey.ts");
		var handWritten = Path.Combine(outDir, "journeys", "mine.journey.ts");
		var unrelated = Path.Combine(outDir, "notes.txt");
		_ = Directory.CreateDirectory(Path.GetDirectoryName(generated)!);
		File.WriteAllText(generated, "// Generated by `journeys export-synthetics` from deleted.journey. Do not edit.\n");
		File.WriteAllText(handWritten, "// written by a person\n");
		File.WriteAllText(unrelated, "keep");

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

		run.ShouldHaveExited(0);
		_ = File.Exists(generated).Should().BeFalse("a generated file that is not produced any more is removed");
		_ = File.Exists(handWritten).Should().BeTrue("a file that does not say it was generated is never removed");
		_ = File.Exists(unrelated).Should().BeTrue();
	}

	[Test]
	public async Task Export_Twice_WritesTheSameBytes()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "page-feedback");
		using var folder = new JourneyFolder().Add("landing/home", Monitor).Add("a/b", "# @monitor\nopen /page");
		var first = Path.Combine(folder.Path, "first");
		var second = Path.Combine(folder.Path, "second");

		_ = await Cli.RunAsync("export", "synthetics", "--url", site.Url, "--env", "demo", "--journeys", folder.Path, "--out", first);
		_ = await Cli.RunAsync("export", "synthetics", "--url", site.Url, "--env", "demo", "--journeys", folder.Path, "--out", second);

		var files = Directory
			.EnumerateFiles(first, "*", SearchOption.AllDirectories)
			.Select(f => Path.GetRelativePath(first, f))
			.Order()
			.ToList();
		_ = files.Should().NotBeEmpty();
		foreach (var file in files)
			_ = File.ReadAllText(Path.Combine(second, file)).Should().Be(File.ReadAllText(Path.Combine(first, file)), file);
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

				run.ShouldHaveFailed();
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
	public async Task Export_TwoSourcesThatWouldShareAMonitorId_AreAnErrorNamingBoth()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("mcp/tools", "# @monitor\nopen /page").AddHttp(
			"mcp-tools",
			"# @monitor\nGET https://docs.example/health\n"
		);

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
			Path.Combine(folder.Path, "out")
		);

		run.ShouldHaveExited(1);
		_ = run.Output.Should().Contain("mcp/tools: its monitor id 'mcp-tools' is already taken by mcp-tools");
	}

	[Test]
	[Arguments("# @monitor schedule=7\nopen /page", "@monitor schedule=7 is not one Synthetics accepts")]
	[Arguments("# @monitor locations=mars\nopen /page", "@monitor location 'mars' is not a public Synthetics location")]
	public async Task Export_AMonitorSettingSyntheticsRefuses_IsAnErrorNamingTheJourney(string journey, string message)
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("landing/home", journey);

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
			Path.Combine(folder.Path, "out")
		);

		run.ShouldHaveExited(1);
		_ = run.Output.Should().Contain($"landing/home: {message}");
	}

	[Test]
	public async Task Export_AJourneyThatDoesNotParse_ExitsOneAndSaysWhere()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("landing/broken", "# @monitor\nthis-is-not-a-command /page");
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

		run.ShouldHaveExited(1);
		_ = run.Output.Should().Contain("landing/broken");
		_ = Directory.Exists(outDir).Should().BeFalse("nothing is written for a journey that cannot be exported");
	}

	[Test]
	public async Task Push_IsNotACommandAnyMore()
	{
		var run = await Cli.RunAsync("push", "--out", "x", "--env", "demo");

		run.ShouldHaveFailed();
	}
}
