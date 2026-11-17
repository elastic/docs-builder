// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using AwesomeAssertions;
using Documentation.Builder.Http;
using Elastic.Documentation.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.Snapshots;

namespace Elastic.Documentation.Build.Tests.Snapshots;

/// <summary>
/// The state machine behind the serve host's diagnostics stream: the events a subscriber sees and the status, counts and
/// stored diagnostics after each build, for builds that finish, fail, queue behind each other and are stopped.
/// Timestamps are left out; everything else is as the client would read it.
/// </summary>
public class InMemoryBuildStateSnapshotTests
{
	private static readonly IFileSystem Real = new System.IO.Abstractions.FileSystem();

	private static InMemoryBuildState CreateState() =>
		new(NullLoggerFactory.Instance, TestHelpers.CreateConfigurationContext(new System.IO.Abstractions.TestingHelpers.MockFileSystem()));

	// The diagnostics of an earlier build stay readable in the next one, under that build's own folder.
	private static Regex TempDirectory() =>
		new(
			Regex.Escape(Paths.WorkingDirectoryRoot.FullName.Replace('\\', '/')) + @"/\.artifacts/test-temp/[a-z-]+-[0-9a-f]{32}",
			RegexOptions.None,
			RegexTimeouts.MatchTimeout
		);

	private static string Describe(BuildEvent e, string docs)
	{
		var sb = new StringBuilder(e.Type);
		if (e.Errors is not null || e.Warnings is not null || e.Hints is not null)
			_ = sb.Append($" errors={e.Errors} warnings={e.Warnings} hints={e.Hints}");
		if (e.Status is not null)
			_ = sb.Append($" status={e.Status}");
		if (e.Diagnostic is { } d)
			_ = sb.Append($" [{Describe(d, docs)}]");
		if (e.Diagnostics is { } all)
			_ = sb.Append($" diagnostics=[{string.Join("; ", all.Select(x => Describe(x, docs)))}]");
		return sb.ToString();
	}

	private static string Describe(DiagnosticDto d, string docs) =>
		$"{d.Severity} {d.File.Replace('\\', '/').Replace(docs.Replace('\\', '/'), "{docs}", StringComparison.Ordinal)}:{d.Line?.ToString() ?? "-"}:{d.Column?.ToString() ?? "-"} {d.Message.Replace('\\', '/').Replace(docs.Replace('\\', '/'), "{docs}", StringComparison.Ordinal)}";

	private static string Snapshot(InMemoryBuildState state, string docs) =>
		$"status={state.Status} errors={state.ErrorCount} warnings={state.WarningCount} hints={state.HintCount} stored={state.GetStoredDiagnostics().Length}\nstate event: {Describe(state.GetCurrentState(), docs)}";

	private static ScopedTempDirectory Docs(string name, string indexMarkdown, string? extraFile = null)
	{
		var temp = new ScopedTempDirectory(Real, name);
		// The build looks for the checkout of the docs; a folder under .artifacts has none of its own, so it gets a marker.
		_ = Real.Directory.CreateDirectory(Real.Path.Join(temp.FullName, ".git"));
		Real.File.WriteAllText(Real.Path.Join(temp.FullName, ".git", "HEAD"), "ref: refs/heads/main\n");
		Real.File.WriteAllText(Real.Path.Join(temp.FullName, "docset.yml"), "project: example\ntoc:\n  - file: index.md\n");
		Real.File.WriteAllText(Real.Path.Join(temp.FullName, "index.md"), indexMarkdown);
		if (extraFile is not null)
			Real.File.WriteAllText(Real.Path.Join(temp.FullName, "extra.md"), extraFile);
		return temp;
	}

	private static async Task<List<BuildEvent>> DrainAsync(ChannelReader<BuildEvent> reader, int terminalEvents, Cancel ct)
	{
		var events = new List<BuildEvent>();
		var seen = 0;
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
		timeout.CancelAfter(TimeSpan.FromMinutes(2));
		await foreach (var e in reader.ReadAllAsync(timeout.Token))
		{
			events.Add(e);
			if (e.Type is "build_complete" or "build_cancelled" && ++seen == terminalEvents)
				break;
		}
		return events;
	}

	private static async Task<string> RunBuildsAsync(
		InMemoryBuildState state,
		string docs,
		string[] sourcePaths,
		int terminalEvents,
		bool scheduleBeforeStart = false
	)
	{
		var ct = TestContext.Current!.Execution.CancellationToken;
		var reader = state.Subscribe();
		using var shutdown = CancellationTokenSource.CreateLinkedTokenSource(ct);
		var sb = new StringBuilder($"before: {Snapshot(state, docs)}\n");

		Task? loop = null;
		if (!scheduleBeforeStart)
			loop = Task.Run(() => state.RunAsync(shutdown.Token), ct);
		foreach (var path in sourcePaths)
			state.ScheduleBuild(path);
		loop ??= Task.Run(() => state.RunAsync(shutdown.Token), ct);

		var events = await DrainAsync(reader, terminalEvents, ct);
		foreach (var e in events)
			_ = sb.Append("event: ").Append(Describe(e, docs)).Append('\n');
		await shutdown.CancelAsync();
		await loop;
		_ = sb.Append($"after: {Snapshot(state, docs)}\n");
		return TempDirectory().Replace(sb.ToString(), "{docs}");
	}

	[Test]
	public void InitialState_IsIdleWithNothingStored()
	{
		using var state = CreateState();
		Snapshot(state, "/none")
			.Should()
			.MatchInlineSnapshot(
				"""
			status=Idle errors=0 warnings=0 hints=0 stored=0
			state event: state errors=0 warnings=0 hints=0 status=idle diagnostics=[]
			"""
			);
	}

