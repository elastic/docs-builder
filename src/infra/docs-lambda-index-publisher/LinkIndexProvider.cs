// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Documentation.Links;

namespace Elastic.Documentation.Lambda.LinkIndexUploader;

/// <summary>
/// Gets the link index from S3 once.
/// You can then update the link index with <see cref="UpdateLinkIndexEntry(LinkRegistryEntry)"/> and save it with <see cref="Save()"/>.
/// If the link index changed in the meantime, <see cref="Save()"/> will throw an exception,
/// thus all the messages from the queue will be sent back to the queue.
/// </summary>
public class LinkIndexProvider(IAmazonS3 s3Client, ILambdaLogger logger, string bucketName, string key)
{
	private string? _etag;
	private LinkReferenceRegistry? _linkIndex;

	private async Task<LinkReferenceRegistry> GetLinkIndex()
	{
		var getObjectRequest = new GetObjectRequest
		{
			BucketName = bucketName,
			Key = key
		};
		logger.LogInformation("Getting link index from s3://{bucketName}/{key}", bucketName, key);
		var getObjectResponse = await s3Client.GetObjectAsync(getObjectRequest);
		await using var stream = getObjectResponse.ResponseStream;
		_etag = getObjectResponse.ETag;
		logger.LogInformation("Successfully got link index from s3://{bucketName}/{key}", bucketName, key);
		_linkIndex = LinkReferenceRegistry.Deserialize(stream);
		return _linkIndex;
	}

	public async Task UpdateLinkIndexEntry(LinkRegistryEntry newEntry)
	{
		_linkIndex ??= await GetLinkIndex();
		var repository = newEntry.Repository;
		var branch = newEntry.Branch;
		// repository already exists in links.json
		if (_linkIndex.Repositories.TryGetValue(repository, out var existingRepositoryEntry))
		{
			// The branch already exists in the repository entry
			if (existingRepositoryEntry.TryGetValue(branch, out var existingBranchEntry))
			{
				if (newEntry.UpdatedAt > existingBranchEntry.UpdatedAt)
				{
					existingRepositoryEntry[branch] = newEntry;
					logger.LogInformation("Updated existing entry for {repository}@{branch}", repository, branch);
				}
				else
					logger.LogInformation("Skipping update for {repository}@{branch} because the existing entry is newer or equal", repository, branch);
			}
			// branch does not exist in the repository entry
			else
			{
				existingRepositoryEntry[branch] = newEntry;
				logger.LogInformation("Added new entry '{repository}@{branch}' to existing entry for '{repository}'", repository, branch, repository);
			}
		}
		// onboarding new repository
		else
		{
			_linkIndex.Repositories.Add(repository, new Dictionary<string, LinkRegistryEntry>
			{
				{ branch, newEntry }
			});
			logger.LogInformation("Added new entry for {repository}@{branch}", repository, branch);
		}
	}

	public async Task Save()
	{
		if (_etag == null || _linkIndex == null)
			throw new InvalidOperationException("You must call UpdateLinkIndexEntry() before Save()");
		var json = LinkReferenceRegistry.Serialize(_linkIndex);
		logger.LogInformation("Saving link index to s3://{bucketName}/{key}", bucketName, key);
		var putObjectRequest = new PutObjectRequest
		{
			BucketName = bucketName,
			Key = key,
			ContentBody = json,
			ContentType = "application/json",
			IfMatch = _etag // Only update if the ETag matches. Meaning the object has not been changed in the meantime.
		};
		var putReponse = await s3Client.PutObjectAsync(putObjectRequest);
		if (putReponse.HttpStatusCode == HttpStatusCode.OK)
			logger.LogInformation("Successfully saved link index to s3://{bucketName}/{key}", bucketName, key);
		else
			// TODO retry?
			logger.LogError("Unable to save index to s3://{bucketName}/{key}", bucketName, key);
	}
}
