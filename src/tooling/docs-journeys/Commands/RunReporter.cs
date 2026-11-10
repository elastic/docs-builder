// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text;
using Nullean.Cdp.Journeys;

namespace Elastic.Documentation.Journeys.Cli.Commands;

/// <summary>Shows a run while it happens: which journeys started, where they are, and how each one ended.</summary>
internal interface IRunReporter
{
	void Planned(IReadOnlyList<(JourneyFile File, string Reason)> skipped, int toRun);
	void Started(JourneyFile file);
	void Step(JourneyFile file, JourneyStep step);
	void Finished(JourneyOutcome outcome);
}

/// <summary>
/// Writes one line per journey as it finishes. On a terminal it also keeps a status line of the running journeys at the
/// bottom, redrawn in place, and draws failures with the journey's source around the failing line; elsewhere (CI, a pipe)
/// it writes plain lines, and a failure in the shape <c>Nullean.Snapshots</c> gives it under TUnit.
/// </summary>
internal sealed class ConsoleRunReporter(TextWriter output, bool interactive, bool verbose, Uri baseUrl) : IRunReporter
{
	/// <summary>A reason shared by more journeys than this is shown as a count instead of a line per journey.</summary>
	internal const int CollapseAbove = 3;

	private const int Label = 11;
	private static readonly TimeSpan StatusInterval = TimeSpan.FromMilliseconds(100);

	private readonly Lock _gate = new();
	private readonly Dictionary<string, string> _running = [];
	private readonly Dictionary<string, Uri> _pages = [];
	private readonly Stopwatch _sinceDraw = Stopwatch.StartNew();
	private int _finished;
	private int _total;
	private int _statusWidth;
	private bool _stale;

	public static bool UseInteractive => !Console.IsOutputRedirected && Environment.GetEnvironmentVariable("CI") is not { Length: > 0 };

	private static int Width => Console.WindowWidth > 20 ? Math.Min(Console.WindowWidth, 110) : 100;

	public void Planned(IReadOnlyList<(JourneyFile File, string Reason)> skipped, int toRun)
	{
		lock (_gate)
		{
			_total = toRun;
			var lines = SkipLines(skipped, verbose);
			if (interactive && lines.Count > 0)
				output.WriteLine();
			foreach (var line in lines)
				output.WriteLine(interactive ? Ansi.Blue($"{Ansi.Skip} {line}") : line);
			if (interactive && lines.Count > 0)
				output.WriteLine();
		}
	}

	public void Started(JourneyFile file)
	{
		lock (_gate)
		{
			_running[file.Id] = "starting";
			DrawStatus(force: true);
		}
	}

	public void Step(JourneyFile file, JourneyStep step)
	{
		lock (_gate)
		{
			_running[file.Id] = $"line {step.Line}";
			if (step.Arguments is ["open", var target, ..] && Uri.TryCreate(baseUrl, target, out var page))
				_pages[file.Id] = page;
			DrawStatus(force: false);
		}
	}

	public void Finished(JourneyOutcome outcome)
	{
		lock (_gate)
		{
			_ = _running.Remove(outcome.File.Id);
			_finished++;
			ClearStatus();
			output.WriteLine(Colour(outcome));
			DrawStatus(force: true);
		}
	}

