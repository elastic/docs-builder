// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Reflection;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Lambda.ChangelogScrubber;
using Elastic.Documentation.ReleaseNotes;

var publicBucketName = Environment.GetEnvironmentVariable("PUBLIC_BUCKET_NAME")
	?? throw new InvalidOperationException("PUBLIC_BUCKET_NAME environment variable is required");

var allowRepos = BuildAllowlist();

await LambdaBootstrapBuilder
	.Create<SQSEvent, SQSBatchResponse>(Handler, new SourceGeneratorLambdaJsonSerializer<SerializerContext>())
	.Build()
	.RunAsync();

return;

IReadOnlyList<string> BuildAllowlist()
{
	using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("assembler.yml")
		?? throw new InvalidOperationException("Embedded assembler.yml not found");
	using var reader = new StreamReader(stream);
	var yaml = reader.ReadToEnd();
	var assembly = AssemblyConfiguration.Deserialize(yaml, skipPrivateRepositories: false);
	return LinkAllowlistSanitizer.BuildAllowReposFromAssembler(assembly);
}

async Task<SQSBatchResponse> Handler(SQSEvent ev, ILambdaContext context)
{
	using var s3Client = new AmazonS3Client();
	var batchItemFailures = new List<SQSBatchResponse.BatchItemFailure>();

	foreach (var message in ev.Records)
	{
		try
		{
			var s3Event = S3EventNotification.ParseJson(message.Body);
			foreach (var record in s3Event.Records)
			{
				var key = Uri.UnescapeDataString(record.S3.Object.Key.Replace('+', ' '));
				var sourceBucket = record.S3.Bucket.Name;
				var eventName = record.EventName;

				context.Logger.LogInformation("Processing event={EventName} key={Key}", eventName, key);

				if (eventName.Value.Contains("ObjectRemoved"))
				{
					await DeleteFromPublicBucket(s3Client, key, context);
				}
				else if (eventName.Value.Contains("ObjectCreated"))
				{
					await ScrubAndCopyToPublicBucket(s3Client, sourceBucket, key, context);
				}
				else
				{
					context.Logger.LogWarning("Ignoring unhandled event type: {EventName}", eventName);
				}
			}
		}
		catch (Exception e)
		{
			context.Logger.LogWarning(e, "Failed to process message {MessageId}", message.MessageId);
			batchItemFailures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
		}
	}

	var response = new SQSBatchResponse(batchItemFailures);
	if (batchItemFailures.Count > 0)
		context.Logger.LogInformation("Failed {FailedCount} of {TotalCount} messages", batchItemFailures.Count, ev.Records.Count);

	var jsonStr = JsonSerializer.Serialize(response, SerializerContext.Default.SQSBatchResponse);
	context.Logger.LogInformation(jsonStr);
	return response;
}

async Task DeleteFromPublicBucket(IAmazonS3 s3Client, string key, ILambdaContext context)
{
	try
	{
		_ = await s3Client.DeleteObjectAsync(new DeleteObjectRequest
		{
			BucketName = publicBucketName,
			Key = key
		});
		context.Logger.LogInformation("Deleted {Key} from public bucket", key);
	}
	catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
	{
		context.Logger.LogInformation("Key {Key} already absent from public bucket", key);
	}
}

