// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Changelogs;

public class ChangelogEntryYamlTests
{
	[Test]
	[Arguments("\n")]
	[Arguments("\r\n")]
	public void WithoutSchemaComments_StripsTheFieldDocumentation_WhateverTheLineEnding(string newline)
	{
		var yaml = string.Join(newline, "# subtype: one of", "#   a", "", "title: Feature", "type: feature", "");
		yaml.WithoutSchemaComments().Should().MatchInlineSnapshot("""
			title: Feature
			type: feature
			""");
	}
}
