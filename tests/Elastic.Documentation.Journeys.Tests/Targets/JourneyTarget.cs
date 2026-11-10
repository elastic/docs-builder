// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Nullean.Snapshots.Browser;
using TUnit.Core.Interfaces;

namespace Elastic.Documentation.Journeys.Tests.Targets;

/// <summary>
/// The site the journeys run against, started once for the whole test session. <c>JOURNEY_TARGET</c> picks it:
/// <c>isolated</c> (the default) serves this repository's <c>docs/</c> folder, <c>assembler</c> and <c>codex</c> serve
/// the output of an earlier build in <c>.artifacts/</c>.
/// </summary>
public sealed partial class JourneyTarget : IAsyncInitializer, IAsyncDisposable
{
	private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(120);

	private Process? _host;

	public string Name { get; private set; } = "isolated";
	public Uri BaseUrl { get; private set; } = new("http://localhost/");

	/// <summary>The feature flags the site reports in <c>docs:features</c>; empty for a site that predates the tag.</summary>
	public IReadOnlySet<string> Features { get; private set; } = new HashSet<string>();

	/// <summary>The build type the site reports: <c>isolated</c>, <c>assembler</c> or <c>codex</c>.</summary>
	public string BuildType { get; private set; } = "isolated";

	public JourneySnapshotOptions Options => new() { BaseUrl = BaseUrl };

	/// <summary>
	/// The journeys open <c>/syntax/...</c> and assert the isolated header and titles, which only the isolated site has
	/// (assembler content lives under <c>/docs/...</c>, Codex under <c>/r/&lt;repo&gt;/...</c>). Other targets skip them
	/// until they get journeys of their own.
	/// </summary>
	public Task<JourneySnapshotOptions> OptionsForIsolatedJourneysAsync(string? variant = null)
	{
		if (Name != "isolated")
			Skip.Test($"These journeys target the isolated site; none are written for '{Name}' yet.");
		return Task.FromResult(new JourneySnapshotOptions { BaseUrl = BaseUrl, Variant = variant });
	}

	public async Task InitializeAsync()
	{
		Name = Environment.GetEnvironmentVariable("JOURNEY_TARGET")?.Trim().ToLowerInvariant() is { Length: > 0 } name ? name : "isolated";
		var root = FindRepositoryRoot();
		var port = FreePort();
		var (workingDirectory, arguments) = Name switch
		{
			"isolated" => (Path.Combine(root, "docs"), new[] { "serve", "--port", $"{port}", "--no-hud" }),
			"assembler" =>
				(root, new[]
				{
					"assembler",
					"serve",
					"--port",
					$"{port}",
					"--path",
					RequireFolder(root, ".artifacts/assembly", "assembler build")
				}),
			"codex" =>
				(root, new[]
				{
					"codex",
					"serve",
					"--port",
					$"{port}",
					"--path",
					RequireFolder(root, ".artifacts/codex/docs", "codex build")
				}),
			_ => throw new InvalidOperationException($"JOURNEY_TARGET '{Name}' is not one of isolated, assembler, codex.")
		};

		BaseUrl = new Uri($"http://localhost:{port}/");
		_host = StartHost(FindDocsBuilderDll(root), workingDirectory, arguments);
		var landingPage = await WaitUntilReadyAsync().ConfigureAwait(false);
		(Features, BuildType) = ReadSiteMetadata(landingPage);
	}

	public ValueTask DisposeAsync()
	{
		if (_host is { HasExited: false })
		{
			_host.Kill(entireProcessTree: true);
			_host.WaitForExit(TimeSpan.FromSeconds(10));
		}
		_host?.Dispose();
		return ValueTask.CompletedTask;
	}

	private static Process StartHost(string dll, string workingDirectory, string[] arguments)
	{
		var info = new ProcessStartInfo("dotnet")
		{
			WorkingDirectory = workingDirectory,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};
		info.ArgumentList.Add(dll);
		foreach (var argument in arguments)
			info.ArgumentList.Add(argument);

		// A flag exported in the developer's shell must not change what the journeys see.
		foreach (var key in info.Environment.Keys.Where(k => k.StartsWith("FEATURE_", StringComparison.OrdinalIgnoreCase)).ToList())
			_ = info.Environment.Remove(key);

		var process = Process.Start(info) ?? throw new InvalidOperationException("docs-builder did not start.");
		// Unread pipes fill up and stall the host.
		process.OutputDataReceived += (_, _) => { };
		process.ErrorDataReceived += (_, _) => { };
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		return process;
	}

	private async Task<string> WaitUntilReadyAsync()
	{
		using var http = new HttpClient();
		var deadline = DateTime.UtcNow + StartupTimeout;
		while (DateTime.UtcNow < deadline)
		{
			if (_host is { HasExited: true })
				throw new InvalidOperationException($"docs-builder exited with code {_host.ExitCode} before it served {BaseUrl}.");
			try
			{
				using var response = await http.GetAsync(BaseUrl).ConfigureAwait(false);
				if (response.StatusCode == HttpStatusCode.OK)
					return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			}
			catch (HttpRequestException)
			{
				// Not listening yet.
			}
			await Task.Delay(500).ConfigureAwait(false);
		}
		throw new TimeoutException($"{BaseUrl} did not answer within {StartupTimeout.TotalSeconds:0} s.");
	}

	// An empty docs:features renders as <meta name="docs:features"> with no content attribute.
	private static (IReadOnlySet<string> Features, string BuildType) ReadSiteMetadata(string html)
	{
		var features = FeaturesMeta().Match(html) is { Success: true } f
			? f.Groups["content"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal)
			: [];
		var buildType = BuildTypeMeta().Match(html) is { Success: true } b
			? b.Groups["content"].Value
			: BuildTypeClass().Match(html) is { Success: true } c ? c.Groups["type"].Value : "isolated";
		return (features, buildType);
	}

	private static string RequireFolder(string root, string relative, string command)
	{
		var path = Path.Combine(root, relative);
		return Directory.Exists(path)
			? path
			: throw new DirectoryNotFoundException($"{path} does not exist. Build it first: docs-builder {command}.");
	}

	private static string FindDocsBuilderDll(string root)
	{
		var configuration = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)).Name.ToLowerInvariant();
		string[] candidates = [configuration, "release", "debug"];
		foreach (var candidate in candidates)
		{
			var dll = Path.Combine(root, ".artifacts", "bin", "docs-builder", candidate, "docs-builder.dll");
			if (File.Exists(dll))
				return dll;
		}
		throw new FileNotFoundException(
			$"docs-builder.dll not found under {Path.Combine(root, ".artifacts/bin/docs-builder")}. Build src/tooling/docs-builder first."
		);
	}

	private static string FindRepositoryRoot()
	{
		for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
		{
			if (File.Exists(Path.Combine(directory.FullName, "docs-builder.slnx")))
				return directory.FullName;
		}
		throw new DirectoryNotFoundException("The repository root (docs-builder.slnx) is not above the test output.");
	}

	private static int FreePort()
	{
		using var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		return ((IPEndPoint)listener.LocalEndpoint).Port;
	}

	[GeneratedRegex("""<meta name="docs:features"(?: content="(?<content>[^"]*)")?""")]
	private static partial Regex FeaturesMeta();

	[GeneratedRegex("""<meta name="docs:build-type" content="(?<content>[^"]*)""")]
	private static partial Regex BuildTypeMeta();

	[GeneratedRegex("""build-type-(?<type>isolated|assembler|codex)""")]
	private static partial Regex BuildTypeClass();
}
