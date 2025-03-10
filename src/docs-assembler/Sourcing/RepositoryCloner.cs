// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Diagnostics;
using Documentation.Assembler.Configuration;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;
using ProcNet;
using ProcNet.Std;

namespace Documentation.Assembler.Sourcing;

public class RepositoryCloner(ILoggerFactory logger, AssembleContext context)
{
	private readonly ILogger<RepositoryCloner> _logger = logger.CreateLogger<RepositoryCloner>();

	private AssemblyConfiguration Configuration => context.Configuration;

	public async Task CloneAll(Cancel ctx = default)
	{
		var dict = new ConcurrentDictionary<string, Stopwatch>();

		_logger.LogInformation("Cloning narrative content: {Repository}", NarrativeRepository.Name);
		CloneRepository(Configuration.Narrative, NarrativeRepository.Name, dict);

		_logger.LogInformation("Cloning {ReferenceRepositoryCount} repositories", Configuration.ReferenceRepositories.Count);
		await Parallel.ForEachAsync(Configuration.ReferenceRepositories,
			new ParallelOptions
			{
				CancellationToken = ctx,
				MaxDegreeOfParallelism = Environment.ProcessorCount
			}, async (kv, c) =>
			{
				await Task.Run(() =>
				{
					var name = kv.Key.Trim();
					CloneRepository(kv.Value, name, dict);
				}, c);
			}).ConfigureAwait(false);

		foreach (var kv in dict.OrderBy(kv => kv.Value.Elapsed))
			Console.WriteLine($"-> {kv.Key}\ttook: {kv.Value.Elapsed}");
	}

	private void CloneRepository(Repository? repository, string name, ConcurrentDictionary<string, Stopwatch> dict)
	{
		repository ??= new Repository();
		repository.CurrentBranch ??= "main";
		repository.Origin ??= $"git@github.com:elastic/{name}.git";

		var checkoutFolder = Path.Combine(context.OutputDirectory.FullName, name);
		var relativePath = Path.GetRelativePath(Paths.Root.FullName, checkoutFolder);
		var sw = Stopwatch.StartNew();
		_ = dict.AddOrUpdate(name, sw, (_, _) => sw);
		if (context.ReadFileSystem.Directory.Exists(checkoutFolder))
		{
			_logger.LogInformation("Pull: {Name}\t{Repository}\t{RelativePath}", name, repository, relativePath);
			// --allow-unrelated-histories due to shallow clones not finding a common ancestor
			ExecIn(checkoutFolder, "git", "pull", "--depth", "1", "--allow-unrelated-histories", "--no-ff");
		}
		else
		{
			_logger.LogInformation("Checkout: {Name}\t{Repository}\t{RelativePath}", name, repository, relativePath);
			if (repository.CheckoutStrategy == "full")
			{
				Exec("git", "clone", repository.Origin, checkoutFolder,
					"--depth", "1", "--single-branch",
					"--branch", repository.CurrentBranch
				);
			}
			else if (repository.CheckoutStrategy == "partial")
			{
				Exec(
					"git", "clone", "--filter=blob:none", "--no-checkout", repository.Origin, checkoutFolder
				);

				ExecIn(checkoutFolder, "git", "sparse-checkout", "set", "--cone");
				ExecIn(checkoutFolder, "git", "checkout", repository.CurrentBranch);
				ExecIn(checkoutFolder, "git", "sparse-checkout", "set", "docs");
			}
		}

		sw.Stop();

		void Exec(string binary, params string[] args) => ExecIn(null, binary, args);

		void ExecIn(string? workingDirectory, string binary, params string[] args)
		{
			var arguments = new StartArguments(binary, args)
			{
				WorkingDirectory = workingDirectory
			};
			var result = Proc.StartRedirected(arguments, new ConsoleLineHandler(_logger, name));
			if (result.ExitCode != 0)
				context.Collector.EmitError("", $"Exit code: {result.ExitCode} while executing {binary} {string.Join(" ", arguments)}");
		}
	}
}

public class ConsoleLineHandler(ILogger<RepositoryCloner> logger, string prefix) : IConsoleLineHandler
{
	public void Handle(LineOut lineOut) => lineOut.CharsOrString(
		r => Console.Write(prefix + ": " + r),
		l => logger.LogInformation("{RepositoryName}: {Message}", prefix, l)
	);

	public void Handle(Exception e) { }
}
