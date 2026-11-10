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
			foreach (var line in SkipLines(skipped, verbose))
				WriteLine(Dim(line));
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

	/// <summary>The site and what the run decided about it, one fact per line.</summary>
	public static string ContextBlock(SiteContext site, Uri url, string? env, string? buildType, string? feature)
	{
		var text = new StringBuilder();

		void Row(string label, string value, string? note = null) =>
			text.AppendLine($"\e[2m{label.PadRight(Label)}\e[0m{value}{(note is null ? "" : $" \e[2m({note})\e[0m")}");

		Row("Site", url.ToString());
		Row("Env", site.Env ?? "unknown", env is null ? null : "--env");
		Row("Build", site.BuildType ?? "unknown", buildType is null ? null : "--build-type");
		Row(
			"Features",
			site.Features.Count == 0 ? "none" : string.Join(' ', site.Features.Order(StringComparer.Ordinal)),
			feature is null ? null : "--feature"
		);
		foreach (var note in site.ProbeNotes)
			_ = text.AppendLine($"\e[33m{"Note".PadRight(Label)}\e[0m{note}");
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
		var totals = $"{passed} passed, {failed} failed, {skipped} skipped in {elapsed.TotalSeconds:0.0}s, against {baseUrl}";
		return text.AppendLine().Append(interactive && failed > 0 ? $"\e[31m{totals}\e[0m" : totals).ToString();
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
		_ = text.AppendLine($"\e[1;31m✗ {outcome.File.Id}\e[0m");
		_ = text.AppendLine($"  \e[2m{where}{(failure is null ? "" : $":{failure.Line}")}\e[0m");
		if (failure is not null)
			AppendSource(text, outcome.File.Path, failure.Line);
		_ = text.AppendLine();
		foreach (var line in Wrap(outcome.Error ?? failure!.Message, Width - 4))
			_ = text.AppendLine($"  {line}");
		_ = text.AppendLine();
		if (_pages.TryGetValue(outcome.File.Id, out var page))
			_ = text.AppendLine($"  \e[2m{"last open".PadRight(10)}\e[0m{page}");
		_ = text.AppendLine($"  \e[2m{"rerun".PadRight(10)}\e[0m{rerun}");
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
				? text.AppendLine($"  \e[1;31m▶ {number,3} │ {lines[number - 1]}\e[0m")
				: text.AppendLine($"  \e[2m  {number,3} │ {lines[number - 1]}\e[0m");
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
		return !interactive
			? line
			: outcome.Skipped ? Dim(line) : outcome.Passed ? $"\e[32m{line[..2]}\e[0m{line[2..]}" : $"\e[31m{line}\e[0m";
	}

	private string Dim(string line) => interactive ? $"\e[2m{line}\e[0m" : line;

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
		output.Write($"\e[2m{status}\e[0m");
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
