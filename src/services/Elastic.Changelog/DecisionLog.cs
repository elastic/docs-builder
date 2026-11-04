// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog;

/// <summary>
/// Collects the per-item decisions of a long run, such as "pull request #123 is excluded because of label
/// <c>&gt;non-issue</c>", and logs them as a few lines instead of one line each. Every section ("entries", "bundle",
/// "notes") has its own buffer. A section logs when it holds <c>maxItems</c> decisions or when its oldest decision is
/// <c>interval</c> old, and the rest logs on <see cref="Flush"/> and <see cref="Dispose"/>. A line groups the decisions of a
/// section by outcome and reason: <c>entries: pool #1 #2 · inferred #3 · exclude (&gt;non-issue) #4 #5</c>.
/// Warnings and errors do not go through here. They are reported at once.
/// <para>
/// There are no locks and no timer. A section is a concurrent queue that any thread can add to, and a flush drains it:
/// every decision is dequeued by exactly one thread, so none is lost or logged twice. Age is checked when a decision is
/// added, against a cheap timestamp, so a section that stops receiving decisions waits for the next flush. The timing
/// is approximate on purpose.
/// </para>
/// </summary>
public sealed partial class DecisionLog(
	ILogger logger,
	int maxItems = DecisionLog.DefaultMaxItems,
	TimeSpan? interval = null,
	TimeProvider? time = null
) : IDisposable
{
	public const int DefaultMaxItems = 50;
	public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(4);

	[GeneratedRegex(@"/(?:pull|issues)/(\d+)")]
	private static partial Regex PullRequestUrlRegex();

	private readonly int _maxItems = Math.Max(1, maxItems);
	private readonly TimeSpan _interval = interval ?? DefaultInterval;
	private readonly TimeProvider _time = time ?? TimeProvider.System;
	private readonly ConcurrentDictionary<string, Section> _sections = [];
	private int _flushing;

	private readonly record struct Decision(string Outcome, string? Reason, string Subject);

	private sealed class Section
	{
		private int _pending;
		private long _firstAt;

		public ConcurrentQueue<Decision> Decisions { get; } = new();

		public int Pending => Volatile.Read(ref _pending);

		/// <summary>When the oldest pending decision was added, or 0 when nothing is pending.</summary>
		public long FirstAt => Volatile.Read(ref _firstAt);

		public void Add(Decision decision, long now)
		{
			Decisions.Enqueue(decision);
			_ = Interlocked.CompareExchange(ref _firstAt, now, 0);
			_ = Interlocked.Increment(ref _pending);
		}

		public void Drained(int count)
		{
			_ = Interlocked.Add(ref _pending, -count);
			_ = Interlocked.Exchange(ref _firstAt, 0);
		}
	}

	/// <summary>
	/// A log that does not batch: every decision is logged as soon as it is recorded. For a single command, such as
	/// <c>changelog add</c>, where there is nothing to group.
	/// </summary>
	public static DecisionLog Immediate(ILogger logger) => new(logger, maxItems: 1);

	/// <summary>The subject of a pull request URL, <c>#123</c>, or the URL itself when it names no number.</summary>
	public static string PullRequestSubject(string url) =>
		PullRequestUrlRegex().Match(url) is { Success: true } m ? $"#{m.Groups[1].Value}" : url;

	/// <summary>Records one decision, and logs every section that is full or has waited longer than the interval.</summary>
	public void Record(string section, string outcome, string subject, string? reason = null)
	{
		var now = _time.GetTimestamp();
		var buffer = _sections.GetOrAdd(section, static _ => new Section());
		buffer.Add(new Decision(outcome, reason, subject), now);

		if (buffer.Pending >= _maxItems)
			Flush(section, buffer);

		foreach (var (name, other) in _sections)
		{
			if (other.FirstAt is var first and not 0 && _time.GetElapsedTime(first, now) >= _interval)
				Flush(name, other);
		}
	}

	/// <summary>
	/// Logs everything that is pending, the sections that started first coming first. It returns when every decision recorded
	/// before the call is logged, including those another thread has taken from a section and is still logging.
	/// </summary>
	public void Flush()
	{
		foreach (var (name, buffer) in _sections.OrderBy(s => s.Value.FirstAt == 0 ? long.MaxValue : s.Value.FirstAt))
			Flush(name, buffer);

		var wait = new SpinWait();
		while (Volatile.Read(ref _flushing) > 0)
			wait.SpinOnce();
	}

	private void Flush(string section, Section buffer)
	{
		// Counted from before the first decision is taken until after the line is logged, so Flush can wait for it.
		_ = Interlocked.Increment(ref _flushing);
		try
		{
			FlushCore(section, buffer);
		}
		finally
		{
			_ = Interlocked.Decrement(ref _flushing);
		}
	}

	private void FlushCore(string section, Section buffer)
	{
		var groups = new Dictionary<(string Outcome, string? Reason), List<string>>();
		var drained = 0;
		while (buffer.Decisions.TryDequeue(out var decision))
		{
			drained++;
			if (!groups.TryGetValue((decision.Outcome, decision.Reason), out var subjects))
				groups[(decision.Outcome, decision.Reason)] = subjects = [];

			subjects.Add(decision.Subject);
		}

		if (drained == 0)
			return;

		buffer.Drained(drained);
		logger.LogInformation("{Decisions}", Format(section, groups));
	}

	private static string Format(string section, Dictionary<(string Outcome, string? Reason), List<string>> groups)
	{
		var sb = new StringBuilder(section).Append(':');
		var first = true;
		foreach (var ((outcome, reason), subjects) in groups)
		{
			_ = sb.Append(first ? " " : " · ").Append(outcome);
			if (reason is not null)
				_ = sb.Append(" (").Append(reason).Append(')');
			_ = sb.Append(' ').AppendJoin(' ', subjects);
			first = false;
		}

		return sb.ToString();
	}

	public void Dispose() => Flush();
}
