// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using AwesomeAssertions;

namespace Elastic.Documentation.Journeys.Tests;

/// <summary>
/// <c>docs-journeys run</c> and <c>list</c> as a process, against a fake site that serves the <c>docs:*</c> tags and
/// records who asked, so what the tool does with its context, identity and read-only default is checked end to end.
/// </summary>
public class DocsJourneysContextTests
{
	private const string Passing = "open /page\nexpect title Page";

	[Test]
	public async Task Run_SiteTags_DecideWhichJourneysRunAndWhichAreSkippedWithAReason()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "page-feedback navigation-preview");
		using var folder = new JourneyFolder()
			.Add("a/assembler", $"# @requires build-type:assembler\n{Passing}")
			.Add("a/isolated", $"# @requires build-type:isolated\n{Passing}")
			.Add("a/flag", $"# @requires navigation-preview\n{Passing}")
			.Add("a/no-flag", $"# @requires !navigation-preview\n{Passing}");

		var run = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path, "--junit", folder.JUnit);

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run.Output.Should().Contain("context: env=local, build-type=assembler, features=[navigation-preview page-feedback]");
		_ = run.Output.Should().Contain("ok      a/assembler").And.Contain("ok      a/flag");
		_ = run
			.Output
			.Should()
			.Contain("skipped a/isolated (requires build-type:isolated)")
			.And
			.Contain("skipped a/no-flag (requires !navigation-preview)");
		_ = run.Output.Should().Contain("2 passed, 0 failed, 2 skipped");
		var junit = File.ReadAllText(folder.JUnit);
		_ = junit.Should().Contain("skipped=\"2\"").And.Contain("<skipped message=\"requires build-type:isolated\"");
	}

	[Test]
	public async Task Run_SiteWithoutTags_IsAnAssemblerSiteWithNoFeaturesSoOnlyMatchingJourneysRun()
	{
		await using var site = new FakeSite(buildType: null, features: null);
		using var folder = new JourneyFolder()
			.Add("a/plain", Passing)
			.Add("a/needs-build", $"# @requires build-type:isolated\n{Passing}")
			.Add("a/assembler", $"# @requires build-type:assembler\n{Passing}");

		var run = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path);

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run
			.Output
			.Should()
			.Contain("has no docs:build-type or docs:features tags")
			.And
			.Contain("build-type=assembler")
			.And
			.Contain("features=[]");
		_ = run.Output.Should().Contain("ok      a/plain").And.Contain("ok      a/assembler").And.Contain("skipped a/needs-build");
	}

	[Test]
	public async Task Run_ExplicitBuildTypeAndFeatures_WinOverTheSitesTags()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "page-feedback");
		using var folder = new JourneyFolder().Add("a/isolated", $"# @requires build-type:isolated\n{Passing}").Add(
			"a/flag",
			$"# @requires navigation-preview\n{Passing}"
		);

		var run = await Cli.RunAsync(
			"run",
			"--url",
			site.Url,
			"--journeys",
			folder.Path,
			"--build-type",
			"isolated",
			"--feature",
			"navigation-preview"
		);

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run
			.Output
			.Should()
			.Contain("ok      a/isolated")
			.And
			.Contain("ok      a/flag")
			.And
			.Contain("build-type=isolated (--build-type)");
	}

	[Test]
	public async Task Run_Env_IsTakenFromTheFlagAndSkipsJourneysThatExcludeIt()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("a/not-prod", $"# @requires !env:prod\n{Passing}").Add("a/any", Passing);

		var run = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path, "--env", "prod");

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run
			.Output
			.Should()
			.Contain("env=prod (--env)")
			.And
			.Contain("skipped a/not-prod (requires !env:prod)")
			.And
			.Contain("ok      a/any");
	}

	[Test]
	public async Task Run_EverySelectedJourneySkipped_ExitsOneBecauseNothingWasChecked()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("a/isolated", $"# @requires build-type:isolated\n{Passing}");

		var run = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path);

		_ = run.ExitCode.Should().Be(1, run.Output);
		_ = run.Output.Should().Contain("Every selected journey was skipped");
	}

	[Test]
	public async Task Run_MutatingJourney_IsSkippedUnlessMutationsAreAllowed()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("a/writes", $"# @mutates\n{Passing}").Add("a/reads", Passing);

		var skipped = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path);
		var allowed = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path, "--allow-mutations");

		_ = skipped.Output.Should().Contain("skipped a/writes (mutates data; a read-only run skips it)").And.Contain("ok      a/reads");
		_ = allowed.Output.Should().Contain("ok      a/writes").And.Contain("2 passed, 0 failed, 0 skipped");
	}

	[Test]
	public async Task Run_Tag_RunsOnlyJourneysWithThatTag()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("a/smoke", $"# @tags smoke nav\n{Passing}").Add("a/slow", $"# @tags slow\n{Passing}");

		var run = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path, "--tag", "smoke");

		_ = run.Output.Should().Contain("ok      a/smoke").And.Contain("skipped a/slow (no tag in [smoke])");
	}

	[Test]
	public async Task Run_ChangedOnly_RunsJourneysThatDifferFromTheRefOrAreNew()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("a/committed", Passing).Add("a/edited", Passing);
		await Cli.GitAsync(folder.Path, "init", "-q", "-b", "main");
		await Cli.GitAsync(folder.Path, "add", ".");
		await Cli.GitAsync(
			folder.Path,
			"-c",
			"user.name=t",
			"-c",
			"user.email=t@example.com",
			"-c",
			"commit.gpgsign=false",
			"commit",
			"-q",
			"-m",
			"base"
		);
		folder.Add("a/edited", $"{Passing}\n# edited").Add("a/new", Passing);

		var run = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path, "--changed-only", "HEAD");
		var none = await Cli.RunAsync(
			"run",
			"--url",
			site.Url,
			"--journeys",
			folder.Path,
			"--changed-only",
			"HEAD",
			"--area",
			"a",
			"--tag",
			"none"
		);

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run.Output.Should().Contain("a/edited").And.Contain("a/new").And.NotContain("a/committed");
		_ = none.Output.Should().NotContain("a/committed");
	}

	[Test]
	public async Task Run_SiteServedFromTheRoot_FallsBackToTheRootPageForItsTags()
	{
		await using var site = new FakeSite(buildType: "isolated", features: "", servedFromRoot: true);
		using var folder = new JourneyFolder().Add("a/isolated", $"# @requires build-type:isolated\n{Passing}");

		var run = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path);

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run.Output.Should().Contain("build-type=isolated").And.Contain("ok      a/isolated");
	}

	[Test]
	public async Task Run_EmptyFeatureOverride_MeansNoFeaturesEvenWhenTheSiteHasSome()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "page-feedback");
		using var folder = new JourneyFolder().Add("a/flag", $"# @requires page-feedback\n{Passing}").Add(
			"a/no-flag",
			$"# @requires !page-feedback\n{Passing}"
		);

		var run = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path, "--feature", "");

		_ = run.Output.Should().Contain("features=[]").And.Contain("ok      a/no-flag").And.Contain("skipped a/flag");
	}

	[Test]
	public async Task Run_ChangedOnly_FindsJourneysWithNonAsciiNames()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("a/base", Passing);
		await Cli.GitAsync(folder.Path, "init", "-q", "-b", "main");
		await Cli.GitAsync(folder.Path, "add", ".");
		await Cli.GitAsync(
			folder.Path,
			"-c",
			"user.name=t",
			"-c",
			"user.email=t@example.com",
			"-c",
			"commit.gpgsign=false",
			"commit",
			"-q",
			"-m",
			"base"
		);
		folder.Add("a/café", Passing);

		var run = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path, "--changed-only", "HEAD");

		_ = run.ExitCode.Should().Be(0, run.Output);
		_ = run.Output.Should().Contain("a/café").And.NotContain("No journeys changed");
	}

	[Test]
	public async Task Run_ByDefault_IdentifiesAsAMonitorAndRealUserAgentTurnsThatOff()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("a/plain", Passing);

		_ = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path);
		var monitor = site.PageRequests.ToList();
		site.PageRequests.Clear();
		_ = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path, "--real-user-agent");
		var person = site.PageRequests.ToList();

		_ = monitor.Should().NotBeEmpty().And.OnlyContain(r => r.UserAgent.Contains("Elastic/Synthetics") && r.MonitorHeader == "true");
		_ = person.Should().NotBeEmpty().And.OnlyContain(r => !r.UserAgent.Contains("Elastic/Synthetics") && r.MonitorHeader == null);
	}

	[Test]
	public async Task Run_ByDefault_RefusesAWriteAndAllowWritesLetsItThrough()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("a/save", "open /page\nfind role button click --name Save\nexpect title Saved");

		var refused = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path);
		var allowed = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path, "--allow-writes");

		_ = refused.ExitCode.Should().Be(1, refused.Output);
		_ = refused.Output.Should().Contain("Blocked POST").And.Contain("read-only");
		_ = site.Writes.Should().Be(1, "only the --allow-writes run reached the server");
		_ = allowed.ExitCode.Should().Be(0, allowed.Output);
	}

	[Test]
	public async Task Run_WritableGlob_LetsAReadOnlyRunWriteToThatUrl()
	{
		await using var site = new FakeSite(buildType: "assembler", features: "");
		using var folder = new JourneyFolder().Add("a/save", "open /page\nfind role button click --name Save\nexpect title Saved");

		var run = await Cli.RunAsync("run", "--url", site.Url, "--journeys", folder.Path, "--writable", "**/save");

		_ = run.ExitCode.Should().Be(0, run.Output);
	}

	[Test]
	public async Task List_ShowsWhatEachHeaderSaysAndJsonCarriesTheSame()
	{
		using var folder = new JourneyFolder().Add(
			"a/full",
			"# @tags smoke\n# @requires build-type:assembler !env:prod\n# @monitor schedule=10 locations=germany,us_east\n# @alert pagerduty\n# @mutates\nopen /"
		).Add("a/plain", "open /");

		var text = await Cli.RunAsync("list", "--journeys", folder.Path);
		var json = await Cli.RunAsync("list", "--journeys", folder.Path, "--json");

		_ = text
			.Output
			.Should()
			.Contain(
				"a/full  tags=smoke requires=build-type:assembler !env:prod monitor=schedule:10/locations:germany,us_east alert=pagerduty mutates"
			);
		_ = text.Output.Should().Contain("a/plain").And.NotContain("a/plain  ");
		_ = json
			.Output
			.Should()
			.Contain("\"id\": \"a/full\"")
			.And
			.Contain("\"schedule\": 10")
			.And
			.Contain("\"mutates\": true")
			.And
			.Contain("\"build-type:assembler\"");
	}

	[Test]
	public async Task List_UnparsableJourney_IsShownAsAnErrorAndExitsOne()
	{
		using var folder = new JourneyFolder().Add("a/ok", "open /").Add("a/bad", "# @requries nothing\nopen /");

		var run = await Cli.RunAsync("list", "--journeys", folder.Path);

		_ = run.ExitCode.Should().Be(1, run.Output);
		_ = run.Output.Should().Contain("a/bad  ERROR");
	}
}

