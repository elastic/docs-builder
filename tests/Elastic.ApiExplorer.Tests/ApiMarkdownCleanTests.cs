// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Tests;

public class ApiMarkdownCleanTests
{
	private const string BumpSearchDescription =
		"""
		**All methods and paths for this operation:**

		<div>
		  <span class="operation-verb get">GET</span>
		  <span class="operation-path">/_search</span>
		  </div>
		<div>
		  <span class="operation-verb post">POST</span>
		  <span class="operation-path">/_search</span>
		  </div>
		<div>
		  <span class="operation-verb get">GET</span>
		  <span class="operation-path">/{index}/_search</span>
		  </div>
		<div>
		  <span class="operation-verb post">POST</span>
		  <span class="operation-path">/{index}/_search</span>
		  </div>

		Returns hits that match the query defined in the request.

		IMPORTANT: The same point-in-time ID should be used for all slices.
		""";

	[Fact]
	public void Clean_Null_ReturnsEmpty() => ApiMarkdown.Clean(null).Should().BeEmpty();

	[Fact]
	public void Clean_Empty_ReturnsEmpty() => ApiMarkdown.Clean("").Should().BeEmpty();

	[Fact]
	public void Clean_BumpIslandAndImportant_StripsHtmlAndRewritesAdmonition()
	{
		var cleaned = ApiMarkdown.Clean(BumpSearchDescription);

		cleaned.Should().NotContain("<div>");
		cleaned.Should().NotContain("operation-verb");
		cleaned.Should().NotContain("operation-path");
		cleaned.Should().NotContain("All methods and paths");
		cleaned.Should().Contain("Returns hits that match the query");
		cleaned.Should().Contain(":::{important}");
		cleaned.Should().Contain("The same point-in-time ID should be used for all slices.");
		cleaned.Should().NotContain("- **GET**");
	}

	[Fact]
	public void Clean_AlreadyClean_IsIdempotent()
	{
		var cleaned = ApiMarkdown.Clean(BumpSearchDescription);

		ApiMarkdown.Clean(cleaned).Should().Be(cleaned);
	}

	[Fact]
	public void Clean_NoteAndWarning_RewritesToMyst()
	{
		var cleaned = ApiMarkdown.Clean("NOTE: Check the index name.\n\nWARNING: This deletes data.");

		cleaned.Should().Contain(":::{note}");
		cleaned.Should().Contain("Check the index name.");
		cleaned.Should().Contain(":::{warning}");
		cleaned.Should().Contain("This deletes data.");
	}

	[Fact]
	public void Clean_MidSentenceImportant_LeavesProse()
	{
		var cleaned = ApiMarkdown.Clean("This is IMPORTANT: keep the marker as prose.");

		cleaned.Should().Be("This is IMPORTANT: keep the marker as prose.");
		cleaned.Should().NotContain(":::{important}");
	}

	[Fact]
	public void Clean_Caution_MapsToWarning()
	{
		var cleaned = ApiMarkdown.Clean("CAUTION: Do not run this in production.");

		cleaned.Should().Contain(":::{warning}");
		cleaned.Should().Contain("Do not run this in production.");
		cleaned.Should().NotContain(":::{caution}");
	}

	[Fact]
	public void Prepare_BumpHtml_DoesNotStrip()
	{
		var prepared = ApiMarkdown.Prepare(BumpSearchDescription, "/api/doc/elasticsearch");

		prepared.Should().Contain("operation-verb");
		prepared.Should().Contain("**All methods and paths for this operation:**");
	}
}
