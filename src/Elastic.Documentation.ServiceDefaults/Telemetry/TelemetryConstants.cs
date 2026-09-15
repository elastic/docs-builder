// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ServiceDefaults.Telemetry;

/// <summary>
/// Centralized constants for OpenTelemetry instrumentation names.
/// These ensure consistency between source/meter creation and registration.
/// </summary>
public static class TelemetryConstants
{
	public const string AssemblerSyncInstrumentationName = "Elastic.Documentation.Assembler.Sync";

	/// <summary>
	/// Tag/baggage name used to annotate spans and log records with the user's EUID value.
	/// </summary>
	public const string UserEuidAttributeName = "user.euid";

	/// <summary>
	/// Request header sent by our synthetics monitors (see synthetics.config.ts) so their
	/// traffic can be excluded from ASP.NET Core tracing instrumentation. Metrics aren't
	/// filtered: OpenTelemetry.Instrumentation.AspNetCore's metrics extension takes no
	/// options/filter in the version we're on.
	/// </summary>
	public const string SyntheticMonitorHeaderName = "X-Docs-Synthetic-Monitor";
}
