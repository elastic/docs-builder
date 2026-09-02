// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Documentation.OpenApiIndex.Tests;

public class VersionIndexBuilderTests
{
	[Fact]
	public void Build_SingleVersion_CreatesMajorEntry()
	{
		var index = VersionIndexBuilder.Build(["elastic/elasticsearch/8.16/openapi.json"]).Index;

		index.Should().ContainKey("elastic/elasticsearch");
		index["elastic/elasticsearch"]["openapi.json"]["8"].Version.Should().Be("8.16");
	}

	[Fact]
	public void Build_NewMajorAddedToExistingIndex_CreatesSeparateEntry()
	{
		var index = VersionIndexBuilder.Build(["elastic/elasticsearch/8.16/openapi.json", "elastic/elasticsearch/9.0/openapi.json"]).Index;

		var byMajor = index["elastic/elasticsearch"]["openapi.json"];
		byMajor.Should().HaveCount(2);
		byMajor["8"].Version.Should().Be("8.16");
		byMajor["9"].Version.Should().Be("9.0");
	}

	[Fact]
	public void Build_MinorBumpWithinExistingMajor_KeepsHighestMinor()
	{
		var index = VersionIndexBuilder.Build(["elastic/elasticsearch/8.16/openapi.json", "elastic/elasticsearch/8.17/openapi.json"]).Index;

		index["elastic/elasticsearch"]["openapi.json"]["8"].Version.Should().Be("8.17");
	}

	[Fact]
	public void Build_OutOfOrderArrival_HigherMinorListedBeforeLower_KeepsHighestMinor()
	{
		var index = VersionIndexBuilder.Build(["elastic/elasticsearch/8.17/openapi.json", "elastic/elasticsearch/8.16/openapi.json"]).Index;

		index["elastic/elasticsearch"]["openapi.json"]["8"].Version.Should().Be("8.17");
	}

	[Fact]
	public void Build_MainVersion_CreatesMainEntry()
	{
		var index = VersionIndexBuilder.Build(["elastic/elasticsearch/main/openapi.json"]).Index;

		index["elastic/elasticsearch"]["openapi.json"]["main"].Version.Should().Be("main");
	}

	[Fact]
	public void Build_MainAndReleaseVersions_KeepsBothSeparately()
	{
		var index = VersionIndexBuilder.Build(["elastic/elasticsearch/main/openapi.json", "elastic/elasticsearch/8.16/openapi.json"]).Index;

		var byMajor = index["elastic/elasticsearch"]["openapi.json"];
		byMajor.Should().HaveCount(2);
		byMajor["main"].Version.Should().Be("main");
		byMajor["8"].Version.Should().Be("8.16");
	}

	[Fact]
	public void Build_MultipleSpecFilesInSameRepo_IndexesEachIndependently()
	{
		// Two spec files from one repo may share a version: they are separate objects in the bucket.
		var index = VersionIndexBuilder.Build(["elastic/kibana/8.16/kibana.json", "elastic/kibana/8.16/kibana-serverless.json"]).Index;

		var byFile = index["elastic/kibana"];
		byFile.Should().HaveCount(2);
		byFile["kibana.json"]["8"].Version.Should().Be("8.16");
		byFile["kibana-serverless.json"]["8"].Version.Should().Be("8.16");
	}

	[Fact]
	public void Build_MultipleRepos_KeepsSeparateEntriesPerRepo()
	{
		var index = VersionIndexBuilder.Build(["elastic/elasticsearch/8.16/openapi.json", "elastic/kibana/8.16/kibana.json"]).Index;

		index.Should().HaveCount(2);
		index["elastic/elasticsearch"]["openapi.json"]["8"].Version.Should().Be("8.16");
		index["elastic/kibana"]["kibana.json"]["8"].Version.Should().Be("8.16");
	}

	[Fact]
	public void Build_EmptyKeys_ReturnsEmptyIndex() => VersionIndexBuilder.Build([]).Index.Should().BeEmpty();

	[Theory]
	[InlineData("elastic/elasticsearch/openapi.json")] // missing version segment

	[InlineData("elastic/elasticsearch/8.16/nested/openapi.json")] // too many segments

	[InlineData("elastic//8.16/openapi.json")] // empty repo segment

	[InlineData("elastic/elasticsearch/8.16/")] // empty file segment

	[InlineData("elastic/elasticsearch/master/openapi.json")] // not "main" or <major>.<minor>

	[InlineData("elastic/elasticsearch/8/openapi.json")] // missing minor

	[InlineData("elastic/elasticsearch/8./openapi.json")] // missing minor after the dot

	[InlineData("elastic/elasticsearch/8.x/openapi.json")] // non-numeric minor

	[InlineData("elastic/elasticsearch/.16/openapi.json")] // missing major

	[InlineData("elastic/elasticsearch/+8.16/openapi.json")] // signed major

	public void Build_KeyOfUnexpectedShape_IsReportedAndSkipped(string key)
	{
		var (index, invalidKeys) = VersionIndexBuilder.Build([key]);

		index.Should().BeEmpty();
		invalidKeys.Should().ContainSingle().Which.Should().Be(key);
	}

	[Fact]
	public void Build_MixOfValidAndInvalidKeys_IndexesValidAndReportsInvalidOnly()
	{
		var (index, invalidKeys) = VersionIndexBuilder.Build([
			"elastic/elasticsearch/8.16/openapi.json",
			"not-a-valid-key",
			"elastic/elasticsearch/master/openapi.json"
		]);

		index["elastic/elasticsearch"]["openapi.json"]["8"].Version.Should().Be("8.16");
		invalidKeys.Should().BeEquivalentTo(["not-a-valid-key", "elastic/elasticsearch/master/openapi.json"]);
	}
}
