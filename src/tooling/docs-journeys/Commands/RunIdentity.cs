// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Journeys.Cli.Commands;

/// <summary>Who the run says it is, and whether it may change the site.</summary>
internal sealed record RunIdentity(bool Synthetic, bool ReadOnly, IReadOnlyList<string> Writable)
{
	public const string UserAgent =
		"Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko) Chrome Safari/537.36 Elastic/Synthetics docs-journeys";
	public const string MonitorHeader = "X-Docs-Synthetic-Monitor";

	/// <summary>The docs site's telemetry exports, which a real-user run sends as writes. A synthetic run sends none.</summary>
	public static readonly string[] TelemetryWrites = ["**/v1/o/t", "**/v1/o/l", "**/v1/o/m"];
}
