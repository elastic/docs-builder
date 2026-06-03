// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Elastic.Documentation.Mcp.Remote.Health;

/// <summary>
/// Readiness check that pings the Elasticsearch cluster.
/// Registered on <c>/health</c> (no tag predicate) so that a cluster outage returns 503,
/// giving infra/monitoring a direct signal. Intentionally excluded from <c>/alive</c>
/// (tagged <c>"live"</c>) to prevent pod crash-loops during transient ES downtime.
/// </summary>
internal sealed class ElasticsearchHealthCheck(ElasticsearchClientAccessor accessor) : IHealthCheck
{
	public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
	{
		try
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(TimeSpan.FromSeconds(5));
			return await accessor.CanConnect(cts.Token)
				? HealthCheckResult.Healthy()
				: HealthCheckResult.Unhealthy("Elasticsearch ping failed");
		}
		catch (Exception ex)
		{
			return HealthCheckResult.Unhealthy("Elasticsearch unreachable", ex);
		}
	}
}
