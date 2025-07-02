// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json;
using ConsoleAppFramework;
using Documentation.Assembler.Deploying.Serialization;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Tooling.ExternalCommands;

namespace Documentation.Assembler.Deploying;

internal enum KvsOperation
{
	Puts,
	Deletes
}

public class AwsCloudFrontKeyValueStoreProxy(DiagnosticsCollector collector, IDirectoryInfo workingDirectory) : ExternalCommandExecutor(collector, workingDirectory)
{
	public void UpdateRedirects(string kvsName, IReadOnlyDictionary<string, string> sourcedRedirects)
	{
		var (kvsArn, eTag) = DescribeKeyValueStore(kvsName);
		if (string.IsNullOrEmpty(kvsArn) || string.IsNullOrEmpty(eTag))
			return;

		var existingRedirects = ListAllKeys(kvsArn);

		var toPut = sourcedRedirects
			.Select(kvp => new PutKeyRequestListItem { Key = kvp.Key, Value = kvp.Value });
		var toDelete = existingRedirects
			.Except(sourcedRedirects.Keys)
			.Select(k => new DeleteKeyRequestListItem { Key = k });

		eTag = ProcessBatchUpdates(kvsArn, eTag, toPut, KvsOperation.Puts);
		_ = ProcessBatchUpdates(kvsArn, eTag, toDelete, KvsOperation.Deletes);
	}

	private (string? Arn, string? ETag) DescribeKeyValueStore(string kvsName)
	{
		ConsoleApp.Log("Describing KeyValueStore");
		try
		{
			var json = CaptureMultiple("aws", "cloudfront", "describe-key-value-store", "--name", kvsName);
			var describeResponse = JsonSerializer.Deserialize<DescribeKeyValueStoreResponse>(string.Concat(json), AwsCloudFrontKeyValueStoreJsonContext.Default.DescribeKeyValueStoreResponse);
			if (describeResponse?.ETag is not null && describeResponse.KeyValueStore is { ARN.Length: > 0 })
				return (describeResponse.KeyValueStore.ARN, describeResponse.ETag);

			Collector.EmitError("", "Could not deserialize the DescribeKeyValueStoreResponse");
			return (null, null);
		}
		catch (Exception e)
		{
			Collector.EmitError("", "An error occurred while describing the KeyValueStore", e);
			return (null, null);
		}
	}

	private HashSet<string> ListAllKeys(string kvsArn)
	{
		ConsoleApp.Log("Acquiring existing redirects");
		var allKeys = new HashSet<string>();
		string[] baseArgs = ["cloudfront-keyvaluestore", "list-keys", "--kvs-arn", kvsArn];
		string? nextToken = null;
		try
		{
			do
			{
				var json = CaptureMultiple("aws", [.. baseArgs, .. nextToken is not null ? (string[])["--starting-token", nextToken] : []]);
				var response = JsonSerializer.Deserialize<ListKeysResponse>(string.Concat(json), AwsCloudFrontKeyValueStoreJsonContext.Default.ListKeysResponse);

				if (response?.Items != null)
				{
					foreach (var item in response.Items)
						_ = allKeys.Add(item.Key);
				}

				nextToken = response?.NextToken;
			} while (!string.IsNullOrEmpty(nextToken));
		}
		catch (Exception e)
		{
			Collector.EmitError("", "An error occurred while acquiring existing redirects in the KeyValueStore", e);
			return [];
		}
		return allKeys;
	}


	private string ProcessBatchUpdates(
		string kvsArn,
		string eTag,
		IEnumerable<object> items,
		KvsOperation operation)
	{
		const int batchSize = 50;
		ConsoleApp.Log($"Processing {items.Count()} items in batches of {batchSize} for {operation} update operation.");
		try
		{
			foreach (var batch in items.Chunk(batchSize))
			{
				var payload = operation switch
				{
					KvsOperation.Puts => JsonSerializer.Serialize(batch.Cast<PutKeyRequestListItem>().ToList(),
						AwsCloudFrontKeyValueStoreJsonContext.Default.ListPutKeyRequestListItem),
					KvsOperation.Deletes => JsonSerializer.Serialize(batch.Cast<DeleteKeyRequestListItem>().ToList(),
						AwsCloudFrontKeyValueStoreJsonContext.Default.ListDeleteKeyRequestListItem),
					_ => string.Empty
				};
				var responseJson = CaptureMultiple(false, 1, "aws", "cloudfront-keyvaluestore", "update-keys", "--kvs-arn", kvsArn, "--if-match", eTag,
					$"--{operation.ToString().ToLowerInvariant()}", "--payload", payload);
				var updateResponse = JsonSerializer.Deserialize<UpdateKeysResponse>(string.Concat(responseJson), AwsCloudFrontKeyValueStoreJsonContext.Default.UpdateKeysResponse);

				if (string.IsNullOrEmpty(updateResponse?.ETag))
					throw new Exception("Failed to get new ETag after update operation.");

				eTag = updateResponse.ETag;
			}
		}
		catch (Exception e)
		{
			Collector.EmitError("", $"An error occurred while performing a {operation} update to the KeyValueStore", e);
		}
		return eTag;
	}
}
