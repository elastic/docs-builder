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
}
