// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Amazon.CloudFront;
using Amazon.CloudFront.Model;

namespace Elastic.Documentation.OpenApiIndex;

/// <summary>
/// Invalidates CloudFront paths after OpenAPI spec or index updates.
/// </summary>
public sealed class CloudFrontCacheInvalidator(IAmazonCloudFront cloudFrontClient, string distributionId)
{
	/// <summary>
	/// Creates a CloudFront invalidation for the given paths.
	/// </summary>
	public async Task InvalidateAsync(IReadOnlyList<string> paths, string callerReference, Cancel ctx)
	{
		if (paths.Count == 0)
			return;

		var request = new CreateInvalidationRequest
		{
			DistributionId = distributionId,
			InvalidationBatch = new InvalidationBatch
			{
				CallerReference = callerReference,
				Paths = new Paths { Quantity = paths.Count, Items = [.. paths] }
			}
		};

		_ = await cloudFrontClient.CreateInvalidationAsync(request, ctx).ConfigureAwait(false);
	}
}
