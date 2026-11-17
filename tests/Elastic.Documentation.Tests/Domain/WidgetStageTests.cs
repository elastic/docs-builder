// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Documentation.Tests.Domain;

public class WidgetStageTests
{
	[Test]
	public void Draft_NeedsOnlyAName()
	{
		((WidgetStage)new DraftWidget("")).RuleIds().Should().Equal("name-required");
		((WidgetStage)new DraftWidget("w")).RuleIds().Should().BeEmpty();
	}

	[Test]
	public void Published_NeedsADescriptionToo() =>
		((WidgetStage)new PublishedWidget("w", "")).RuleIds().Should().Equal("description-required");

	[Test]
	public void SameName_IsValidAsADraftAndInvalidAsPublished()
	{
		((WidgetStage)new DraftWidget("w")).RuleIds().Should().BeEmpty();
		((WidgetStage)new PublishedWidget("w", "")).RuleIds().Should().NotBeEmpty();
	}
}
