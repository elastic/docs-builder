// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Cdp;
using Nullean.Cdp.Journeys;

namespace Elastic.Documentation.Journeys.Cli.Commands;

internal sealed record JourneyOutcome(JourneyFile File, JourneyResult? Result, string? Error, string? SkipReason, TimeSpan Elapsed)
{
	public bool Skipped => SkipReason is not null;
	public bool Passed => !Skipped && Error is null && Result is { Passed: true };
	public bool Failed => !Skipped && !Passed;

	public string Describe() => Error is not null ? $"{File.Path}: {Error}" : Result!.Failure!.Describe(File.Path);

	public static JourneyOutcome Skip(JourneyFile file, string reason) => new(file, null, null, reason, TimeSpan.Zero);
}

/// <summary>How a run behaves beyond which journeys it picked.</summary>
internal sealed record RunSettings(Uri BaseUrl, IReadOnlyList<string> Allow, JourneyContext Context, RunIdentity Identity)
{
	public string? Artifacts { get; init; }
	public int Parallel { get; init; } = 4;
}

/// <summary>Who the run says it is, and whether it may change the site.</summary>
internal sealed record RunIdentity(bool Synthetic, bool ReadOnly, IReadOnlyList<string> Writable)
{
	public const string UserAgent =
		"Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko) Chrome Safari/537.36 Elastic/Synthetics docs-journeys";
	public const string MonitorHeader = "X-Docs-Synthetic-Monitor";

	/// <summary>The docs site's telemetry exports, which a real-user run sends as writes. A synthetic run sends none.</summary>
	public static readonly string[] TelemetryWrites = ["**/v1/o/t", "**/v1/o/l", "**/v1/o/m"];
}

internal static class JourneyExecution
{
	/// <summary>Runs the loaded journeys, at most <see cref="RunSettings.Parallel"/> at a time in one shared browser. Skipped ones do not start a browser.</summary>
	public static async Task<IReadOnlyList<JourneyOutcome>> RunAsync(
		IReadOnlyList<(LoadedJourney Loaded, string? Skip)> plan,
		RunSettings settings,
		IRunReporter reporter,
		CancellationToken ct
	)
	{
		await using var browser = plan.Any(p => p.Skip is null && p.Loaded.Journey is not null)
			? await Browser.LaunchAsync(cancellationToken: ct).ConfigureAwait(false)
			: null;
		using var gate = new SemaphoreSlim(Math.Max(1, settings.Parallel));
		var tasks = plan.Select(async item =>
		{
			var file = item.Loaded.File;
			if (item.Loaded.Error is { } parseError)
				return Finish(new JourneyOutcome(file, null, parseError, null, TimeSpan.Zero));
			if (item.Skip is { } reason)
				return JourneyOutcome.Skip(file, reason);

			await gate.WaitAsync(ct).ConfigureAwait(false);
			reporter.Started(file);
			var started = DateTime.UtcNow;
			try
			{
				var options = Options(file, settings, step => reporter.Step(file, step));
				var result = await JourneyRunner.RunAsync(item.Loaded.Journey!, browser!, options, ct).ConfigureAwait(false);
				return Finish(new JourneyOutcome(file, result, null, null, DateTime.UtcNow - started));
			}
			catch (Exception e) when (e is not OperationCanceledException || !ct.IsCancellationRequested)
			{
				return Finish(new JourneyOutcome(file, null, e.Message, null, DateTime.UtcNow - started));
			}
			finally
			{
				_ = gate.Release();
			}
		});
		return await Task.WhenAll(tasks).ConfigureAwait(false);

		JourneyOutcome Finish(JourneyOutcome outcome)
		{
			reporter.Finished(outcome);
			return outcome;
		}
	}

	private static JourneyOptions Options(JourneyFile file, RunSettings settings, Action<JourneyStep> onStep)
	{
		var headers = new Dictionary<string, string>();
		if (settings.Identity.Synthetic)
			headers[RunIdentity.MonitorHeader] = "true";
		return new JourneyOptions
		{
			BaseUrl = settings.BaseUrl,
			AllowedHosts = settings.Allow,
			ArtifactsDirectory = settings.Artifacts is null ? null : Path.Combine(settings.Artifacts, file.Area, file.Name),
			Features = new HashSet<string>(settings.Context.Features, StringComparer.Ordinal),
			BuildType = settings.Context.BuildType,
			Env = settings.Context.Env,
			Viewport = new JourneyViewport(1280, 800),
			UserAgent = settings.Identity.Synthetic ? RunIdentity.UserAgent : null,
			Headers = headers,
			OnStep = onStep,
			ReadOnly = settings.Identity.ReadOnly,
			WritableUrls = settings.Identity.Writable
		};
	}
}
