// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
/// bottom, redrawn in place; elsewhere (CI, a pipe) it writes plain lines only.
/// </summary>
internal sealed class ConsoleRunReporter(TextWriter output, bool interactive, bool verbose) : IRunReporter
{
	/// <summary>A reason shared by more journeys than this is shown as a count instead of a line per journey.</summary>
	internal const int CollapseAbove = 3;

	private readonly Lock _gate = new();
	private readonly Dictionary<string, string> _running = [];
	private int _finished;
	private int _total;
	private int _statusWidth;

	public static bool UseInteractive => !Console.IsOutputRedirected && Environment.GetEnvironmentVariable("CI") is not { Length: > 0 };

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
			DrawStatus();
		}
	}

	public void Step(JourneyFile file, JourneyStep step)
	{
		lock (_gate)
		{
			_running[file.Id] = $"line {step.Line}";
			DrawStatus();
		}
	}

	public void Finished(JourneyOutcome outcome)
	{
		lock (_gate)
		{
			_ = _running.Remove(outcome.File.Id);
			_finished++;
			WriteLine(Colour(outcome));
		}
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

	/// <summary>What failed, where, and on which page, followed by the totals. This is the last thing a run writes.</summary>
	public static string Summary(IReadOnlyList<JourneyOutcome> outcomes, Uri baseUrl, TimeSpan elapsed, string journeysRoot, string rerun)
	{
		var text = new StringBuilder();
		foreach (var outcome in outcomes.Where(o => o.Failed))
			_ = text.AppendLine().Append(
				FailureBlock(outcome, journeysRoot, rerun.Replace("{id}", outcome.File.Id, StringComparison.Ordinal))
			);
		var passed = outcomes.Count(o => o.Passed);
		var failed = outcomes.Count(o => o.Failed);
		var skipped = outcomes.Count(o => o.Skipped);
		_ = text.AppendLine().Append(
			$"{passed} passed, {failed} failed, {skipped} skipped in {elapsed.TotalSeconds:0.0}s, against {baseUrl}"
		);
		return text.ToString();
	}

	/// <summary>
	/// A failure in the shape Nullean.Snapshots gives it under TUnit and in <c>snap journey run</c>: the step it stopped at, the
	/// error as written, then labelled lines. The message is shown as it came, never reflowed or cut.
	/// </summary>
	internal static string FailureBlock(JourneyOutcome outcome, string journeysRoot, string rerun)
	{
		var where = outcome.File.DisplayPath(journeysRoot);
		var text = new StringBuilder();
		if (outcome.Error is { } error)
			_ = text.AppendLine($"Journey did not run: {where}").AppendLine(error.TrimEnd());
		else
		{
			var failure = outcome.Result!.Failure!;
			_ = text.AppendLine($"Journey stopped at {failure.Describe(where)}");
			if (outcome.Result.Checkpoints is [.., { Url: { Length: > 0 } url }])
				_ = text.AppendLine("Page:".PadRight(Label) + url);
		}
		return text.AppendLine("Rerun:".PadRight(Label) + rerun).ToString();
	}

	private const int Label = 11;

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
		DrawStatus();
	}

	private void DrawStatus()
	{
		ClearStatus();
		if (!interactive || _running.Count == 0)
			return;
		var running = string.Join(" · ", _running.OrderBy(r => r.Key, StringComparer.Ordinal).Select(r => $"{r.Key} ({r.Value})"));
		var status = $"running {_finished}/{_total} · {running}";
		var width = Math.Max(20, Console.WindowWidth > 0 ? Console.WindowWidth - 1 : 79);
		if (status.Length > width)
			status = status[..(width - 1)] + "…";
		_statusWidth = status.Length;
		output.Write($"\e[2m{status}\e[0m");
		output.Flush();
	}

	private void ClearStatus()
	{
		if (!interactive || _statusWidth == 0)
			return;
		output.Write("\r\e[K");
		_statusWidth = 0;
	}
}
