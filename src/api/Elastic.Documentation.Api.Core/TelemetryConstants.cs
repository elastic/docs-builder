// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Core;

/// <summary>
/// Constants for OpenTelemetry instrumentation in the Docs API.
/// </summary>
public static class TelemetryConstants
{
	/// <summary>
	/// ActivitySource name for AskAi operations.
	/// Used in AskAiUsecase to create spans.
	/// </summary>
	public const string AskAiSourceName = "Elastic.Documentation.Api.AskAi";

	/// <summary>
	/// ActivitySource name for StreamTransformer operations.
	/// Used in stream transformer implementations to create spans.
	/// </summary>
	public const string StreamTransformerSourceName = "Elastic.Documentation.Api.StreamTransformer";

	/// <summary>
	/// Tag/baggage name used to annotate spans with the user's EUID value.
	/// </summary>
	public const string UserEuidAttributeName = "user.euid";

	/// <summary>
	/// ActivitySource name for OTLP proxy operations.
	/// Used to trace frontend telemetry proxying.
	/// </summary>
	public const string OtlpProxySourceName = "Elastic.Documentation.Api.OtlpProxy";
}
