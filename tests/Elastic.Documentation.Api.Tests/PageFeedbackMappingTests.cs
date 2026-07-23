// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Api.Adapters.PageFeedback;

namespace Elastic.Documentation.Api.Tests;

public class PageFeedbackMappingTests
{
	[Fact]
	public void Mapping_EnvironmentProvided_GeneratesExpectedIndexAndFields()
	{
		var context = PageFeedbackMappingContext.PageFeedbackDocument.CreateContext(env: "staging");
		using var mapping = JsonDocument.Parse(context.GetMappingsJson());
		var root = mapping.RootElement;
		var properties = root.GetProperty("properties");

		context.IndexStrategy.Should().NotBeNull();
		context.IndexStrategy.WriteTarget.Should().Be("page-feedback-v1-staging");
		root.GetProperty("dynamic").GetBoolean().Should().BeFalse();
		properties.GetProperty("feedback_id").GetProperty("type").GetString().Should().Be("keyword");
		properties.GetProperty("page_url").GetProperty("ignore_above").GetInt32().Should().Be(2048);
		properties.GetProperty("page_title").GetProperty("ignore_above").GetInt32().Should().Be(500);
		properties.GetProperty("reaction").GetProperty("type").GetString().Should().Be("keyword");
		properties.GetProperty("reason").GetProperty("type").GetString().Should().Be("keyword");
		properties.GetProperty("reason_set_version").GetProperty("type").GetString().Should().Be("integer");
		properties.GetProperty("comment").GetProperty("type").GetString().Should().Be("text");
		properties.GetProperty("euid").GetProperty("ignore_above").GetInt32().Should().Be(256);
		properties.GetProperty("@timestamp").GetProperty("type").GetString().Should().Be("date");
	}

	[Fact]
	public void Serialization_DetailsProvided_WritesQueryableReasonFields()
	{
		var document = new PageFeedbackDocument
		{
			FeedbackId = Guid.NewGuid().ToString(),
			PageUrl = "/docs/test-page",
			PageTitle = "Test page",
			Reaction = "thumbsUp",
			Reason = "helpfulExamples",
			ReasonSetVersion = 2,
			Timestamp = DateTimeOffset.UtcNow
		};

		var json = JsonSerializer.Serialize(document, PageFeedbackJsonContext.Default.PageFeedbackDocument);
		using var serialized = JsonDocument.Parse(json);

		serialized.RootElement.GetProperty("reason").GetString().Should().Be("helpfulExamples");
		serialized.RootElement.GetProperty("reason_set_version").GetInt32().Should().Be(2);
	}
}
