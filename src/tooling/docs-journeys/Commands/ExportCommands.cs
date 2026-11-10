// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Journeys.Cli.Synthetics;
using Nullean.Argh;

namespace Elastic.Documentation.Journeys.Cli.Commands;

internal sealed class ExportCommands
{
	/// <summary>
	/// Writes the Elastic Synthetics monitors for the journeys that apply to a site: the ones marked <c># @monitor</c>
	/// whose <c>@requires</c> the site meets, and that do not mutate. The site is read like <c>run</c> reads it, and the
	/// files are written by <c>@nullean/journeys</c> through <c>npx</c>. Run <c>push</c> afterwards to send them to Kibana.
	/// </summary>
	/// <param name="url">-u, The origin of the site the monitors check, such as <c>https://www.elastic.co</c>.</param>
	/// <param name="journeys">-j, The folder of journeys. Required.</param>
	/// <param name="@out">-o, Where the generated monitors and the project config go.</param>
	/// <param name="env">The environment's name (<c>prod</c>, <c>edge</c>, <c>demo</c>, <c>staging</c>). Defaults to the one the URL's host belongs to. Names the project.</param>
	/// <param name="buildType">The site's build type, for <c>build-type:</c> conditions. Defaults to the site's <c>docs:build-type</c> tag, or unknown.</param>
	/// <param name="feature">The feature flags that are on, comma separated. Defaults to the site's <c>docs:features</c> tag, or none.</param>
	/// <param name="probe">The page that is read for the site's <c>docs:*</c> tags. Default: /docs, falling back to / when it does not answer</param>
	/// <param name="paging">Turn <c># @alert pagerduty</c> into alert configuration. Only the prod environment may: edge and demo never page.</param>
	/// <param name="project">The Synthetics project id. Default: elastic-co-docs-&lt;env&gt;</param>
	/// <param name="kibanaUrl">The Kibana that holds the project.</param>
	/// <param name="space">The Kibana space. Default: default</param>
	/// <param name="allowEmpty">Do not fail when no journey is exported. A push of nothing deletes every monitor of the project.</param>
	/// <param name="dryRun">Print the context, the project config and the command, and write and run nothing.</param>
	public async Task<int> Synthetics(
		string url,
		string journeys,
		string @out,
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
		var context = new ExportContext(site.Env, site.BuildType, site.Features);
		var arguments = SyntheticsPlan.ExportArguments(journeys, @out, context, paging, baseUrl);
		var config = SyntheticsPlan.Config(projectId, kibanaUrl, space, site.Env, baseUrl);

		if (dryRun)
		{
			Console.Out.WriteLine($"project: {projectId}");
			Console.Out.WriteLine($"config: {SyntheticsPlan.ConfigPath(@out)}");
			Console.Out.Write(config);
			Console.Out.WriteLine($"package.json: {SyntheticsPlan.PackageJsonPath(@out)}");
			Console.Out.Write(SyntheticsPlan.PackageJson());
			Console.Out.WriteLine($"command: {Npx.Describe(arguments)}");
			Console.Out.WriteLine($"then, in {@out}: {Npx.Describe(SyntheticsPlan.InstallArguments, Npx.NpmCommand)}");
			return 0;
		}

		ClearJourneysFolder(journeys, @out);
		_ = Directory.CreateDirectory(@out);
		await File.WriteAllTextAsync(SyntheticsPlan.ConfigPath(@out), config, ct).ConfigureAwait(false);
		var exit = await Npx.RunAsync(arguments, ct).ConfigureAwait(false);
		if (exit != 0)
			return exit;

		await File.WriteAllTextAsync(SyntheticsPlan.PackageJsonPath(@out), SyntheticsPlan.PackageJson(), ct).ConfigureAwait(false);
		exit = await Npx.RunAsync(Npx.NpmCommand, SyntheticsPlan.InstallArguments, ct, Path.GetFullPath(@out)).ConfigureAwait(false);
		if (exit != 0)
			return exit;

		var written = MonitorFiles(@out);
		Console.Out.WriteLine($"{written} monitor file(s) in {SyntheticsPlan.JourneysFolder(@out)} for project {projectId}");
		if (written == 0 && !allowEmpty)
		{
			Console.Error.WriteLine(
				"No journey was exported. Pushing nothing would delete every monitor of the project, so this is an error (--allow-empty to accept it)."
			);
			return 1;
		}
		return 0;
	}

	// A reused output folder must not keep monitors of journeys that were deleted or no longer apply.
	private static void ClearJourneysFolder(string journeys, string outDir)
	{
		var folder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(SyntheticsPlan.JourneysFolder(outDir)));
		var source = Path.TrimEndingDirectorySeparator(Path.GetFullPath(journeys));
		// macOS and Windows file systems ignore case by default, so `Journeys` and `journeys` are one folder there.
		var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;
		if (string.Equals(source, folder, comparison) || source.StartsWith(folder + Path.DirectorySeparatorChar, comparison))
			throw new ArgumentException($"--out would overwrite the journeys folder {journeys}. Choose another output folder.");
		if (Directory.Exists(folder))
			Directory.Delete(folder, recursive: true);
	}

	internal static int MonitorFiles(string outDir)
	{
		var folder = SyntheticsPlan.JourneysFolder(outDir);
		return Directory.Exists(folder) ? Directory.EnumerateFiles(folder, "*.ts", SearchOption.AllDirectories).Count() : 0;
	}
}