internal static class Cli
{
	public static async Task<(int ExitCode, string Output)> RunAsync(params string[] arguments)
	{
		var info = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
		info.ArgumentList.Add(FindDll());
		foreach (var argument in arguments)
			info.ArgumentList.Add(argument);
		return await RunAsync(info).ConfigureAwait(false);
	}

	/// <summary>Runs the tool with some environment variables set, or removed when the value is <c>null</c>.</summary>
	public static async Task<(int ExitCode, string Output)> RunWithEnvironmentAsync(
		IReadOnlyDictionary<string, string?> environment,
		params string[] arguments
	)
	{
		var info = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
		info.ArgumentList.Add(FindDll());
		foreach (var argument in arguments)
			info.ArgumentList.Add(argument);
		foreach (var (name, value) in environment)
		{
			if (value is null)
				_ = info.Environment.Remove(name);
			else
				info.Environment[name] = value;
		}
		return await RunAsync(info).ConfigureAwait(false);
	}

	public static async Task GitAsync(string folder, params string[] arguments)
	{
		var info = new ProcessStartInfo("git") { WorkingDirectory = folder, RedirectStandardOutput = true, RedirectStandardError = true };
		foreach (var argument in arguments)
			info.ArgumentList.Add(argument);
		var (code, output) = await RunAsync(info).ConfigureAwait(false);
		_ = code.Should().Be(0, output);
	}

