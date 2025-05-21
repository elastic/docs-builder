// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Documentation.Links;

namespace Elastic.Documentation.LinkIndex;

public interface ILinkIndexProvider
{
	Task<LinkReferenceRegistry> GetLinkIndex(Cancel cancellationToken = default);
	Task SaveLinkIndex(LinkReferenceRegistry registry, Cancel cancellationToken = default);
	Task<LinkReference> GetLinkReference(string key, Cancel cancellationToken = default);

	string GetLinkIndexPublicUrl();
}

public class AwsS3LinkIndexProvider(IAmazonS3 s3Client, string bucketName = "elastic-docs-link-index", string registryKey = "link-index.json") : ILinkIndexProvider
{

	public static AwsS3LinkIndexProvider CreateAnonymous()
	{
		var credentials = new AnonymousAWSCredentials();
		var config = new AmazonS3Config
		{
			RegionEndpoint = Amazon.RegionEndpoint.USEast2
		};
		var s3Client = new AmazonS3Client(credentials, config);
		return new AwsS3LinkIndexProvider(s3Client);
	}

	public async Task<LinkReferenceRegistry> GetLinkIndex(Cancel cancellationToken = default)
	{
		var getObjectRequest = new GetObjectRequest
		{
			BucketName = bucketName,
			Key = registryKey
		};
		var getObjectResponse = await s3Client.GetObjectAsync(getObjectRequest, cancellationToken);
		await using var stream = getObjectResponse.ResponseStream;
		var linkIndex = LinkReferenceRegistry.Deserialize(stream);
		return linkIndex with { ETag = getObjectResponse.ETag };
	}

	public async Task SaveLinkIndex(LinkReferenceRegistry registry, Cancel cancellationToken = default)
	{
		if (registry.ETag == null)
			// The ETag should not be null if the LinkReferenceRegistry was retrieved from GetLinkIndex()
			throw new InvalidOperationException($"{nameof(LinkReferenceRegistry)}.{nameof(registry.ETag)} cannot be null");
		var json = LinkReferenceRegistry.Serialize(registry);
		var putObjectRequest = new PutObjectRequest
		{
			BucketName = bucketName,
			Key = registryKey,
			ContentBody = json,
			ContentType = "application/json",
			IfMatch = registry.ETag // Only update if the ETag matches. Meaning the object has not been changed in the meantime.
		};
		var putResponse = await s3Client.PutObjectAsync(putObjectRequest, cancellationToken);
		if (putResponse.HttpStatusCode != HttpStatusCode.OK)
			throw new Exception($"Unable to save {nameof(LinkReferenceRegistry)} to s3://{bucketName}/{registryKey}");
	}

	public async Task<LinkReference> GetLinkReference(string key, Cancel cancellationToken)
	{
		var getObjectRequest = new GetObjectRequest
		{
			BucketName = bucketName,
			Key = key
		};
		var getObjectResponse = await s3Client.GetObjectAsync(getObjectRequest, cancellationToken);
		await using var stream = getObjectResponse.ResponseStream;
		return LinkReference.Deserialize(stream);
	}

	public string GetLinkIndexPublicUrl() => $"https://{bucketName}.s3.{s3Client.Config.RegionEndpoint.SystemName}.amazonaws.com/{registryKey}";
}
