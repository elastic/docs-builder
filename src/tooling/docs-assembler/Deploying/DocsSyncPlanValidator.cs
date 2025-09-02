// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Deploying;

public class DocsSyncPlanValidator(ILoggerFactory logFactory)
{
	private readonly ILogger<AwsS3SyncPlanStrategy> _logger = logFactory.CreateLogger<AwsS3SyncPlanStrategy>();

	public PlanValidationResult Validate(SyncPlan plan)
	{
		if (plan.DeleteThresholdDefault is not null)
			_logger.LogInformation("Using user-specified delete threshold of {Threshold}", plan.DeleteThresholdDefault);

		var deleteThreshold = plan.DeleteThresholdDefault ?? 0.2f;
		if (!plan.RemoteListingCompleted)
		{
			_logger.LogError("Remote files were not read to completion, cannot validate deployment plan");
			return new(false, 1.0f, deleteThreshold);
		}

		if (plan.TotalSourceFiles == 0)
		{
			_logger.LogError("No files to sync");
			return new(false, 1.0f, deleteThreshold);
		}

		var deleteRatio = (float)plan.DeleteRequests.Count / plan.TotalRemoteFiles;
		if (plan.TotalRemoteFiles == 0)
		{
			_logger.LogInformation("No files discovered in S3, assuming a clean bucket resetting delete threshold to `0.0' as our plan should not have ANY deletions");
			deleteThreshold = 0.0f;
		}
		// if the total remote files are less than or equal to 100, we enforce a higher ratio of 0.8
		// this allows newer assembled documentation to be in a higher state of flux
		if (plan.TotalRemoteFiles <= 100)
		{
			_logger.LogInformation("Plan has less than 100 total remote files ensuring delete threshold is at minimum 0.8");
			deleteThreshold = Math.Max(deleteThreshold, 0.8f);
		}

		// if the total remote files are less than or equal to 1000, we enforce a higher ratio of 0.5
		// this allows newer assembled documentation to be in a higher state of flux
		else if (plan.TotalRemoteFiles <= 1000)
		{
			_logger.LogInformation("Plan has less than 1000 but more than a 100 total remote files ensuring delete threshold is at minimum 0.5");
			deleteThreshold = Math.Max(deleteThreshold, 0.5f);
		}

		if (deleteRatio > deleteThreshold)
		{
			_logger.LogError("Delete ratio is {Ratio} which is greater than the threshold of {Threshold}", deleteRatio, deleteThreshold);
			return new(false, deleteRatio, deleteThreshold);
		}

		return new(true, deleteRatio, deleteThreshold);
	}


}
