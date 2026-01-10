// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;

namespace Elastic.Documentation.AppliesTo;

/// <summary>
/// Comparer for ordering ApplicableTo objects according to documentation standards.
/// Orders by:
/// 1. Serverless first, then Stack (latest to oldest)
/// 2. For deployments: ech/ess, ece, eck, self-managed
/// 3. Unavailable lifecycle comes last
/// </summary>
public class ApplicableToOrderComparer : IComparer<ApplicableTo?>
{
	public int Compare(ApplicableTo? x, ApplicableTo? y)
	{
		if (ReferenceEquals(x, y))
			return 0;
		if (x is null)
			return 1;
		if (y is null)
			return -1;

		// Check if either has unavailable lifecycle - unavailable goes last
		var xIsUnavailable = IsUnavailable(x);
		var yIsUnavailable = IsUnavailable(y);

		if (xIsUnavailable && !yIsUnavailable)
			return 1;
		if (!xIsUnavailable && yIsUnavailable)
			return -1;

		// Both unavailable or both available, continue with normal ordering

		// Determine the primary category for each
		var xCategory = GetPrimaryCategory(x);
		var yCategory = GetPrimaryCategory(y);

		// Compare by category first
		var categoryComparison = xCategory.CompareTo(yCategory);
		if (categoryComparison != 0)
			return categoryComparison;

		// Within the same category, apply specific ordering rules
		return xCategory switch
		{
			ApplicabilityCategory.Serverless => 0,
			ApplicabilityCategory.Stack => CompareStack(x, y),
			ApplicabilityCategory.Deployment => CompareDeployment(x, y),
			_ => 0
		};
	}

	private static bool IsUnavailable(ApplicableTo applicableTo)
	{
		// Check if any applicability has unavailable lifecycle
		if (applicableTo.Stack is not null &&
			applicableTo.Stack.Any(a => a.Lifecycle == ProductLifecycle.Unavailable))
			return true;

		if (applicableTo.Serverless is not null)
		{
			var serverless = applicableTo.Serverless;
			if ((serverless.Elasticsearch?.Any(a => a.Lifecycle == ProductLifecycle.Unavailable) ?? false) ||
				(serverless.Observability?.Any(a => a.Lifecycle == ProductLifecycle.Unavailable) ?? false) ||
				(serverless.Security?.Any(a => a.Lifecycle == ProductLifecycle.Unavailable) ?? false))
				return true;
		}

		if (applicableTo.Deployment is not null)
		{
			var deployment = applicableTo.Deployment;
			if ((deployment.Ess?.Any(a => a.Lifecycle == ProductLifecycle.Unavailable) ?? false) ||
				(deployment.Ece?.Any(a => a.Lifecycle == ProductLifecycle.Unavailable) ?? false) ||
				(deployment.Eck?.Any(a => a.Lifecycle == ProductLifecycle.Unavailable) ?? false) ||
				(deployment.Self?.Any(a => a.Lifecycle == ProductLifecycle.Unavailable) ?? false))
				return true;
		}

		return false;
	}

	private static ApplicabilityCategory GetPrimaryCategory(ApplicableTo applicableTo)
	{
		// Serverless takes priority
		if (applicableTo.Serverless is not null)
			return ApplicabilityCategory.Serverless;

		// Then Stack
		if (applicableTo.Stack is not null)
			return ApplicabilityCategory.Stack;

		// Then Deployment
		if (applicableTo.Deployment is not null)
			return ApplicabilityCategory.Deployment;

		// Default
		return ApplicabilityCategory.Other;
	}

	private static int CompareStack(ApplicableTo x, ApplicableTo y)
	{
		// Stack: order from latest to oldest version
		var xVersion = GetLatestVersion(x.Stack);
		var yVersion = GetLatestVersion(y.Stack);

		if (xVersion is null && yVersion is null)
			return 0;
		if (xVersion is null)
			return 1;
		if (yVersion is null)
			return -1;

		// Higher version comes first (descending order)
		return yVersion.CompareTo(xVersion);
	}

	private static int CompareDeployment(ApplicableTo x, ApplicableTo y)
	{
		// Deployment order: ech/ess, ece, eck, self-managed
		var xDeploymentType = GetPrimaryDeploymentType(x.Deployment!);
		var yDeploymentType = GetPrimaryDeploymentType(y.Deployment!);

		return xDeploymentType.CompareTo(yDeploymentType);
	}

	private static DeploymentType GetPrimaryDeploymentType(DeploymentApplicability deployment)
	{
		// Return the first deployment type found in priority order
		if (deployment.Ess is not null)
			return DeploymentType.Ech; // ESS = ECH (Elastic Cloud Hosted)
		if (deployment.Ece is not null)
			return DeploymentType.Ece;
		if (deployment.Eck is not null)
			return DeploymentType.Eck;
		if (deployment.Self is not null)
			return DeploymentType.SelfManaged;

		return DeploymentType.Unknown;
	}

	private static SemVersion? GetLatestVersion(AppliesCollection? collection)
	{
		if (collection is null || collection.Count == 0)
			return null;

		// Find the highest version in the collection using LINQ
		return collection
			.Select(applicability => applicability.Version?.Min)
			.Where(version => version is not null)
			.DefaultIfEmpty(null)
			.Max();
	}

	private enum ApplicabilityCategory
	{
		Serverless = 0,  // Serverless first
		Stack = 1,       // Stack second
		Deployment = 2,  // Deployment third
		Other = 3        // Everything else last
	}

	private enum DeploymentType
	{
		Ech = 0,         // ECH/ESS first
		Ece = 1,         // ECE second
		Eck = 2,         // ECK third
		SelfManaged = 3, // Self-managed last
		Unknown = 4
	}
}
