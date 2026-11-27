// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Deploying.Synchronization;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Deploying.Tests;

/// <summary>
/// The validator's verdict and every log call it makes, as deployment telemetry sees them: level, message template (<c>{OriginalFormat}</c>), the named
/// properties with their value types, and the rendered message. The expectations were captured from the validator on main.
/// </summary>
public class DocsSyncPlanValidatorLogTests
{
	private sealed class RecordingLogger : ILogger<DocsSyncPlanValidator>
	{
		public List<string> Entries { get; } = [];

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
			var properties = state is IEnumerable<KeyValuePair<string, object?>> kv
				? string.Join(
					", ",
					kv.Select(p => p.Key == "{OriginalFormat}" ? $"template=[{p.Value}]" : $"{p.Key}={p.Value?.GetType().Name}")
				)
				: "";
			Entries.Add($"{logLevel}: {properties} => {formatter(state, exception)}");
		}
	}

	private sealed class RecordingFactory(RecordingLogger logger) : ILoggerFactory
	{
		public void AddProvider(ILoggerProvider provider) { }

		public ILogger CreateLogger(string categoryName) => logger;

		public void Dispose() { }
	}

	private static string Transcript(bool completed, int source, int remote, int deletes, float? threshold)
	{
		var logger = new RecordingLogger();
		var plan = new SyncPlan
		{
			RemoteListingCompleted = completed,
			DeleteThresholdDefault = threshold,
			TotalSourceFiles = source,
			TotalRemoteFiles = remote,
			ExcludePatterns = [],
			TotalSyncRequests = 0,
			AddRequests = [],
			UpdateRequests = [],
			SkipRequests = [],
			DeleteRequests = [.. Enumerable.Range(0, deletes).Select(i => new DeleteRequest { DestinationPath = $"f{i}" })]
		};
		var result = new DocsSyncPlanValidator(new RecordingFactory(logger)).Validate(plan);
		var sb = new StringBuilder();
		_ = sb.AppendLine(
			CultureInfo.InvariantCulture,
			$"verdict: {result.Valid} {result.DeleteRatio.ToString(CultureInfo.InvariantCulture)} {result.DeleteThreshold.ToString(CultureInfo.InvariantCulture)}"
		);
		foreach (var e in logger.Entries)
			_ = sb.AppendLine(e);
		return sb.ToString().ReplaceLineEndings("\n");
	}

	private static string Scenarios()
	{
		var sb = new StringBuilder();
		(string Name, bool Completed, int Source, int Remote, int Deletes, float? Threshold)[] scenarios =
		[
			("negative source, nothing deleted", true, -1, 10, 0, null),
			("negative source, over the threshold", true, -1, 10, 9, null),
			("zero source", true, 0, 10, 0, null),
			("incomplete listing", false, 5, 10, 0, 0.3f),
			("incomplete listing and empty build", false, 0, 10, 0, null),
			("empty bucket", true, 5, 0, 0, null),
			("over the threshold", true, 5, 2000, 401, 0.2f),
			("user threshold on a young bucket", true, 5, 500, 100, 0.3f),
			("within the threshold", true, 5, 2000, 400, null)
		];
		foreach (var s in scenarios)
			_ = sb.Append("## ").AppendLine(s.Name).Append(Transcript(s.Completed, s.Source, s.Remote, s.Deletes, s.Threshold));
		return sb.ToString();
	}

	private const string Expected =
		"""
## negative source, nothing deleted
verdict: True 0 0.8
Information: template=[Plan has less than 100 total remote files ensuring delete threshold is at minimum 0.8] => Plan has less than 100 total remote files ensuring delete threshold is at minimum 0.8
## negative source, over the threshold
verdict: False 0.9 0.8
Information: template=[Plan has less than 100 total remote files ensuring delete threshold is at minimum 0.8] => Plan has less than 100 total remote files ensuring delete threshold is at minimum 0.8
Error: Ratio=Single, Threshold=Single, template=[Delete ratio is {Ratio} which is greater than the threshold of {Threshold}] => Delete ratio is 0.9 which is greater than the threshold of 0.8
## zero source
verdict: False 1 0.2
Error: template=[No files to sync] => No files to sync
## incomplete listing
verdict: False 1 0.3
Information: Threshold=Single, template=[Using user-specified delete threshold of {Threshold}] => Using user-specified delete threshold of 0.3
Error: template=[Remote files were not read to completion, cannot validate deployment plan] => Remote files were not read to completion, cannot validate deployment plan
## incomplete listing and empty build
verdict: False 1 0.2
Error: template=[Remote files were not read to completion, cannot validate deployment plan] => Remote files were not read to completion, cannot validate deployment plan
## empty bucket
verdict: True NaN 0
Information: template=[No files discovered in S3, assuming a clean bucket resetting delete threshold to `0.0' as our plan should not have ANY deletions] => No files discovered in S3, assuming a clean bucket resetting delete threshold to `0.0' as our plan should not have ANY deletions
## over the threshold
verdict: False 0.2005 0.2
Information: Threshold=Single, template=[Using user-specified delete threshold of {Threshold}] => Using user-specified delete threshold of 0.2
Error: Ratio=Single, Threshold=Single, template=[Delete ratio is {Ratio} which is greater than the threshold of {Threshold}] => Delete ratio is 0.2005 which is greater than the threshold of 0.2
## user threshold on a young bucket
verdict: True 0.2 0.5
Information: Threshold=Single, template=[Using user-specified delete threshold of {Threshold}] => Using user-specified delete threshold of 0.3
Information: template=[Plan has less than 1000 but more than a 100 total remote files ensuring delete threshold is at minimum 0.5] => Plan has less than 1000 but more than a 100 total remote files ensuring delete threshold is at minimum 0.5
## within the threshold
verdict: True 0.2 0.2
""";

	[Test]
	[Arguments("")]
	[Arguments("de-DE")]
	public void Validate_LogsEveryFindingAsMainDid(string culture)
	{
		var previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
		try
		{
			// the rendered number follows the culture; the verdict line and the templates and properties do not
			var actual = Scenarios().TrimEnd('\n');
			Console.WriteLine("ACT\n" + actual);
			actual.Should().Be(Expected);
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}
}
