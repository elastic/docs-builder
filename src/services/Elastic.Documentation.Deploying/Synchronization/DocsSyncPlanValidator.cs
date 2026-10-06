// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Deploying.Synchronization;

/// <summary>Checks a <see cref="SyncPlan"/> against <see cref="SyncPlanConstraints"/> and logs what is wrong with it.</summary>
public class DocsSyncPlanValidator(ILoggerFactory logFactory)
{
	private readonly ILogger<DocsSyncPlanValidator> _logger = logFactory.CreateLogger<DocsSyncPlanValidator>();

	public PlanValidationResult Validate(SyncPlan plan)
	{
		if (plan.DeleteThresholdDefault is not null)
			_logger.LogInformation("Using user-specified delete threshold of {Threshold}", plan.DeleteThresholdDefault);

		// The plan is checked once, here: this validator is the single gate in front of a sync.
		var errors = Result
			.From(SyncPlanConstraints.All.Report(plan), "sync-plan")
			.Diagnostics
			.Where(d => d.Severity == Severity.Error)
			.ToList();
		if (!plan.RemoteListingCompleted || plan.TotalSourceFiles == 0)
		{
			foreach (var error in errors.Take(1))
				_logger.LogError("{Message}", error.Message);
			return new(false, 1.0f, plan.DeleteThresholdDefault ?? SyncPlan.DefaultDeleteThreshold);
		}

		var deleteThreshold = plan.EffectiveDeleteThreshold(out var reason);
		if (reason is not null)
			_logger.LogInformation("{Reason}", reason);

		foreach (var error in errors)
			_logger.LogError("{Message}", error.Message);
		return new(errors.Count == 0, plan.DeleteRatio, deleteThreshold);
	}
}
