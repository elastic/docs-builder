// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// <see cref="DecisionLog"/> turns hundreds of per-item decisions into a few lines. A section logs when it is full or
/// old, and everything that is left logs on an explicit flush or on dispose, so a run never loses the tail.
/// </summary>
public class DecisionLogTests
{
	private sealed class ListLogger : ILogger
	{
		public List<string> Lines { get; } = [];

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter
		) => Lines.Add(formatter(state, exception));
	}

	[Test]
	public void Flush_GroupsDecisionsByOutcomeAndReasonInOrderOfAppearance()
	{
		var logger = new ListLogger();
		using var log = new DecisionLog(logger, time: new FakeTimeProvider());

		log.Record("entries", "pool", "#1");
		log.Record("entries", "exclude", "#2", ">non-issue");
		log.Record("entries", "pool", "#3");
		log.Record("entries", "exclude", "#4", ">non-issue");
		log.Record("entries", "exclude", "#5", ">test");
		log.Flush();

		logger.Lines.Should().Equal("entries: pool #1 #3 · exclude (>non-issue) #2 #4 · exclude (>test) #5");
	}

	[Test]
	public void Dispose_LogsWhatIsLeft()
	{
		var logger = new ListLogger();
		var log = new DecisionLog(logger, time: new FakeTimeProvider());
		log.Record("bundle", "exclude", "158021.yaml", "per-product type/area filter");

		log.Dispose();

		logger.Lines.Should().Equal("bundle: exclude (per-product type/area filter) 158021.yaml");
	}

	[Test]
	public void Flush_NothingRecorded_LogsNothing()
	{
		var logger = new ListLogger();
		using var log = new DecisionLog(logger, time: new FakeTimeProvider());

		log.Flush();

		logger.Lines.Should().BeEmpty();
	}

	[Test]
	public void Flush_ThenDispose_DoesNotLogTheSameDecisionsTwice()
	{
		var logger = new ListLogger();
		var log = new DecisionLog(logger, time: new FakeTimeProvider());
		log.Record("entries", "pool", "#1");

		log.Flush();
		log.Dispose();

		logger.Lines.Should().ContainSingle();
	}

	[Test]
	public void Record_SectionReachesMaxItems_LogsThatSectionAtOnce()
	{
		var logger = new ListLogger();
		using var log = new DecisionLog(logger, maxItems: 3, time: new FakeTimeProvider());

		log.Record("entries", "pool", "#1");
		log.Record("entries", "pool", "#2");
		logger.Lines.Should().BeEmpty();

		log.Record("entries", "pool", "#3");
		log.Record("entries", "pool", "#4");

		logger.Lines.Should().Equal("entries: pool #1 #2 #3");
		log.Flush();
		logger.Lines.Should().Equal("entries: pool #1 #2 #3", "entries: pool #4");
	}

	[Test]
	public void Record_SectionsFillIndependently()
	{
		var logger = new ListLogger();
		using var log = new DecisionLog(logger, maxItems: 2, time: new FakeTimeProvider());

		log.Record("entries", "pool", "#1");
		log.Record("bundle", "exclude", "a.yaml", "filter");
		log.Record("entries", "pool", "#2");

		logger.Lines.Should().Equal("entries: pool #1 #2");
	}

	[Test]
	public void Record_SectionIsOlderThanTheInterval_LogsOnTheNextRecord()
	{
		var time = new FakeTimeProvider();
		var logger = new ListLogger();
		using var log = new DecisionLog(logger, interval: TimeSpan.FromSeconds(4), time: time);

		log.Record("entries", "pool", "#1");
		time.Advance(TimeSpan.FromSeconds(3));
		log.Record("entries", "pool", "#2");
		logger.Lines.Should().BeEmpty("the section is not stale yet");

		time.Advance(TimeSpan.FromSeconds(2));
		logger.Lines.Should().BeEmpty("there is no timer, so nothing is logged until something is recorded");
		log.Record("entries", "pool", "#3");

		logger.Lines.Should().Equal("entries: pool #1 #2 #3");
	}

	[Test]
	public void Record_AnotherSectionIsStale_LogsThatSectionToo()
	{
		var time = new FakeTimeProvider();
		var logger = new ListLogger();
		using var log = new DecisionLog(logger, interval: TimeSpan.FromSeconds(4), time: time);

		log.Record("bundle", "exclude", "a.yaml", "filter");
		time.Advance(TimeSpan.FromSeconds(5));
		log.Record("entries", "pool", "#1");

		logger.Lines.Should().Equal("bundle: exclude (filter) a.yaml");
	}

	[Test]
	public void Record_AfterALog_TheNextBatchStartsItsOwnInterval()
	{
		var time = new FakeTimeProvider();
		var logger = new ListLogger();
		using var log = new DecisionLog(logger, interval: TimeSpan.FromSeconds(4), time: time);

		log.Record("entries", "pool", "#1");
		time.Advance(TimeSpan.FromSeconds(4));
		log.Record("entries", "pool", "#2");
		logger.Lines.Should().Equal("entries: pool #1 #2");

		time.Advance(TimeSpan.FromSeconds(2));
		log.Record("entries", "pool", "#3");
		time.Advance(TimeSpan.FromSeconds(3));
		log.Record("entries", "pool", "#4");
		logger.Lines.Should().HaveCount(1, "the second batch started with #3 and is only 3 seconds old");

		time.Advance(TimeSpan.FromSeconds(1));
		log.Record("entries", "pool", "#5");

		logger.Lines.Should().Equal("entries: pool #1 #2", "entries: pool #3 #4 #5");
	}

	[Test]
	public async Task Record_FromManyThreads_LogsEveryDecisionExactlyOnce()
	{
		var logger = new ConcurrentListLogger();
		using var log = new DecisionLog(logger, maxItems: 7);
		const int threads = 8;
		const int perThread = 500;

		await Task.WhenAll(Enumerable.Range(0, threads).Select(t => Task.Run(() =>
		{
			for (var i = 0; i < perThread; i++)
				log.Record(i % 2 == 0 ? "entries" : "bundle", "pool", $"t{t}-{i}");
		})));
		log.Flush();

		var logged = logger.Lines.SelectMany(line => line[(line.IndexOf(" pool ", StringComparison.Ordinal) + 6)..].Split(' ')).ToList();
		logged.Should().HaveCount(threads * perThread);
		logged.Should().OnlyHaveUniqueItems();
	}

	[Test]
	public async Task Dispose_AnotherThreadIsStillLogging_WaitsForItToFinish()
	{
		using var logging = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		var logger = new BlockingLogger(logging, release);
		var log = new DecisionLog(logger, maxItems: 1);

		// The first decision fills the section, so this thread takes it and blocks inside the logger.
		var recording = Task.Run(() => log.Record("entries", "pool", "#1"));
		logging.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();

		var disposing = Task.Run(log.Dispose);
		await Task.Delay(200);
		disposing.IsCompleted.Should().BeFalse("the decision is taken from the section but not logged yet");

		release.Set();
		await disposing.WaitAsync(TimeSpan.FromSeconds(10));
		await recording;
		logger.Lines.Should().Equal("entries: pool #1");
	}

	private sealed class BlockingLogger(ManualResetEventSlim logging, ManualResetEventSlim release) : ILogger
	{
		private readonly System.Collections.Concurrent.ConcurrentQueue<string> _lines = new();

		public IReadOnlyCollection<string> Lines => _lines;

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter
		)
		{
			logging.Set();
			_ = release.Wait(TimeSpan.FromSeconds(10));
			_lines.Enqueue(formatter(state, exception));
		}
	}

	private sealed class ConcurrentListLogger : ILogger
	{
		private readonly System.Collections.Concurrent.ConcurrentQueue<string> _lines = new();

		public IReadOnlyCollection<string> Lines => _lines;

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter
		) => _lines.Enqueue(formatter(state, exception));
	}

	[Test]
	[Arguments("https://github.com/elastic/kibana/pull/160947", "#160947")]
	[Arguments("https://github.com/elastic/kibana/issues/12", "#12")]
	[Arguments("not a url", "not a url")]
	public void PullRequestSubject_NamesThePullRequestByNumber(string url, string subject) =>
		DecisionLog.PullRequestSubject(url).Should().Be(subject);
}
