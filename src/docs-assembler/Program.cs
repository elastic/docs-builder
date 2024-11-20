// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using ConsoleAppFramework;
using Documentation.Assembler.Cli;
using Documentation.Builder;
using Elastic.Markdown.IO;
using ProcNet;
using ProcNet.Std;
using static XenoAtom.Interop.libgit2;

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
			Console.WriteLine($"Checkout: {name}\t{repository}\t{checkoutFolder}");
			//, "--single-branch", "--branch", "main"
			var args = new StartArguments("git", "clone", repository, checkoutFolder, "--depth", "1");
			Proc.StartRedirected(args, new ConsoleLineHandler(name));
			sw.Stop();
			Console.WriteLine($"-> {name}\ttook: {sw.Elapsed}");
		}, c);
	}).ConfigureAwait(false);
});

app.Add("", async Task (CancellationToken ctx) =>
{
	Console.WriteLine(config.Repositories.Count);

	var ret = git_libgit2_init();
	ret.Check();

	await Parallel.ForEachAsync(config.Repositories, new ParallelOptions
	{
		CancellationToken = ctx,
		MaxDegreeOfParallelism = Environment.ProcessorCount
	}, async (kv, c) =>
	{
		if (kv.Key != "elasticsearch")
			return;
		await Task.Run(() =>
		{
			var name = kv.Key;
			var repository = kv.Value;
			var checkoutFolder = Path.Combine(Paths.Root.FullName, $".artifacts/assembly/{name}");

			git_clone_options_init(out var options, GIT_CLONE_OPTIONS_VERSION);
			git_fetch_options_init(out var fetchOptions, GIT_FETCH_OPTIONS_VERSION);
			fetchOptions.depth = 1;
			options.fetch_opts = fetchOptions;
			var sw = Stopwatch.StartNew();
			Console.WriteLine($"Checkout: {name}\t{checkoutFolder}\t{repository}");
			git_clone(out var r, repository, checkoutFolder, in options);
			sw.Stop();
			Console.WriteLine($"-> {name}\ttook: {sw.Elapsed}");
		}, c);
	}).ConfigureAwait(false);
});
await app.RunAsync(args);

public class ConsoleLineHandler(string prefix) : IConsoleLineHandler
{
	public void Handle(LineOut lineOut) => lineOut.CharsOrString(
		r => Console.Write(prefix + ": " + r),
		l => Console.WriteLine(prefix + ": " + l));

	public void Handle(Exception e) {}
}

