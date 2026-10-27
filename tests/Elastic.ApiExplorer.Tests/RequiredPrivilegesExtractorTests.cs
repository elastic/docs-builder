// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Operations;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class RequiredPrivilegesExtractorTests
{
	[Test]
	public void ExtractFromDescription_ElasticsearchBlock_ExtractsRowsAndStripsIt()
	{
		const string description =
			"Returns node info.\n\n## Required authorization\n\n* Cluster privileges: `monitor`\n* Index privileges: `read`,`view_index_metadata`\n";

		var result = RequiredPrivilegesExtractor.ExtractFromDescription(description);

		result.Rows.Select(r => r.Label).Should().Equal("Cluster privileges", "Index privileges");
		result.Rows[0].Values.Should().Equal("monitor");
		result.Rows[1].Values.Should().Equal("read", "view_index_metadata");
		result.Description.Should().Be("Returns node info.");
	}

	[Test]
	public void ExtractFromDescription_ElasticsearchBlockFollowedByHeading_KeepsTheLaterSection()
	{
		const string description = "Intro.\n\n## Required authorization\n\n* Cluster privileges: `manage`\n\n## Notes\n\nKeep me.";

		var result = RequiredPrivilegesExtractor.ExtractFromDescription(description);

		result.Rows.Should().ContainSingle();
		result.Description.Should().Contain("## Notes").And.Contain("Keep me.").And.NotContain("Required authorization");
	}

	[Test]
	public void ExtractFromDescription_KibanaOrLine_SplitsAlternativesAndStripsLine()
	{
		const string description =
			"Get an agent policy.<br/><br/>[Required authorization] Route required privileges: fleet-agent-policies-read OR fleet-settings-read.";

		var result = RequiredPrivilegesExtractor.ExtractFromDescription(description);

		result.Rows.Should().ContainSingle();
		result.Rows[0].Label.Should().Be("Privileges");
		result.Rows[0].Values.Should().Equal("fleet-agent-policies-read", "fleet-settings-read");
		result.Rows[0].Join.Should().Be("or");
		result.Description.Should().Be("Get an agent policy.");
	}

	[Test]
	public void ExtractFromDescription_KibanaAndLine_JoinsWithAnd()
	{
		const string description = "[Required authorization] Route required privileges: fleet-agent-policies-read AND fleet-setup.";

		var result = RequiredPrivilegesExtractor.ExtractFromDescription(description);

		result.Rows[0].Values.Should().Equal("fleet-agent-policies-read", "fleet-setup");
		result.Rows[0].Join.Should().Be("and");
		result.Description.Should().BeEmpty();
	}

	[Test]
	public void ExtractFromDescription_KibanaSinglePrivilege_HasNoJoin()
	{
		const string description = "Delete a source.<br/><br/>[Required authorization] Route required privileges: fleet-settings-all.";

		var result = RequiredPrivilegesExtractor.ExtractFromDescription(description);

		result.Rows[0].Values.Should().Equal("fleet-settings-all");
		result.Rows[0].Join.Should().BeNull();
	}

	[Test]
	public void ExtractFromDescription_KibanaMixedAndOr_LeavesDescriptionUntouched()
	{
		const string description = "Do it.<br/><br/>[Required authorization] Route required privileges: a OR b AND c.";

		var result = RequiredPrivilegesExtractor.ExtractFromDescription(description);

		result.Rows.Should().BeEmpty();
		result.Description.Should().Be(description);
	}

	[Test]
	public void ExtractFromDescription_NoAuthorizationText_ReturnsDescriptionUnchanged()
	{
		const string description = "Plain description.";

		var result = RequiredPrivilegesExtractor.ExtractFromDescription(description);

		result.Rows.Should().BeEmpty();
		result.Description.Should().Be(description);
	}

	[Test]
	public void ExtractFromDescription_UnparseableElasticsearchBullet_LeavesBlockInPlace()
	{
		const string description = "Intro.\n\n## Required authorization\n\n* You need the right role.\n";

		var result = RequiredPrivilegesExtractor.ExtractFromDescription(description);

		result.Rows.Should().BeEmpty();
		result.Description.Should().Be(description);
	}

	[Test]
	public async Task Extract_XReqAuth_WinsOverDescriptionAndStillStripsTheBlock()
	{
		var json = /*lang=json,strict*/
			"""
			{
			  "openapi": "3.0.0",
			  "info": { "title": "t", "version": "1" },
			  "paths": {
			    "/a": {
			      "get": {
			        "operationId": "op-a",
			        "responses": { "200": { "description": "ok" } },
			        "x-req-auth": [ "Cluster privileges: `monitor`\n", "Needs the **admin** role" ]
			      }
			    }
			  }
			}
			""";
		var path = Path.Join(Path.GetTempPath(), $"privileges-{Guid.NewGuid():N}.json");
		try
		{
			await File.WriteAllTextAsync(path, json, TestContext.Current!.Execution.CancellationToken);
			var loaded = await OpenApiDocument.LoadAsync(
				path,
				new Microsoft.OpenApi.Reader.OpenApiReaderSettings(),
				TestContext.Current!.Execution.CancellationToken
			);
			var op = loaded.Document!.Paths!["/a"].Operations![HttpMethod.Get]!;

			var result = RequiredPrivilegesExtractor.Extract(
				op,
				"Intro.\n\n## Required authorization\n\n* Index privileges: `read`\n",
				null,
				"/a"
			);

			result.Rows.Select(r => r.Label).Should().Equal("Cluster privileges", "Needs the **admin** role");
			result.Rows[0].Values.Should().Equal("monitor");
			result.Rows[1].Values.Should().BeEmpty();
			result.Description.Should().Be("Intro.");
		}
		finally
		{
			if (File.Exists(path))
				File.Delete(path);
		}
	}

	[Test]
	public void ExtractFromDescription_BulletWithQualifiers_LeavesBlockInPlace()
	{
		const string description = "Intro.\n\n## Required authorization\n\n* Index privileges: `read` or `all` on the target index\n";

		var result = RequiredPrivilegesExtractor.ExtractFromDescription(description);

		result.Rows.Should().BeEmpty();
		result.Description.Should().Be(description);
	}

	[Test]
	public void ExtractFromDescription_BulletWithCommaSeparatedValues_StillParses()
	{
		const string description = "Intro.\n\n## Required authorization\n\n* Index privileges: `read`, `view_index_metadata`\n";

		var result = RequiredPrivilegesExtractor.ExtractFromDescription(description);

		result.Rows.Should().ContainSingle().Which.Values.Should().Equal("read", "view_index_metadata");
	}

	[Test]
	[Arguments(null, "Create a view", "Create a view")]
	[Arguments("", "Create a view", "Create a view")]
	[Arguments("  ", "Create a view", "Create a view")]
	[Arguments("Creates a view.", "Create a view", "Creates a view.")]
	public void DescriptionOrSummary_FallsBackToTheSummaryOnlyWhenTheDescriptionIsEmpty(
		string? description,
		string summary,
		string expected
	) => OperationViewModel.DescriptionOrSummary(description, summary).Should().Be(expected);
}
