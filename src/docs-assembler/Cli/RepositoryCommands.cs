// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Diagnostics;
using ConsoleAppFramework;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;
using ProcNet;
using ProcNet.Std;

namespace Documentation.Assembler.Cli;

public class ConsoleLineHandler(string prefix) : IConsoleLineHandler
{
	public void Handle(LineOut lineOut) => lineOut.CharsOrString(
		r => Console.Write(prefix + ": " + r),
		l => Console.WriteLine(prefix + ": " + l));

	public void Handle(Exception e) { }
}

internal class RepositoryCommands(ILoggerFactory logger)
{
	private void AssignOutputLogger()
	{
		var log = logger.CreateLogger<Program>();
#pragma warning disable CA2254
		ConsoleApp.Log = msg => log.LogInformation(msg);
		ConsoleApp.LogError = msg => log.LogError(msg);
#pragma warning restore CA2254
	}

	// would love to use libgit2 so there is no git dependency but
	// libgit2 is magnitudes slower to clone repositories https://github.com/libgit2/libgit2/issues/4674
	/// <summary> Clones all repositories </summary>
	/// <param name="ctx"></param>
	[Command("clone-all")]
	public async Task CloneAll(Cancel ctx = default)
	{
		AssignOutputLogger();
		var configFile = Path.Combine(Paths.Root.FullName, "src/docs-assembler/conf.yml");
		var config = AssemblyConfiguration.Deserialize(File.ReadAllText(configFile));

		Console.WriteLine(config.Repositories.Count);
		var dict = new ConcurrentDictionary<string, Stopwatch>();
		await Parallel.ForEachAsync(config.Repositories,
			new ParallelOptions { CancellationToken = ctx, MaxDegreeOfParallelism = Environment.ProcessorCount / 4 }, async (kv, c) =>
			{
				await Task.Run(() =>
				{
					var name = kv.Key;
					var repository = kv.Value;
					var checkoutFolder = Path.Combine(Paths.Root.FullName, $".artifacts/assembly/{name}");

					var sw = Stopwatch.StartNew();
					dict.AddOrUpdate(name, sw, (_, _) => sw);
					Console.WriteLine($"Checkout: {name}\t{repository}\t{checkoutFolder}");
					var branch = repository.Branch ?? "main";
					var args = new StartArguments(
						"git", "clone", repository.Origin, checkoutFolder, "--depth", "1"
						, "--single-branch", "--branch", branch
					);
					Proc.StartRedirected(args, new ConsoleLineHandler(name));
					sw.Stop();
				}, c);
			}).ConfigureAwait(false);

		foreach (var kv in dict.OrderBy(kv => kv.Value.Elapsed))
			Console.WriteLine($"-> {kv.Key}\ttook: {kv.Value.Elapsed}");
	}

	/// <summary> List all checked out repositories </summary>
	/// <param name="ctx"></param>
	[Command("list")]
	public async Task ListRepositories(Cancel ctx = default)
	{
		AssignOutputLogger();
		var assemblyPath = Path.Combine(Paths.Root.FullName, $".artifacts/assembly");
		var dir = new DirectoryInfo(assemblyPath);
		var dictionary = new Dictionary<string, string>();
		foreach (var d in dir.GetDirectories())
		{
			var checkoutFolder = Path.Combine(assemblyPath, d.Name);

			var capture = Proc.Start(
				new StartArguments("git", "rev-parse", "--abbrev-ref", "HEAD") { WorkingDirectory = checkoutFolder }
			);
			dictionary.Add(d.Name, capture.ConsoleOut.FirstOrDefault()?.Line ?? "unknown");
		}

		foreach (var kv in dictionary.OrderBy(kv => kv.Value))
			Console.WriteLine($"-> {kv.Key}\tbranch: {kv.Value}");

		await Task.CompletedTask;
	}
}
