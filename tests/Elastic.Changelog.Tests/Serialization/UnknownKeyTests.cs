// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.ReleaseNotes;
using SharpYaml;

namespace Elastic.Changelog.Tests.Serialization;

public class UnknownKeyTests
{
	[Test]
	public void DeserializeEntry_UnknownKey_ThrowsWithLocationAndSuggestion()
	{
		// language=yaml
		const string yaml = """
			title: A feature
			type: feature
			desciption: typo
			""";

		var act = () => ReleaseNotesSerialization.DeserializeEntry(yaml, "changelog/123.yaml");

		var exception = act.Should().Throw<YamlException>().Which;
		exception.Message.Should().Contain("(3,1)").And.Contain("Unknown key 'desciption'").And.Contain("Did you mean 'description'?");
	}

	[Test]
	public void DeserializeBundle_UnknownNestedKey_ThrowsListingEveryUnknownKey()
	{
		// language=yaml
		const string yaml =
			"""
			products:
			  - product: elasticsearch
			    taget: 9.3.0
			entries:
			  - title: Thing
			    tyep: feature
			""";

		var act = () => ReleaseNotesSerialization.DeserializeBundle(yaml, "bundles/9.3.0.yaml");

		var exception = act.Should().Throw<YamlException>().Which;
		exception.Message.Should().Contain("Unknown key 'taget' in $.products[0]").And.Contain("Did you mean 'target'?");
		exception.Message.Should().Contain("Unknown key 'tyep'").And.Contain("Did you mean 'type'?");
	}

	[Test]
	public void DeserializeEntry_MultilineBlockAtEndOfFileWithoutTrailingNewline_HasNoStrayNul()
	{
		const string yaml = "title: A feature\ntype: feature\ndescription: |\n  line one\n  line two";

		var entry = ReleaseNotesSerialization.DeserializeEntry(yaml);

		entry.Description.Should().NotContain("\0").And.Contain("line two");
	}
}