	private static async Task<(int ExitCode, string Output)> RunAsync(ProcessStartInfo info)
	{
		using var process = Process.Start(info) ?? throw new InvalidOperationException($"{info.FileName} did not start.");
		var output = process.StandardOutput.ReadToEndAsync();
		var error = process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync().ConfigureAwait(false);
		return (process.ExitCode, await output.ConfigureAwait(false) + await error.ConfigureAwait(false));
	}

	private static string FindDll()
	{
		var configuration = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)).Name.ToLowerInvariant();
		for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
		{
			var dll = Path.Combine(directory.FullName, ".artifacts", "bin", "docs-journeys", configuration, "docs-journeys.dll");
			if (File.Exists(dll))
				return dll;
		}
		throw new FileNotFoundException("docs-journeys.dll was not built; the test project references it.");
	}
}

internal sealed class JourneyFolder : IDisposable
{
	public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "docs-journeys-" + Guid.NewGuid().ToString("N"));
	public string JUnit => System.IO.Path.Combine(Path, "junit.xml");

	public JourneyFolder Add(string id, string journey)
	{
		var file = System.IO.Path.Combine(Path, id + ".journey");
		_ = Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file)!);
		File.WriteAllText(file, journey);
		return this;
	}

	public void Dispose() => Directory.Delete(Path, recursive: true);
}

