// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Abstractions;
using Documentation.Assembler.Configuration;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;
using ProcNet;
using ProcNet.Std;

namespace Documentation.Assembler.Sourcing;

public class RepositoryCheckoutProvider(ILoggerFactory logger, AssembleContext context)
{
	private readonly ILogger<RepositoryCheckoutProvider> _logger = logger.CreateLogger<RepositoryCheckoutProvider>();

	private AssemblyConfiguration Configuration => context.Configuration;

	public IReadOnlyCollection<Checkout> GetAll()
	{
		var fs = context.ReadFileSystem;
		var repositories = Configuration.ReferenceRepositories.Values.Concat<Repository>([Configuration.Narrative]);
		var checkouts = new List<Checkout>();
		foreach (var repo in repositories)
		{
			var checkoutFolder = fs.DirectoryInfo.New(Path.Combine(context.CheckoutDirectory.FullName, repo.Name));
			var head = Capture(checkoutFolder, "git", "rev-parse", "HEAD");
			var checkout = new Checkout
			{
				Repository = repo,
				Directory = checkoutFolder,
				HeadReference = head
			};
			checkouts.Add(checkout);
		}
		return checkouts;
	}

	public async Task<IReadOnlyCollection<Checkout>> AcquireAllLatest(Cancel ctx = default)
	{
		var dict = new ConcurrentDictionary<string, Stopwatch>();
		var checkouts = new ConcurrentBag<Checkout>();

		if (context.OutputDirectory.Exists)
		{
			_logger.LogInformation("Cleaning output directory: {OutputDirectory}", context.OutputDirectory.FullName);
			context.OutputDirectory.Delete(true);
		}


		_logger.LogInformation("Cloning narrative content: {Repository}", NarrativeRepository.RepositoryName);
		var checkout = CloneOrUpdateRepository(Configuration.Narrative, NarrativeRepository.RepositoryName, dict);
		checkouts.Add(checkout);

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
					var clone = CloneOrUpdateRepository(kv.Value, name, dict);
					checkouts.Add(clone);
				}, c);
			}).ConfigureAwait(false);

		foreach (var kv in dict.OrderBy(kv => kv.Value.Elapsed))
			_logger.LogInformation("-> {Repository}\ttook: {Elapsed}", kv.Key, kv.Value.Elapsed);

		return checkouts.ToList().AsReadOnly();
	}

	private Checkout CloneOrUpdateRepository(Repository repository, string name, ConcurrentDictionary<string, Stopwatch> dict)
	{
		var fs = context.ReadFileSystem;
		var checkoutFolder = fs.DirectoryInfo.New(Path.Combine(context.CheckoutDirectory.FullName, name));
		var relativePath = Path.GetRelativePath(Paths.Root.FullName, checkoutFolder.FullName);
		var sw = Stopwatch.StartNew();
		_ = dict.AddOrUpdate(name, sw, (_, _) => sw);
		var head = string.Empty;
		if (checkoutFolder.Exists)
		{
			_logger.LogInformation("Pull: {Name}\t{Repository}\t{RelativePath}", name, repository, relativePath);
			// --allow-unrelated-histories due to shallow clones not finding a common ancestor
			ExecIn(checkoutFolder, "git", "pull", "--depth", "1", "--allow-unrelated-histories", "--no-ff");
			head = Capture(checkoutFolder, "git", "rev-parse", "HEAD");
		}
		else
		{
			_logger.LogInformation("Checkout: {Name}\t{Repository}\t{RelativePath}", name, repository, relativePath);
			if (repository.CheckoutStrategy == "full")
			{
				Exec("git", "clone", repository.Origin, checkoutFolder.FullName,
					"--depth", "1", "--single-branch",
					"--branch", repository.CurrentBranch
				);
			}
			else if (repository.CheckoutStrategy == "partial")
			{
				Exec(
					"git", "clone", "--filter=blob:none", "--no-checkout", repository.Origin, checkoutFolder.FullName
				);

				ExecIn(checkoutFolder, "git", "sparse-checkout", "set", "--cone");
				ExecIn(checkoutFolder, "git", "checkout", repository.CurrentBranch);
				ExecIn(checkoutFolder, "git", "sparse-checkout", "set", "docs");
				head = Capture(checkoutFolder, "git", "rev-parse", "HEAD");
			}
		}

		sw.Stop();

		return new Checkout
		{
			Repository = repository,
			Directory = checkoutFolder,
			HeadReference = head
		};
	}

	private void Exec(string binary, params string[] args) => ExecIn(null, binary, args);

	private void ExecIn(IDirectoryInfo? workingDirectory, string binary, params string[] args)
	{
		var arguments = new ExecArguments(binary, args)
		{
			WorkingDirectory = workingDirectory?.FullName
		};
		var result = Proc.Exec(arguments);
		if (result != 0)
			context.Collector.EmitError("", $"Exit code: {result} while executing {binary} {string.Join(" ", args)} in {workingDirectory}");
	}

	private string Capture(IDirectoryInfo? workingDirectory, string binary, params string[] args)
	{
		var arguments = new StartArguments(binary, args)
		{
			WorkingDirectory = workingDirectory?.FullName,
			//WaitForStreamReadersTimeout = TimeSpan.FromSeconds(3),
			Timeout = TimeSpan.FromSeconds(3),
			WaitForExit = TimeSpan.FromSeconds(3),
			ConsoleOutWriter = NoopConsoleWriter.Instance
		};
		var result = Proc.Start(arguments);
		if (result.ExitCode != 0)
			context.Collector.EmitError("", $"Exit code: {result.ExitCode} while executing {binary} {string.Join(" ", args)} in {workingDirectory}");
		var line = result.ConsoleOut.FirstOrDefault()?.Line ?? throw new Exception($"No output captured for {binary}: {workingDirectory}");
		return line;
	}
}

public class ConsoleLineHandler(ILogger<RepositoryCheckoutProvider> logger, string prefix) : IConsoleLineHandler
{
	public void Handle(LineOut lineOut) => lineOut.CharsOrString(
		r => Console.Write(prefix + ": " + r),
		l => logger.LogInformation("{RepositoryName}: {Message}", prefix, l)
	);

	public void Handle(Exception e) { }
}

public class NoopConsoleWriter : IConsoleOutWriter
{
	public static readonly NoopConsoleWriter Instance = new();

	public void Write(Exception e) { }

	public void Write(ConsoleOut consoleOut) { }
}