	[Test]
	public async Task CleanDocs_IdleToBuildingToCompleteWithNoDiagnostics()
	{
		using var docs = Docs("state-clean", "# Home\n\nHello.\n");
		using var state = CreateState();
		(await RunBuildsAsync(state, docs.FullName, [docs.FullName], 1)).Should().MatchSnapshot();
	}

	[Test]
	public async Task DocsWithABrokenLink_StreamsTheDiagnosticAndCountsIt()
	{
		using var docs = Docs("state-broken", "# Home\n\n[missing](does-not-exist.md)\n");
		using var state = CreateState();
		(await RunBuildsAsync(state, docs.FullName, [docs.FullName], 1)).Should().MatchSnapshot();
	}

	[Test]
	public async Task SecondBuildResetsCountsAndStoredDiagnostics()
	{
		using var broken = Docs("state-first", "# Home\n\n[missing](does-not-exist.md)\n");
		using var clean = Docs("state-second", "# Home\n");
		using var state = CreateState();
		var first = await RunBuildsAsync(state, broken.FullName, [broken.FullName], 1);
		var second = await RunBuildsAsync(state, clean.FullName, [clean.FullName], 1);
		$"{first}\n=== second build\n{second}"
			.Replace(clean.FullName.Replace('\\', '/'), "{docs}", StringComparison.Ordinal)
			.Should()
			.MatchSnapshot();
	}

	[Test]
	public async Task SourcePathThatDoesNotExist_StillEndsInBuildComplete()
	{
		var missing = Real.Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-temp", $"does-not-exist-{Guid.NewGuid():N}");
		using var state = CreateState();
		(await RunBuildsAsync(state, missing, [missing], 1)).Should().MatchSnapshot();
	}

	[Test]
	public async Task TwoTriggersBeforeTheLoopStarts_OnlyTheLatestRuns()
	{
		using var broken = Docs("state-queue-a", "# Home\n\n[missing](does-not-exist.md)\n");
		using var clean = Docs("state-queue-b", "# Home\n");
		using var state = CreateState();
		(await RunBuildsAsync(
			state,
			broken.FullName,
			[broken.FullName, clean.FullName],
			1,
			scheduleBeforeStart: true
		)).Should().MatchSnapshot();
	}

	[Test]
	public async Task LoopStoppedBeforeAnythingIsScheduled_LeavesTheStateIdle()
	{
		using var state = CreateState();
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();
		await state.RunAsync(cts.Token);
		Snapshot(state, "/none")
			.Should()
			.MatchInlineSnapshot(
				"""
			status=Idle errors=0 warnings=0 hints=0 stored=0
			state event: state errors=0 warnings=0 hints=0 status=idle diagnostics=[]
			"""
			);
	}

	[Test]
	public async Task BuildCancelledByShutdown_EmitsBuildCancelledAndGoesIdle()
	{
		using var docs = Docs("state-cancel", "# Home\n");
		using var state = CreateState();
		var reader = state.Subscribe();
		using var shutdown = new CancellationTokenSource();
		var loop = Task.Run(() => state.RunAsync(shutdown.Token));
		state.ScheduleBuild(docs.FullName);

		// cancel as soon as the build has announced itself
		var first = await reader.ReadAsync(TestContext.Current!.Execution.CancellationToken);
		await shutdown.CancelAsync();
		await loop;

		var rest = new List<string>();
		while (reader.TryRead(out var e))
			rest.Add(e.Type);

		// build_start always comes first. What follows depends on how far the build got, so only the first event and the
		// invariant "a terminal event is one of two" are recorded.
		var terminal = rest.Where(t => t is "build_complete" or "build_cancelled").Distinct().OrderBy(t => t, StringComparer.Ordinal);
		$"first={first.Type}\nterminal events are a subset of: build_cancelled, build_complete => {terminal.All(t => t is "build_complete" or "build_cancelled")}\nfinal status is idle or complete => {state.Status is BuildStatus.Idle or BuildStatus.Complete}".Should().MatchInlineSnapshot(
			"""
				first=build_start
				terminal events are a subset of: build_cancelled, build_complete => True
				final status is idle or complete => True
				"""
		);
	}

	[Test]
	public async Task Subscribers_ReceiveTheSameEventsUntilTheyLeave()
	{
		using var docs = Docs("state-subscribers", "# Home\n");
		using var state = CreateState();
		var ct = TestContext.Current!.Execution.CancellationToken;
		var a = state.Subscribe();
		var b = state.Subscribe();
		var left = state.Subscribe();
		state.Unsubscribe(left);
		using var shutdown = CancellationTokenSource.CreateLinkedTokenSource(ct);
		var loop = Task.Run(() => state.RunAsync(shutdown.Token), ct);
		state.ScheduleBuild(docs.FullName);

		var fromA = await DrainAsync(a, 1, ct);
		var fromB = await DrainAsync(b, 1, ct);
		await shutdown.CancelAsync();
		await loop;
		var leftEvents = new List<string>();
		while (left.TryRead(out var e))
			leftEvents.Add(e.Type);

		state.CompleteAllClients();
		$"a: {string.Join(",", fromA.Select(e => e.Type))}\nb: {string.Join(",", fromB.Select(e => e.Type))}\nunsubscribed: [{string.Join(",", leftEvents)}] completed={left.Completion.IsCompleted}\nafter CompleteAllClients a completed={a.Completion.IsCompleted} b completed={b.Completion.IsCompleted}".Should().MatchInlineSnapshot(
			"""
			a: build_start,build_complete
			b: build_start,build_complete
			unsubscribed: [] completed=True
			after CompleteAllClients a completed=True b completed=True
			"""
		);
	}
}
