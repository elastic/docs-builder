// See https://aka.ms/new-console-template for more information

using System.Collections.Concurrent;
using System.Diagnostics;
using ConsoleAppFramework;
using Documentation.Assembler.Cli;
using Documentation.Builder;
using Elastic.Markdown.IO;
using ProcNet;
using ProcNet.Std;

var configFile = Path.Combine(Paths.Root.FullName, "src/docs-assembler/conf.yml");
var config = AssemblyConfiguration.Deserialize(File.ReadAllText(configFile));

var app = ConsoleApp.Create();
app.UseFilter<StopwatchFilter>();
app.UseFilter<CatchExceptionFilter>();

// would love to use libgit2 so there is no git dependency but
// libgit2 is magnitudes slower to clone repositories https://github.com/libgit2/libgit2/issues/4674
app.Add("clone-all", async Task (CancellationToken ctx) =>
{
	Console.WriteLine(config.Repositories.Count);
	var dict = new ConcurrentDictionary<string, Stopwatch>();
	await Parallel.ForEachAsync(config.Repositories, new ParallelOptions
	{
		CancellationToken = ctx,
		MaxDegreeOfParallelism = Environment.ProcessorCount / 4
	}, async (kv, c) =>
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

	foreach(var kv in dict.OrderBy(kv => kv.Value.Elapsed))
		Console.WriteLine($"-> {kv.Key}\ttook: {kv.Value.Elapsed}");
});
app.Add("list", async Task (CancellationToken ctx) =>{

	var assemblyPath = Path.Combine(Paths.Root.FullName, $".artifacts/assembly");
	var dir = new DirectoryInfo(assemblyPath);
	var dictionary = new Dictionary<string, string>();
	foreach (var d in dir.GetDirectories())
	{
		var checkoutFolder = Path.Combine(assemblyPath, d.Name);

		var consoleOut = new NoopConsoleLineHandler();
		var capture = Proc.StartRedirected(
			new StartArguments("git", "rev-parse", "--abbrev-ref", "HEAD")
			{
				WorkingDirectory = checkoutFolder
			}
			, consoleOut);
		dictionary.Add(d.Name, consoleOut.Lines.FirstOrDefault()?.Line ?? "unknown");
	}
	foreach(var kv in dictionary.OrderBy(kv => kv.Value))
		Console.WriteLine($"-> {kv.Key}\tbranch: {kv.Value}");

	await Task.CompletedTask;
});

await app.RunAsync(args);

public class ConsoleLineHandler(string prefix) : IConsoleLineHandler
{
	public void Handle(LineOut lineOut) => lineOut.CharsOrString(
		r => Console.Write(prefix + ": " + r),
		l => Console.WriteLine(prefix + ": " + l));

	public void Handle(Exception e) {}
}

public class NoopConsoleLineHandler : IConsoleLineHandler
{
	public List<LineOut> Lines { get; } = new();

	public void Handle(LineOut lineOut) => Lines.Add(lineOut);

	public void Handle(Exception e) {}
}