internal sealed record PageRequest(string UserAgent, string? MonitorHeader);

/// <summary>A site with the docs tags on <c>/docs</c>, a page with a form on <c>/page</c>, and a record of who asked.</summary>
internal sealed class FakeSite : IAsyncDisposable
{
	private readonly HttpListener _listener;
	private readonly Task _serving;
	private readonly string _docs;
	private readonly bool _servedFromRoot;
	private int _writes;

	public FakeSite(string? buildType, string? features, bool servedFromRoot = false, string bodyClass = "")
	{
		_servedFromRoot = servedFromRoot;
		(_listener, Url) = StartListener();
		var tags = new StringBuilder();
		if (buildType is not null)
			_ = tags.Append($"<meta name=\"docs:build-type\" content=\"{buildType}\">");
		if (features is not null)
			_ = tags.Append(
				features.Length == 0 ? "<meta name=\"docs:features\">" : $"<meta name=\"docs:features\" content=\"{features}\">"
			);
		_docs = $"<html><head><title>Docs</title>{tags}</head><body class=\"{bodyClass}\">Docs</body></html>";
		_serving = Task.Run(ServeAsync);
	}

	public string Url { get; }
	public List<PageRequest> PageRequests { get; } = [];

	/// <summary>The path of every GET the site answered, in order.</summary>
	public List<string> Paths { get; } = [];
	public int Writes => Volatile.Read(ref _writes);

	private async Task ServeAsync()
	{
		while (_listener.IsListening)
		{
			HttpListenerContext context;
			try
			{
				context = await _listener.GetContextAsync();
			}
			catch (Exception e) when (e is HttpListenerException or ObjectDisposedException or InvalidOperationException or ArgumentException)
			{
				return;
			}

			var request = context.Request;
			if (request.HttpMethod == "GET")
				lock (Paths)
					Paths.Add(request.Url!.AbsolutePath);
			if (request.HttpMethod == "POST")
				_ = Interlocked.Increment(ref _writes);
			else if (request.Url!.AbsolutePath == "/page")
				lock (PageRequests)
					PageRequests.Add(new PageRequest(request.UserAgent ?? "", request.Headers["X-Docs-Synthetic-Monitor"]));

			var html = request.Url!.AbsolutePath switch
			{
				"/docs" when _servedFromRoot => null,
				"/docs" or "/" when !_servedFromRoot || request.Url!.AbsolutePath == "/" => _docs,
				"/page" =>
					"<html><head><title>Page</title></head><body><form method=\"post\" action=\"/save\"><button type=\"submit\">Save</button></form></body></html>",
				"/save" => "<html><head><title>Saved</title></head><body>Saved</body></html>",
				_ => "<html><head><title>Not found</title></head></html>"
			};
			if (request.Url!.AbsolutePath == "/error")
			{
				context.Response.StatusCode = 500;
				context.Response.Close();
				continue;
			}
			if (html is null)
			{
				context.Response.StatusCode = 404;
				context.Response.Close();
				continue;
			}
			try
			{
				var bytes = Encoding.UTF8.GetBytes(html);
				context.Response.ContentType = "text/html; charset=utf-8";
				await context.Response.OutputStream.WriteAsync(bytes);
				context.Response.Close();
			}
			catch (Exception e) when (e is HttpListenerException or ObjectDisposedException or IOException) { }
		}
	}

	// Tests run at once, so a port that was free a moment ago can be taken: try another.
	private static (HttpListener Listener, string Url) StartListener()
	{
		for (var attempt = 0; ; attempt++)
		{
			using var probe = new TcpListener(IPAddress.Loopback, 0);
			probe.Start();
			var port = ((IPEndPoint)probe.LocalEndpoint).Port;
			probe.Stop();
			var listener = new HttpListener();
			listener.Prefixes.Add($"http://localhost:{port}/");
			try
			{
				listener.Start();
				return (listener, $"http://localhost:{port}");
			}
			catch (HttpListenerException) when (attempt < 10)
			{
				listener.Close();
			}
		}
	}

	public async ValueTask DisposeAsync()
	{
		try
		{
			// Close stops the listener; stopping first makes the second prefix removal re-bind a port that may be taken.
			_listener.Close();
		}
		catch (HttpListenerException) { }
		await _serving;
	}
}
