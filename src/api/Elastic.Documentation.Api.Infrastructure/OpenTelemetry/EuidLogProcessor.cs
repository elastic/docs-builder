// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Documentation.Api.Core;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Elastic.Documentation.Api.Infrastructure.OpenTelemetry;

/// <summary>
/// OpenTelemetry log processor that automatically adds user.euid attribute to log records
/// when it exists in the current activity's baggage.
/// This ensures the euid is present on all log records when set by the ASP.NET Core instrumentation.
/// </summary>
public class EuidLogProcessor : BaseProcessor<LogRecord>
{
	public override void OnEnd(LogRecord logRecord)
	{
		// Check if euid already exists as an attribute
		var hasEuidAttribute = logRecord.Attributes?.Any(a =>
			a.Key == TelemetryConstants.UserEuidAttributeName) ?? false;

		if (hasEuidAttribute)
		{
			return;
		}

		// Read euid from current activity baggage (set by ASP.NET Core request enrichment)
		var euid = Activity.Current?.GetBaggageItem(TelemetryConstants.UserEuidAttributeName);
		if (!string.IsNullOrEmpty(euid))
		{
			// Add euid as an attribute to this log record
			var newAttributes = new List<KeyValuePair<string, object?>>(logRecord.Attributes ?? [])
			{
				new(TelemetryConstants.UserEuidAttributeName, euid)
			};
			logRecord.Attributes = newAttributes;
		}
	}
}
