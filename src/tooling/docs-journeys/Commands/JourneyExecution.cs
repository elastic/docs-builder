// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Cdp;
using Nullean.Cdp.Journeys;

namespace Elastic.Documentation.Journeys.Cli.Commands;

/// <summary>The outcome of one journey.</summary>
internal sealed record JourneyOutcome(JourneyFile File, JourneyResult? Result, string? Error, TimeSpan Elapsed)
{
	public bool Passed => Error is null && Result is { Passed: true };

	public string Describe() => Error is not null ? $"{File.Path}: {Error}" : Result!.Failure!.Describe(File.Path);
}

internal static class JourneyExecution
{
	/// <summary>Runs <paramref name="files"/> against <paramref name="baseUrl"/>, at most <paramref name="parallel"/> at a time in one shared browser.</summary>
	public static async Task<IReadOnlyList<JourneyOutcome>> RunAsync(
		IReadOnlyList<JourneyFile> files,
		Uri baseUrl,
		IReadOnlyList<string> allow,
		string? artifacts,
		int parallel,
		CancellationToken ct
	)
	{
		await using var browser = await Browser.LaunchAsync(cancellationToken: ct).ConfigureAwait(false);
		using var gate = new SemaphoreSlim(Math.Max(1, parallel));
		var tasks = files.Select(async file =>
		{
			await gate.WaitAsync(ct).ConfigureAwait(false);
			var started = DateTime.UtcNow;
			try
			{
				var journey = Journey.Load(file.Path);
				var options = new JourneyOptions
				{
					BaseUrl = baseUrl,
					AllowedHosts = allow,
					ArtifactsDirectory = artifacts is null ? null : Path.Combine(artifacts, file.Area, file.Name)
				};
				var result = await JourneyRunner.RunAsync(journey, browser, options, ct).ConfigureAwait(false);
				return new JourneyOutcome(file, result, null, DateTime.UtcNow - started);
			}
			catch (Exception e) when (e is not OperationCanceledException || !ct.IsCancellationRequested)
			{
				return new JourneyOutcome(file, null, e.Message, DateTime.UtcNow - started);
			}
			finally
			{
				_ = gate.Release();
			}
		});
		return await Task.WhenAll(tasks).ConfigureAwait(false);
	}
}
