// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Journeys.Cli.Synthetics;
using Nullean.Argh;

namespace Elastic.Documentation.Journeys.Cli.Commands;

internal sealed class PushCommand
{
	/// <summary>
	/// Sends the monitors that <c>export synthetics</c> wrote to Kibana through <c>@elastic/synthetics</c>. A push replaces
	/// the project's whole monitor set, so a journey that is no longer exported loses its monitor. The API key is read from
	/// the <c>SYNTHETICS_API_KEY</c> environment variable and nowhere else.
	/// </summary>
	/// <param name="@out">-o, The folder <c>export synthetics</c> wrote.</param>
	/// <param name="env">The environment's name; it names the project when <c>--project</c> is not given.</param>
	/// <param name="project">The Synthetics project id. Default: elastic-co-docs-&lt;env&gt;</param>
	/// <param name="dryRun">Print what would be pushed and call nothing. Needs no API key.</param>
	public async Task<int> Push(string @out, string env, string? project = null, bool dryRun = false, Cancel ct = default)
	{
		SyntheticsPlan.RequireEnv(env);

		if (!File.Exists(SyntheticsPlan.ConfigPath(@out)))
			throw new FileNotFoundException(
				$"{SyntheticsPlan.ConfigPath(@out)} does not exist. Run `docs-journeys export synthetics` first."
			);
		var (exportedProject, exportedEnv) = SyntheticsPlan.ReadExported(SyntheticsPlan.ConfigPath(@out));
		var projectId = project ?? exportedProject ?? SyntheticsPlan.DefaultProject(env);
		SyntheticsPlan.RequireIdentifier(projectId, "project");
		if (exportedEnv is not null && exportedEnv != env)
			throw new ArgumentException(
				$"{@out} was exported for the {exportedEnv} environment, not {env}. Pushing it would replace another environment's monitors."
			);
		if (exportedProject is not null && exportedProject != projectId)
			throw new ArgumentException(
				$"{@out} was exported for project {exportedProject}, not {projectId}. Pushing it would replace another project's monitors."
			);
		var monitors = ExportCommands.MonitorFiles(@out);
		if (monitors == 0)
		{
			Console.Error.WriteLine(
				$"{SyntheticsPlan.JourneysFolder(@out)} holds no monitors. Pushing nothing would delete every monitor of {projectId}."
			);
			return 1;
		}

		if (!Directory.Exists(Path.Combine(@out, "node_modules")))
			throw new DirectoryNotFoundException(
				$"{Path.Combine(@out, "node_modules")} does not exist. `docs-journeys export synthetics` installs it; run that first."
			);

		var arguments = SyntheticsPlan.PushArguments(projectId);
		var keySet = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(SyntheticsPlan.ApiKeyVariable));
		if (dryRun)
		{
			Console.Out.WriteLine($"would push {monitors} monitor(s) to project {projectId} ({env})");
			Console.Out.WriteLine($"in: {Path.GetFullPath(@out)}");
			Console.Out.WriteLine($"command: {Npx.Describe(arguments)}");
			Console.Out.WriteLine($"{SyntheticsPlan.ApiKeyVariable}: {(keySet ? "set" : "not set")}");
			return 0;
		}

		if (!keySet)
		{
			Console.Error.WriteLine($"{SyntheticsPlan.ApiKeyVariable} is not set. Nothing was pushed.");
			return 1;
		}

		Console.Out.WriteLine($"pushing {monitors} monitor(s) to project {projectId} ({env})");
		return await Npx.RunAsync(arguments, ct, Path.GetFullPath(@out)).ConfigureAwait(false);
	}
}
