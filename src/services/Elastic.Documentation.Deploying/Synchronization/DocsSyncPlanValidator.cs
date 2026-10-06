// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Deploying.Synchronization;

/// <summary>The verdict on a <see cref="SyncPlan"/>, from the findings of <see cref="SyncPlanConstraints"/>, logged.</summary>
public class DocsSyncPlanValidator(ILoggerFactory logFactory)
{
	private readonly ILogger<DocsSyncPlanValidator> _logger = logFactory.CreateLogger<DocsSyncPlanValidator>();

	/// <summary>Checks a plan the planner just built (once, through <see cref="SyncPlanLoader.Check"/>) and logs what is wrong with it.</summary>
	public PlanValidationResult Validate(SyncPlan plan) => Validate(SyncPlanLoader.Check(plan));

	/// <summary>
	/// The verdict on a plan that was already checked (by <see cref="SyncPlanLoader.Load"/> or <see cref="SyncPlanLoader.Check"/>): reads its findings,
	/// does not run the rules again, so a bad plan is reported once with all of its violations.
	/// </summary>
	public PlanValidationResult Validate(Result<SyncPlan> checkedPlan)
	{
		var plan = checkedPlan.Value ?? throw new ArgumentException("A plan that did not load has no verdict.", nameof(checkedPlan));
		if (plan.DeleteThresholdDefault is not null)
			_logger.LogInformation("Using user-specified delete threshold of {Threshold}", plan.DeleteThresholdDefault);

		// the plan was checked once, by the loader: this validator turns its findings into the verdict in front of a sync
		var errors = checkedPlan.Diagnostics.Where(d => d.Severity == Severity.Error).ToList();
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
