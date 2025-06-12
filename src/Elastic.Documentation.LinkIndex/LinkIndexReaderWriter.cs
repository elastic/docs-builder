// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Documentation.Links;

namespace Elastic.Documentation.LinkIndex;

public class AwsS3LinkIndexReaderWriter(
	IAmazonS3 s3Client,
	string bucketName = "elastic-docs-link-index",
	string registryKey = "link-index.json"
) : Aws3LinkIndexReader(s3Client, bucketName, registryKey), ILinkIndexReaderWriter
{
	private readonly IAmazonS3 _s3Client = s3Client;
	private readonly string _bucketName = bucketName;
	private readonly string _registryKey = registryKey;

	public async Task SaveRegistry(LinkRegistry registry, Cancel cancellationToken = default)
	{
		if (registry.ETag == null)
			// The ETag should not be null if the LinkReferenceRegistry was retrieved from GetLinkIndex()
			throw new InvalidOperationException($"{nameof(LinkRegistry)}.{nameof(registry.ETag)} cannot be null");
		var json = LinkRegistry.Serialize(registry);
		var putObjectRequest = new PutObjectRequest
		{
			BucketName = _bucketName,
			Key = _registryKey,
			ContentBody = json,
			ContentType = "application/json",
			IfMatch = registry.ETag // Only update if the ETag matches. Meaning the object has not been changed in the meantime.
		};
		var putResponse = await _s3Client.PutObjectAsync(putObjectRequest, cancellationToken);
		if (putResponse.HttpStatusCode != HttpStatusCode.OK)
			throw new Exception($"Unable to save {nameof(LinkRegistry)} to s3://{_bucketName}/{_registryKey}");
	}
}