async Task ScrubAndCopyToPublicBucket(IAmazonS3 s3Client, string sourceBucket, string key, ILambdaContext context)
{
	var fileName = Path.GetFileName(key);
	if (string.Equals(fileName, "registry-index.json", StringComparison.OrdinalIgnoreCase))
	{
		await CopyPassThrough(s3Client, sourceBucket, key, context);
		return;
	}

	if (key.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
	{
		context.Logger.LogWarning("Skipping unapproved JSON key: {Key}", key);
		return;
	}

	if (!key.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) &&
		!key.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
	{
		context.Logger.LogInformation("Skipping non-YAML key: {Key}", key);
		return;
	}

	var getResponse = await s3Client.GetObjectAsync(new GetObjectRequest
	{
		BucketName = sourceBucket,
		Key = key
	});

	string content;
	using (var reader = new StreamReader(getResponse.ResponseStream))
		content = await reader.ReadToEndAsync();

	var scrubbed = await ScrubContent(key, content, context);

	_ = await s3Client.PutObjectAsync(new PutObjectRequest
	{
		BucketName = publicBucketName,
		Key = key,
		ContentBody = scrubbed,
		ContentType = "application/yaml"
	});

	context.Logger.LogInformation("Scrubbed and wrote {Key} to public bucket", key);
}

async Task CopyPassThrough(IAmazonS3 s3Client, string sourceBucket, string key, ILambdaContext context)
{
	_ = await s3Client.CopyObjectAsync(new CopyObjectRequest
	{
		SourceBucket = sourceBucket,
		SourceKey = key,
		DestinationBucket = publicBucketName,
		DestinationKey = key
	});
	context.Logger.LogInformation("Copied {Key} to public bucket (pass-through)", key);
}

async Task<string> ScrubContent(string key, string content, ILambdaContext context)
{
	var isBundlePath = key.Contains("/bundles/", StringComparison.OrdinalIgnoreCase);

	if (isBundlePath)
		return await ScrubBundle(content, context);

	return await ScrubChangelog(content, context);
}

async Task<string> ScrubBundle(string content, ILambdaContext context)
{
	var bundle = ReleaseNotesSerialization.DeserializeBundle(content);
	var owner = bundle.Products.Count > 0 ? bundle.Products[0].Owner ?? "elastic" : "elastic";
	var repo = bundle.Products.Count > 0 ? bundle.Products[0].Repo : null;

	await using var collector = new DiagnosticsCollector([]);
	if (!LinkAllowlistSanitizer.TryApplyBundle(collector, bundle, allowRepos, owner, repo, out var sanitized, out var changed))
		throw new InvalidOperationException($"Failed to apply allowlist to bundle; errors: {collector.Errors}");

	if (!changed)
	{
		context.Logger.LogInformation("Bundle had no private references, writing unchanged");
		LinkAllowlistSanitizer.ValidateNoPrivateReferences(content, allowRepos);
		return content;
	}

	sanitized = LinkAllowlistSanitizer.StripBundleSentinels(sanitized);
	var result = ReleaseNotesSerialization.SerializeBundle(sanitized);
	LinkAllowlistSanitizer.ValidateNoPrivateReferences(result, allowRepos);
	return result;
}

async Task<string> ScrubChangelog(string content, ILambdaContext context)
{
	var normalized = ReleaseNotesSerialization.NormalizeYaml(content);
	var entry = ReleaseNotesSerialization.DeserializeEntry(normalized);

	var bundledEntry = new BundledEntry
	{
		Type = entry.Type,
		Title = entry.Title,
		Description = entry.Description,
		Impact = entry.Impact,
		Action = entry.Action,
		Prs = entry.Prs,
		Issues = entry.Issues,
		Areas = entry.Areas,
		Highlight = entry.Highlight,
		Subtype = entry.Subtype
	};

	await using var collector = new DiagnosticsCollector([]);
	if (!LinkAllowlistSanitizer.TryApplyChangelogEntry(
		collector, bundledEntry, allowRepos, "elastic", null,
		out var sanitized, out var changed))
		throw new InvalidOperationException($"Failed to apply allowlist to changelog entry; errors: {collector.Errors}");

	if (!changed)
	{
		context.Logger.LogInformation("Changelog entry had no private references, writing unchanged");
		LinkAllowlistSanitizer.ValidateNoPrivateReferences(content, allowRepos);
		return content;
	}

	var scrubEntry = entry with
	{
		Description = sanitized.Description,
		Impact = sanitized.Impact,
		Action = sanitized.Action,
		Prs = sanitized.Prs,
		Issues = sanitized.Issues
	};

	var result = ReleaseNotesSerialization.SerializeEntry(scrubEntry);
	LinkAllowlistSanitizer.ValidateNoPrivateReferences(result, allowRepos);
	return result;
}