	/// <summary>The tool's version, or empty while builds are unstamped: the SDK's default <c>1.0.0</c> says nothing about the build.</summary>
	private static string ToolVersion =>
		typeof(ConsoleRunReporter)
			.Assembly
			.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), inherit: false)
			.OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
			.FirstOrDefault()?.InformationalVersion.Split('+')[0]
			?? "dev";

	/// <summary>A lead-in, then the site and what the run decided about it, one fact per line.</summary>
	public static string ContextBlock(SiteContext site, Uri url, string? env, string? buildType, string? feature)
	{
		var text = new StringBuilder();
		_ = text.AppendLine($"{Ansi.Bold($"docs-journeys {ToolVersion}".TrimEnd())} running against:").AppendLine();

		void Row(string label, string value, string? note = null) =>
			text.AppendLine($"  {label.PadRight(Label)}{Ansi.Cyan(value)}{(note is null ? "" : $" {Ansi.Dim($"({note})")}")}");

		Row("Site", url.ToString());
		Row("Env", site.Env ?? "unknown", env is null ? null : "--env");
		Row("Build", site.BuildType ?? "unknown", buildType is null ? null : "--build-type");
		Row(
			"Features",
			site.Features.Count == 0 ? "none" : string.Join(' ', site.Features.Order(StringComparer.Ordinal)),
			feature is null ? null : "--feature"
		);
		foreach (var note in site.ProbeNotes)
			_ = text.AppendLine($"  {Ansi.Yellow("Note".PadRight(Label))}{note}");
		return text.ToString().TrimEnd();
	}

	/// <summary>The lines that say which journeys were skipped and why, collapsing a reason that many journeys share.</summary>
	internal static IReadOnlyList<string> SkipLines(IReadOnlyList<(JourneyFile File, string Reason)> skipped, bool listAll)
	{
		var lines = new List<string>();
		foreach (var group in skipped.GroupBy(s => s.Reason).OrderBy(g => g.Key, StringComparer.Ordinal))
		{
			var files = group.Select(g => g.File).OrderBy(f => f.Id, StringComparer.Ordinal).ToList();
			if (listAll || files.Count <= CollapseAbove)
				lines.AddRange(files.Select(f => $"skipped {f.Id} ({group.Key})"));
			else
				lines.Add($"skipped {files.Count} journeys ({group.Key}); --verbose lists them");
		}
		return lines;
	}

	public static string Line(JourneyOutcome outcome) =>
		outcome.Skipped
			? $"skipped {outcome.File.Id} ({outcome.SkipReason})"
			: outcome.Passed ? $"ok      {outcome.File.Id} ({outcome.Elapsed.TotalSeconds:0.0}s)" : $"FAILED  {outcome.File.Id}";

	/// <summary>What failed, then the totals. This is the last thing a run writes.</summary>
	public string Summary(IReadOnlyList<JourneyOutcome> outcomes, TimeSpan elapsed, string journeysRoot, string rerun)
	{
		var text = new StringBuilder();
		foreach (var outcome in outcomes.Where(o => o.Failed))
		{
			var command = rerun.Replace("{id}", outcome.File.Id, StringComparison.Ordinal);
			_ = text.AppendLine().Append(
				interactive ? PrettyFailure(outcome, journeysRoot, command) : FailureBlock(outcome, journeysRoot, command)
			);
		}
		var passed = outcomes.Count(o => o.Passed);
		var failed = outcomes.Count(o => o.Failed);
		var skipped = outcomes.Count(o => o.Skipped);
		return text
			.AppendLine()
			.Append(interactive ? PrettyTotals(passed, failed, skipped, elapsed) : PlainTotals(passed, failed, skipped, elapsed))
			.ToString();
	}

	private string PlainTotals(int passed, int failed, int skipped, TimeSpan elapsed) =>
		$"{passed} passed, {failed} failed, {skipped} skipped in {elapsed.TotalSeconds:0.0}s, against {baseUrl}";

	private string PrettyTotals(int passed, int failed, int skipped, TimeSpan elapsed)
	{
		var counts = new List<string>
		{
			passed > 0 ? Ansi.Green($"{Ansi.Ok} {passed} passed") : $"{Ansi.Ok} {passed} passed",
			failed > 0 ? Ansi.BoldRed($"{Ansi.Fail} {failed} failed") : $"{Ansi.Fail} {failed} failed",
			skipped > 0 ? Ansi.Blue($"{Ansi.Skip} {skipped} skipped") : $"{Ansi.Skip} {skipped} skipped"
		};
		return $"{string.Join("   ", counts)}\n\n{Ansi.Dim($"{elapsed.TotalSeconds:0.0}s against")} {Ansi.Cyan(baseUrl.ToString())}";
	}

	/// <summary>
	/// A failure in the shape Nullean.Snapshots gives it under TUnit and in <c>snap journey run</c>: the step it stopped at, the
	/// error as written, then labelled lines. The message is shown as it came, never reflowed or cut.
	/// </summary>
	internal string FailureBlock(JourneyOutcome outcome, string journeysRoot, string rerun)
	{
		var where = outcome.File.DisplayPath(journeysRoot);
		var text = new StringBuilder();
		if (outcome.Error is { } error)
			_ = text.AppendLine($"Journey did not run: {where}").AppendLine(error.TrimEnd());
		else
		{
			var failure = outcome.Result!.Failure!;
			_ = text.AppendLine($"Journey stopped at {failure.Describe(where)}");
			if (_pages.TryGetValue(outcome.File.Id, out var page))
				_ = text.AppendLine("Last open:".PadRight(Label) + page);
		}
		return text.AppendLine("Rerun:".PadRight(Label) + rerun).ToString();
	}

	/// <summary>A failure for a person: the journey's lines around the one that failed, the error wrapped, the page and the rerun command.</summary>
	internal string PrettyFailure(JourneyOutcome outcome, string journeysRoot, string rerun)
	{
		var where = outcome.File.DisplayPath(journeysRoot);
		var text = new StringBuilder();
		var failure = outcome.Result?.Failure;
		_ = text.AppendLine(Ansi.BoldRed($"{Ansi.Fail} {outcome.File.Id}"));
		_ = text.AppendLine($"  {Ansi.Dim($"{where}{(failure is null ? "" : $":{failure.Line}")}")}");
		if (failure is not null)
			AppendSource(text, outcome.File.Path, failure.Line);
		_ = text.AppendLine();
		foreach (var line in Wrap(outcome.Error ?? failure!.Message, Width - 4))
			_ = text.AppendLine($"  {line}");
		_ = text.AppendLine();
		if (_pages.TryGetValue(outcome.File.Id, out var page))
			_ = text.AppendLine($"  {Ansi.Dim("last open".PadRight(10))}{Ansi.Cyan(page.ToString())}");
		_ = text.AppendLine($"  {Ansi.Dim("rerun".PadRight(10))}{rerun}");
		return text.ToString();
	}

	private static void AppendSource(StringBuilder text, string path, int failedLine)
	{
		string[] lines;
		try
		{
			lines = File.ReadAllLines(path);
		}
		catch (IOException)
		{
			return;
		}
		_ = text.AppendLine();
		var last = Math.Min(lines.Length, failedLine + 1);
		for (var number = Math.Max(1, failedLine - 2); number <= last; number++)
			_ = number == failedLine
				? text.AppendLine($"  {Ansi.BoldRed($"{Ansi.Pointer} {number,3} {Ansi.Bar} {lines[number - 1]}")}")
				: text.AppendLine($"  {Ansi.Dim($"  {number,3} {Ansi.Bar} {lines[number - 1]}")}");
	}

	/// <summary>Breaks <paramref name="text"/> at spaces to fit <paramref name="width"/>; a word longer than that stays whole.</summary>
	internal static IEnumerable<string> Wrap(string text, int width)
	{
		foreach (var paragraph in text.ReplaceLineEndings("\n").TrimEnd().Split('\n'))
		{
			var line = new StringBuilder();
			foreach (var word in paragraph.Split(' '))
			{
				if (line.Length > 0 && line.Length + 1 + word.Length > width)
				{
					yield return line.ToString();
					_ = line.Clear();
				}
				_ = line.Length == 0 ? line.Append(word) : line.Append(' ').Append(word);
			}
			yield return line.ToString();
		}
	}

	private string Colour(JourneyOutcome outcome)
	{
		var line = Line(outcome);
		if (!interactive)
			return line;
		if (outcome.Skipped)
			return Ansi.Blue($"{Ansi.Skip} {line}");
		return outcome.Passed
			? $"{Ansi.Green($"{Ansi.Ok} ok")}      {outcome.File.Id} {Ansi.Dim($"({outcome.Elapsed.TotalSeconds:0.0}s)")}"
			: $"{Ansi.BoldRed($"{Ansi.Fail} FAILED")}  {Ansi.Bold(outcome.File.Id)}";
	}

	private string Dim(string line) => interactive ? Ansi.Dim(line) : line;

	private void WriteLine(string line)
	{
		ClearStatus();
		output.WriteLine(line);
		DrawStatus(force: true);
	}

	/// <summary>Redraws the status line; a step update that comes moments after the last draw is deferred to the end of the interval, so a fast journey does not flood the terminal.</summary>
	private void DrawStatus(bool force)
	{
		if (!interactive || _running.Count == 0)
			return;
		if (!force && _sinceDraw.Elapsed < StatusInterval)
		{
			Defer();
			return;
		}
		_stale = false;
		ClearStatus();
		var running = string.Join(" · ", _running.OrderBy(r => r.Key, StringComparer.Ordinal).Select(r => $"{r.Key} ({r.Value})"));
		var status = $"running {_finished}/{_total} · {running}";
		var width = Width - 1;
		if (status.Length > width)
			status = status[..(width - 1)] + "…";
		_statusWidth = status.Length;
		output.Write(Ansi.Dim(status));
		output.Flush();
		_sinceDraw.Restart();
	}

	private void Defer()
	{
		if (_stale)
			return;
		_stale = true;
		var wait = StatusInterval - _sinceDraw.Elapsed;
		var due = wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
		_ = Task.Delay(due).ContinueWith(_ => RedrawIfStale(), TaskScheduler.Default);
	}

	private void RedrawIfStale()
	{
		lock (_gate)
		{
			if (_stale)
				DrawStatus(force: true);
		}
	}

	private void ClearStatus()
	{
		if (!interactive || _statusWidth == 0)
			return;
		output.Write("\r\e[K");
		_statusWidth = 0;
	}
}
