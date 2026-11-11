// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Journeys.Cli.Synthetics;
using Nullean.Argh;
using Nullean.Cdp.Journeys;
using Nullean.HttpFileParser;

namespace Elastic.Documentation.Journeys.Cli.Commands;

internal sealed class ExportCommands
{
	/// <summary>
	/// Writes the Elastic Synthetics monitors for the journeys that apply to a site: the ones marked <c># @monitor</c>
	/// whose <c>@requires</c> the site meets, and that do not mutate. The site is read like <c>run</c> reads it. The output is a
	/// project the caller validates and pushes with npm (<c>npm ci</c>, <c>elastic-synthetics --dry-run</c>, <c>@elastic/synthetics push</c>):
	/// this tool runs no Node itself.
	/// </summary>
	/// <param name="url">-u, The origin of the site the monitors check, such as <c>https://www.elastic.co</c>.</param>
	/// <param name="journeys">-j, A folder of journeys instead of the ones compiled into this binary.</param>
	/// <param name="@out">-o, Where the generated monitors and the project config go.</param>
	/// <param name="env">The environment's name (<c>prod</c>, <c>edge</c>, <c>demo</c>, <c>staging</c>). Defaults to the one the URL's host belongs to. Names the project.</param>
	/// <param name="buildType">The site's build type, for <c>build-type:</c> conditions. Defaults to the site's <c>docs:build-type</c> tag, or unknown.</param>
	/// <param name="feature">The feature flags that are on, comma separated. Defaults to the site's <c>docs:features</c> tag, or none.</param>
	/// <param name="probe">The page that is read for the site's <c>docs:*</c> tags. Default: /docs, falling back to / when it does not answer</param>
	/// <param name="paging">Turn <c># @alert pagerduty</c> into alert configuration. Only the prod environment may: edge and demo never page.</param>
	/// <param name="project">The Synthetics project id. Default: elastic-co-docs-&lt;env&gt;</param>
	/// <param name="kibanaUrl">The Kibana that holds the project.</param>
	/// <param name="space">The Kibana space. Default: default</param>
	/// <param name="allowEmpty">Do not fail when no journey is exported. Pushing nothing would delete every monitor of the project.</param>
	/// <param name="dryRun">Print the context, the project config, the files that would be written and the manifest, and write nothing.</param>
	public async Task<int> Synthetics(
		string url,
		string @out,
		string? journeys = null,
		string? env = null,
		string? buildType = null,
		string? feature = null,
		string probe = "/docs",
		bool paging = false,
		string? project = null,
		string kibanaUrl = SyntheticsPlan.DefaultKibanaUrl,
		string space = SyntheticsPlan.DefaultSpace,
		bool allowEmpty = false,
		bool dryRun = false,
		Cancel ct = default
	)
	{
		var baseUrl = Uri.TryCreate(url, UriKind.Absolute, out var parsed) && parsed.Scheme is "http" or "https"
			? parsed
			: throw new ArgumentException($"--url must be an http or https origin, not '{url}'.");
		journeys = JourneySource.Folder(journeys);
		if (!Directory.Exists(journeys))
			throw new DirectoryNotFoundException($"No journeys folder at {journeys}.");
		if (env is not null)
			SyntheticsPlan.RequireEnv(env);

		var site = await RunCommand.ResolveSiteAsync(
			baseUrl,
			new SiteContextOptions(env, buildType, feature is null ? null : SiteContext.Split(feature), probe),
			new RunIdentity(true, true, []),
			ct
		).ConfigureAwait(false);
		Console.Out.WriteLine($"context: {site.Describe}");

		if (site.Env is null)
			throw new ArgumentException($"The environment of {baseUrl.Host} is unknown, so the project cannot be named. Pass --env.");
		if (site.ProbeFailed)
			throw new InvalidOperationException(
				$"The site context of {baseUrl.Host} could not be read, so which monitors apply is unknown and a push could delete deployed ones. Retry, or pass --build-type and --feature."
			);
		if (paging && !SyntheticsPlan.MayPage(site.Env))
			throw new ArgumentException(
				$"--paging is refused for the {site.Env} environment: only prod pages. Edge and demo monitors never alert PagerDuty."
			);

		var projectId = project ?? SyntheticsPlan.DefaultProject(site.Env);
		SyntheticsPlan.RequireIdentifier(projectId, "project");
		var journeyBase = SiteContext.JourneyBase(baseUrl, site.BuildType);
		var context = new MonitorContext(site.Features, site.BuildType, site.Env, paging, journeyBase.ToString());
		var config = SyntheticsPlan.Config(projectId, kibanaUrl, space, site.Env, baseUrl);

		ExportResult result;
		var sources = MonitorExporter.ReadSources(journeys);
		try
		{
			result = MonitorExporter.Export(sources, context);
		}
		catch (Exception e) when (e is ExportException or JourneyParseException or HttpFileParseException)
		{
			await Console.Error.WriteLineAsync(e.Message.AsMemory(), ct).ConfigureAwait(false);
			return 1;
		}

		foreach (var skip in result.Skipped)
			Console.Out.WriteLine($"skipped {skip.Name}: {skip.Reason}");
		Console.Out.WriteLine($"exported {result.Files.Count} of {sources.Count} for project {projectId}");
		var manifest = SyntheticsPlan.Manifest(projectId, site.Env, result.Files);

		if (result.Files.Count == 0 && !allowEmpty)
		{
			await Console
				.Error
				.WriteLineAsync(
					"No journey was exported. Pushing nothing would delete every monitor of the project, so this is an error (--allow-empty to accept it).".AsMemory(),
					ct
				)
				.ConfigureAwait(false);
			return 1;
		}

		if (dryRun)
		{
			Console.Out.WriteLine($"project: {projectId}");
			Console.Out.WriteLine($"config: {SyntheticsPlan.ConfigPath(@out)}");
			Console.Out.Write(config);
			Console.Out.WriteLine($"package.json: {SyntheticsPlan.PackageJsonPath(@out)}");
			Console.Out.Write(SyntheticsPlan.PackageJson());
			Console.Out.WriteLine($"package-lock.json: {SyntheticsPlan.LockPath(@out)}");
			foreach (var file in result.Files)
				Console.Out.WriteLine($"monitor: {Path.Combine(SyntheticsPlan.JourneysFolder(@out), file.FileName)}");
			Console.Out.WriteLine($"manifest.json: {SyntheticsPlan.ManifestPath(@out)}");
			Console.Out.Write(manifest);
			return 0;
		}

		GuardOutputFolder(journeys, @out);
		RemoveStaleMonitors(@out, result.Files);
		_ = Directory.CreateDirectory(@out);
		foreach (var file in result.Files)
		{
			var target = Path.GetFullPath(Path.Combine(SyntheticsPlan.JourneysFolder(@out), file.FileName));
			var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(SyntheticsPlan.JourneysFolder(@out)))
				+ Path.DirectorySeparatorChar;
			if (!target.StartsWith(root, StringComparison.Ordinal))
			{
				await Console.Error.WriteLineAsync($"{file.FileName} would be written outside {root}".AsMemory(), ct).ConfigureAwait(false);
				return 1;
			}
			_ = Directory.CreateDirectory(Path.GetDirectoryName(target)!);
			await File.WriteAllTextAsync(target, file.Content, ct).ConfigureAwait(false);
		}
		await File.WriteAllTextAsync(SyntheticsPlan.ConfigPath(@out), config, ct).ConfigureAwait(false);
		await File.WriteAllTextAsync(SyntheticsPlan.PackageJsonPath(@out), SyntheticsPlan.PackageJson(), ct).ConfigureAwait(false);
		await File.WriteAllTextAsync(SyntheticsPlan.LockPath(@out), SyntheticsPlan.PackageLock(), ct).ConfigureAwait(false);
		await File.WriteAllTextAsync(SyntheticsPlan.ManifestPath(@out), manifest, ct).ConfigureAwait(false);
		Console.Out.WriteLine($"{result.Files.Count} monitor file(s) in {SyntheticsPlan.JourneysFolder(@out)}");
		return 0;
	}

	// The output must not be, or sit inside, the journeys it was made from: a re-export removes files it generated before.
	private static void GuardOutputFolder(string journeys, string outDir)
	{
		var folder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(SyntheticsPlan.JourneysFolder(outDir)));
		var source = Path.TrimEndingDirectorySeparator(Path.GetFullPath(journeys));
		// macOS and Windows file systems ignore case by default, so `Journeys` and `journeys` are one folder there.
		var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;
		if (string.Equals(source, folder, comparison) || source.StartsWith(folder + Path.DirectorySeparatorChar, comparison))
			throw new ArgumentException($"--out would overwrite the journeys folder {journeys}. Choose another output folder.");
	}

	// Only files that say they were generated are removed, so a re-export drops the monitors of journeys that were deleted or
	// no longer apply and touches nothing else in the folder.
	private static void RemoveStaleMonitors(string outDir, IReadOnlyList<ExportedFile> files)
	{
		var folder = SyntheticsPlan.JourneysFolder(outDir);
		if (!Directory.Exists(folder))
			return;
		var keep = files.Select(f => Path.GetFullPath(Path.Combine(folder, f.FileName))).ToHashSet(StringComparer.Ordinal);
		foreach (var existing in Directory.EnumerateFiles(folder, "*.journey.ts", SearchOption.AllDirectories))
		{
			if (!keep.Contains(Path.GetFullPath(existing)) && MonitorExporter.IsGenerated(File.ReadAllText(existing)))
				File.Delete(existing);
		}
	}
}
