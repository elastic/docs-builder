// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog;

/// <summary>
/// Collects the per-item decisions of a long run, such as "pull request #123 is excluded because of label
/// <c>&gt;non-issue</c>", and logs them as a few lines instead of one line each. Every section ("entries", "bundle",
/// "notes") has its own buffer. A section logs when it holds <c>maxItems</c> decisions or when its oldest decision is
/// <c>interval</c> old, and once more when the log is flushed or disposed. A line groups the decisions of a section by
/// outcome and reason: <c>entries: pool #1 #2 · inferred #3 · exclude (&gt;non-issue) #4 #5</c>.
/// Warnings and errors do not go through here. They are reported at once.
/// </summary>
public sealed partial class DecisionLog : IDisposable
{
	public const int DefaultMaxItems = 50;
	public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(4);

	[GeneratedRegex(@"/(?:pull|issues)/(\d+)")]
	private static partial Regex PullRequestUrlRegex();

	private readonly ILogger _logger;
	private readonly int _maxItems;
	private readonly TimeSpan _interval;
	private readonly TimeProvider _time;
	private readonly ITimer _timer;
	private readonly Lock _gate = new();
	private readonly Dictionary<string, Section> _sections = [];

	private sealed class Section(DateTimeOffset startedAt)
	{
		public DateTimeOffset StartedAt { get; } = startedAt;
		public int Count { get; set; }

		/// <summary>The decisions in the order they first appeared, grouped by outcome and reason.</summary>
		public Dictionary<(string Outcome, string? Reason), List<string>> Groups { get; } = [];
	}

	public DecisionLog(ILogger logger, int maxItems = DefaultMaxItems, TimeSpan? interval = null, TimeProvider? time = null)
	{
		_logger = logger;
		_maxItems = Math.Max(1, maxItems);
		_interval = interval ?? DefaultInterval;
		_time = time ?? TimeProvider.System;
		_timer = _time.CreateTimer(_ => FlushDue(), null, _interval, _interval);
	}

	/// <summary>The subject of a pull request URL, <c>#123</c>, or the URL itself when it names no number.</summary>
	public static string PullRequestSubject(string url) =>
		PullRequestUrlRegex().Match(url) is { Success: true } m ? $"#{m.Groups[1].Value}" : url;

	/// <summary>Records one decision. The section logs at once when it is full.</summary>
	public void Record(string section, string outcome, string subject, string? reason = null)
	{
		string? full = null;
		lock (_gate)
		{
			if (!_sections.TryGetValue(section, out var buffer))
				_sections[section] = buffer = new Section(_time.GetUtcNow());

			if (!buffer.Groups.TryGetValue((outcome, reason), out var subjects))
				buffer.Groups[(outcome, reason)] = subjects = [];

			subjects.Add(subject);
			buffer.Count++;
			if (buffer.Count >= _maxItems)
				full = section;
		}

		if (full is not null)
			Flush(full);
	}

	/// <summary>Logs every section that holds something, oldest first.</summary>
	public void Flush()
	{
		string[] sections;
		lock (_gate)
			sections = [.. _sections.OrderBy(s => s.Value.StartedAt).Select(s => s.Key)];

		foreach (var section in sections)
			Flush(section);
	}

	private void FlushDue()
	{
		string[] due;
		lock (_gate)
		{
			var now = _time.GetUtcNow();
			due = [.. _sections.Where(s => now - s.Value.StartedAt >= _interval).Select(s => s.Key)];
		}

		foreach (var section in due)
			Flush(section);
	}

	private void Flush(string section)
	{
		string? line;
		lock (_gate)
		{
			if (!_sections.Remove(section, out var buffer) || buffer.Count == 0)
				return;

			line = Format(section, buffer);
		}

		_logger.LogInformation("{Decisions}", line);
	}

	private static string Format(string section, Section buffer)
	{
		var sb = new StringBuilder(section).Append(':');
		var first = true;
		foreach (var ((outcome, reason), subjects) in buffer.Groups)
		{
			_ = sb.Append(first ? " " : " · ").Append(outcome);
			if (reason is not null)
				_ = sb.Append(" (").Append(reason).Append(')');
			_ = sb.Append(' ').AppendJoin(' ', subjects);
			first = false;
		}

		return sb.ToString();
	}

	public void Dispose()
	{
		_timer.Dispose();
		Flush();
	}
}
