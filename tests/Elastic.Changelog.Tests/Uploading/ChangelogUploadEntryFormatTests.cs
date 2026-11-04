// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Tests.Changelogs;
using Elastic.Changelog.Tests.Reconciliation;
using Elastic.Changelog.Uploading;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Uploading;

/// <summary>Uploads of a repository that sets <c>entry_format</c> in its changelog.yml.</summary>
public class ChangelogUploadEntryFormatTests() : ChangelogTestBase()
{
	[Test]
	public async Task Upload_DirectoryOverride_StillConvertsWithTheConfiguredFormat()
	{
		var root = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		var configPath = FileSystem.Path.Join(root, "changelog.yml");
		var entries = FileSystem.Path.Join(root, "entries");
		FileSystem.Directory.CreateDirectory(entries);
		var ct = TestContext.Current!.Execution.CancellationToken;
		await FileSystem.File.WriteAllTextAsync(configPath, "entry_format: elasticsearch\n", ct);
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Join(entries, "144630.yaml"),
			"pr: 144630\nsummary: Limit size of sql query\narea: SQL\ntype: bug\nissues: []\n",
			ct
		);

		var s3 = new FakeS3("test-bucket");
		var service = new ChangelogUploadService(NullLoggerFactory.Instance, FileSystem, ConfigurationContext, s3.Client);
		var args = new ChangelogUploadArguments
		{
			ArtifactType = ArtifactType.Changelog,
			Target = UploadTargetKind.S3,
			S3BucketName = "test-bucket",
			Config = configPath,
			Directory = entries,
			Owner = "elastic",
			Repo = "elasticsearch",
			Branch = "main"
		};

		var result = await service.Upload(Collector, args, ct);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		var put = s3.Puts.Should().ContainSingle().Subject;
		put.Key.Should().Be("changelog/elastic/elasticsearch/main/144630.yaml");
		var uploaded = ReleaseNotesSerialization.DeserializeEntry(put.ContentBody);
		uploaded.Title.Should().Be("Limit size of sql query");
		uploaded.Type.Should().Be(ChangelogEntryType.BugFix);
	}
}
