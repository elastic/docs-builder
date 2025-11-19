// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Documentation.Api.Core;
using OpenTelemetry;

namespace Elastic.Documentation.Api.Infrastructure;

/// <summary>
/// OpenTelemetry span processor that automatically adds user.euid tag to all spans
/// when it exists in the activity baggage.
/// This ensures the euid is present on all spans (root and children) without manual propagation.
/// </summary>
public class EuidSpanProcessor : BaseProcessor<Activity>
{
	public override void OnStart(Activity activity)
	{
		// Check if euid exists in baggage (set by ASP.NET Core request enrichment)
		var euid = activity.GetBaggageItem(TelemetryConstants.UserEuidAttributeName);
		if (!string.IsNullOrEmpty(euid))
		{
			// Add as a tag to this span if not already present
			var hasEuidTag = activity.TagObjects.Any(t => t.Key == TelemetryConstants.UserEuidAttributeName);
			if (!hasEuidTag)
			{
				_ = activity.SetTag(TelemetryConstants.UserEuidAttributeName, euid);
			}
		}
	}
}
