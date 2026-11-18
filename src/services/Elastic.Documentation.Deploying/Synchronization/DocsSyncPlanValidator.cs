// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Sync;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Deploying.Synchronization;

public class DocsSyncPlanValidator(ILoggerFactory logFactory)
{
	private const string SyncPlanFile = "sync-plan";

	private readonly ILogger<DocsSyncPlanValidator> _logger = logFactory.CreateLogger<DocsSyncPlanValidator>();

	/// <summary>The verdict on a plan: the findings of <see cref="SyncPlanCounts"/> logged, and the delete ratio and threshold behind them.</summary>
	public PlanValidationResult Validate(SyncPlan plan)
	{
		if (plan.DeleteThresholdDefault is not null)
			_logger.LogInformation("Using user-specified delete threshold of {Threshold}", plan.DeleteThresholdDefault);

		var counts = plan.Counts;
		var errors = counts.Check(SyncPlanFile).Findings.Where(d => d.Severity == Severity.Error).ToList();

		// an incomplete listing or an empty build stops the verdict at that finding, as the checks always ran in this order
		if (!counts.RemoteListingCompleted || counts.TotalSourceFiles == 0)
		{
			foreach (var error in errors.Take(1))
				_logger.LogError("{Message}", error.Message);
			return new(false, 1.0f, counts.DeleteThresholdDefault ?? SyncPlanCounts.DefaultDeleteThreshold);
		}

		if (counts.DeleteThresholdReason is { } reason)
			_logger.LogInformation("{Reason}", reason);

		foreach (var error in errors)
			_logger.LogError("{Message}", error.Message);
		return new(errors.Count == 0, counts.DeleteRatio, counts.DeleteThreshold);
	}
}
