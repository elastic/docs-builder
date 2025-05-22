// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Documentation.Links;

namespace Elastic.Documentation.LinkIndex;

public class Aws3LinkIndexReader(IAmazonS3 s3Client, string bucketName = "elastic-docs-link-index", string registryKey = "link-index.json") : ILinkIndexReader
{

	// <summary>
	// Using <see cref="AnonymousAWSCredentials"/> to access the link index
	// allows to read from the link index without the need to provide AWS credentials.
	// </summary>
	public static Aws3LinkIndexReader CreateAnonymous()
	{
		var credentials = new AnonymousAWSCredentials();
		var config = new AmazonS3Config
		{
			RegionEndpoint = Amazon.RegionEndpoint.USEast2
		};
		var s3Client = new AmazonS3Client(credentials, config);
		return new AwsS3LinkIndexReaderWriter(s3Client);
	}

	public async Task<LinkRegistry> GetRegistry(Cancel cancellationToken = default)
	{
		var getObjectRequest = new GetObjectRequest
		{
			BucketName = bucketName,
			Key = registryKey
		};
		var getObjectResponse = await s3Client.GetObjectAsync(getObjectRequest, cancellationToken);
		await using var stream = getObjectResponse.ResponseStream;
		var linkIndex = LinkRegistry.Deserialize(stream);
		return linkIndex with { ETag = getObjectResponse.ETag };
	}
	public async Task<RepositoryLinks> GetRepositoryLinks(string key, Cancel cancellationToken)
	{
		var getObjectRequest = new GetObjectRequest
		{
			BucketName = bucketName,
			Key = key
		};
		var getObjectResponse = await s3Client.GetObjectAsync(getObjectRequest, cancellationToken);
		await using var stream = getObjectResponse.ResponseStream;
		return RepositoryLinks.Deserialize(stream);
	}

	public string RegistryUrl { get; } = $"https://{bucketName}.s3.{s3Client.Config.RegionEndpoint.SystemName}.amazonaws.com/{registryKey}";
}
