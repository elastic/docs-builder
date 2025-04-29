// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;

namespace Elastic.Documentation.Lambda.LinkIndexUploader;

public class LinkReferenceProvider(IAmazonS3 s3Client, ILambdaLogger logger, string bucketName)
{
	public async Task<LinkReference> GetLinkReference(string key, Cancel ctx)
	{
		var getObjectRequest = new GetObjectRequest
		{
			BucketName = bucketName,
			Key = key
		};
		logger.LogInformation("Getting object {key} from bucket {bucketName}", key, bucketName);
		var getObjectResponse = await s3Client.GetObjectAsync(getObjectRequest, ctx);
		await using var stream = getObjectResponse.ResponseStream;
		logger.LogInformation("Successfully got object {key} from bucket {bucketName}", key, bucketName);
		return LinkReference.Deserialize(stream);
	}
}
