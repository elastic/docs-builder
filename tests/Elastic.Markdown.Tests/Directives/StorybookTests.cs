// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.Storybook;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public abstract class StorybookRegistryTest(string content) : DirectiveTest<StorybookBlock>(content)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile("docs/docs_registry.json", new MockFileData(RegistryJson));

	protected override string? GetDocsetExtraYaml() => """
storybook:
  registry: docs_registry.json
""";

	private const string RegistryJson =
		/*lang=json,strict*/
		"""
		{
		  "schemaVersion": 1,
		  "producer": "kibana-storybook",
		  "baseUrl": "http://127.0.0.1:6007/storybook-docs",
		  "build": {
		    "commit": "abc123",
		    "branch": "storybook-to-docs"
		  },
		  "stories": {
		    "kibana:shared_ux:ai-components-aibutton--default": {
		      "alias": "shared_ux",
		      "docsId": "ai-components-aibutton--default",
		      "storybookId": "ai-components-aibutton--default",
		      "title": "ai-components/aibutton",
		      "name": "Default",
		      "height": 360,
		      "type": "story",
		      "renderMode": "inline",
		      "inline": {
		        "entry": "http://127.0.0.1:6007/storybook-docs/shared_ux/registry.js",
		        "bundleId": "shared_ux",
		        "bootstrap": {
		          "publicPath": "http://127.0.0.1:6007/storybook/shared_ux/",
		          "scripts": [
		            "http://127.0.0.1:6007/storybook/shared_ux/kbn-ui-shared-deps-npm.dll.js",
		            "http://127.0.0.1:6007/storybook/shared_ux/kbn-ui-shared-deps-src.js"
		          ],
		          "styles": [
		            "http://127.0.0.1:6007/storybook/shared_ux/kbn-ui-shared-deps-src.css",
		            "https://fonts.googleapis.com/css2?family=Inter:wght@300..700&display=swap"
		          ]
		        }
		      },
		      "iframe": {
		        "url": "http://127.0.0.1:6007/storybook/shared_ux/iframe.html?id=ai-components-aibutton--default&viewMode=story"
		      }
		    },
		    "kibana:shared_ux:components-callout--info": {
		      "alias": "shared_ux",
		      "docsId": "components-callout--info",
		      "storybookId": "components-callout--info-storybook",
		      "title": "components-callout",
		      "name": "Info",
		      "type": "story",
		      "renderMode": "iframe",
		      "iframe": {
		        "url": "http://127.0.0.1:6007/storybook/shared_ux/iframe.html?id=components-callout--info-storybook&viewMode=story"
		      }
		    }
		  }
		}
		""";
}

[InheritsTests]
public class StorybookInlineIdTests() : StorybookRegistryTest(
	"""
:::{storybook}
:id: kibana:shared_ux:ai-components-aibutton--default
:title: AI Button / Default story
:::
"""
)
{
	[Test]
	public void ResolvesStory()
	{
		Block!.Project.Should().Be("kibana");
		Block.Storybook.Should().Be("shared_ux");
		Block.DocsId.Should().Be("ai-components-aibutton--default");
		Block.StoryId.Should().Be("ai-components-aibutton--default");
		Block.InlineEntry.Should().Be("http://127.0.0.1:6007/storybook-docs/shared_ux/registry.js");
		Block
			.StoryUrl
			.Should()
			.Be("http://127.0.0.1:6007/storybook/shared_ux/iframe.html?id=ai-components-aibutton--default&viewMode=story");
		Block.Height.Should().Be(360);
	}

	[Test]
	public void RendersInlineStory()
	{
		Html.Should().Contain("<storybook-story");
		Html.Should().Contain("story-id=\"ai-components-aibutton--default\"");
		Html.Should().Contain("entry=\"http://127.0.0.1:6007/storybook-docs/shared_ux/registry.js\"");
		Html.Should().Contain("http://127.0.0.1:6007/storybook/shared_ux/kbn-ui-shared-deps-src.css");
		Html.Should().Contain("https://fonts.googleapis.com");
		Html.Should().NotContain("kibana:shared_ux:ai-components-aibutton--default");
	}
}

/// <summary>Deterministic <see cref="IEnvironmentVariables"/> so storybook interpolation tests don't depend on the host shell.</summary>
internal sealed class TestEnvironmentVariables : IEnvironmentVariables
{
	private readonly Dictionary<string, string?> _variables = [with(StringComparer.Ordinal)];

	public string? this[string name] { set => _variables[name] = value; }

	public string? GetEnvironmentVariable(string name) => _variables.GetValueOrDefault(name);

	public bool IsRunningOnCI => false;
}

[InheritsTests]
public class StorybookInterpolatedRegistryTests() : StorybookRegistryTest(
	"""
:::{storybook}
:id: kibana:shared_ux:ai-components-aibutton--default
:::
"""
)
{
	protected override IEnvironmentVariables? GetEnvironment() => new TestEnvironmentVariables();

	protected override string? GetDocsetExtraYaml() => """
storybook:
  registry: ${KIBANA_STORYBOOK_REGISTRY:-docs_registry.json}
""";

	[Test]
	public void ResolvesDefaultWhenEnvironmentVariableUnset() => Block!.StoryId.Should().Be("ai-components-aibutton--default");
}

[InheritsTests]
public class StorybookDisallowedRegistryVariableTests() : StorybookRegistryTest(
	"""
:::{storybook}
:id: kibana:shared_ux:ai-components-aibutton--default
:::
"""
)
{
	// A disallowed variable must not be read even when present in the environment.
	protected override IEnvironmentVariables? GetEnvironment() =>
		new TestEnvironmentVariables { ["AWS_SECRET_ACCESS_KEY"] = "super-secret" };

	protected override string? GetDocsetExtraYaml() => """
storybook:
  registry: ${AWS_SECRET_ACCESS_KEY:-docs_registry.json}
""";

	[Test]
	public void WarnsAndLeavesExpressionLiteral() =>
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Warning && d.Message.Contains("not allow-listed for interpolation"));
}

[InheritsTests]
public class StorybookStructuredReferenceTests() : StorybookRegistryTest(
	"""
:::{storybook}
:project: kibana
:storybook: shared_ux
:component: ai-components-aibutton
:story: default
:::
"""
)
{
	[Test]
	public void ResolvesComponentAndStory() => Block!.StoryId.Should().Be("ai-components-aibutton--default");
}

[InheritsTests]
public class StorybookStructuredReferenceWrongStorybookTests() : StorybookRegistryTest(
	"""
:::{storybook}
:project: kibana
:storybook: content_management
:story: ai-components-aibutton--default
:::
"""
)
{
	[Test]
	public void DoesNotFallbackToAnotherStorybook() =>
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Message.Contains("does not contain id 'kibana:content_management:ai-components-aibutton--default'"));
}

[InheritsTests]
public class StorybookBareIdTests() : StorybookRegistryTest("""
:::{storybook}
:id: ai-components-aibutton--default
:::
""")
{
	[Test]
	public void ResolvesFromConfiguredRegistry() => Block!.StoryId.Should().Be("ai-components-aibutton--default");
}

[InheritsTests]
public class StorybookIframeTests() : StorybookRegistryTest("""
:::{storybook}
:id: kibana:shared_ux:components-callout--info
:::
""")
{
	[Test]
	public void RendersIframeFallback()
	{
		Block!.HasInlineStory.Should().BeFalse();
		Html.Should().Contain("<iframe");
		Html.Should().Contain(
			"src=\"http://127.0.0.1:6007/storybook/shared_ux/iframe.html?id=components-callout--info-storybook&amp;viewMode=story\""
		);
	}
}

[InheritsTests]
public class StorybookBodyTests() : StorybookRegistryTest(
	"""
:::{storybook}
:id: kibana:shared_ux:components-callout--info
Supporting details for this story.
:::
"""
)
{
	[Test]
	public void RendersBodyContent() => Html.Should().Contain("Supporting details for this story.");
}

[InheritsTests]
public class StorybookInvalidHeightTests() : StorybookRegistryTest(
	"""
:::{storybook}
:id: kibana:shared_ux:components-callout--info
:height: tall
:::
"""
)
{
	[Test]
	public void WarnsAndFallsBackToDefaultHeight()
	{
		Block!.Height.Should().Be(400);
		Collector
			.Diagnostics
			.Should()
			.ContainSingle(d => d.Severity == Severity.Warning && d.Message.Contains(":height: must be a positive integer"));
		Html.Should().Contain("height:400px");
	}
}

[InheritsTests]
public class StorybookMissingRegistryTests() : DirectiveTest<StorybookBlock>(
	"""
:::{storybook}
:id: kibana:shared_ux:ai-components-aibutton--default
:::
"""
)
{
	[Test]
	public void EmitsError() => Collector.Diagnostics.Should().Contain(d => d.Message.Contains("requires docset.yml storybook.registry"));
}

[InheritsTests]
public class StorybookMissingIdTests() : StorybookRegistryTest("""
:::{storybook}
:::
""")
{
	[Test]
	public void EmitsError() => Collector.Diagnostics.Should().Contain(d => d.Message.Contains("requires :id: or :project:"));
}

[InheritsTests]
public class StorybookPositionalArgumentWarningTests() : StorybookRegistryTest(
	"""
:::{storybook} /storybook/ignored
:id: kibana:shared_ux:components-callout--info
:::
"""
)
{
	[Test]
	public void EmitsWarning() =>
		Collector
			.Diagnostics
			.Should()
			.ContainSingle(d => d.Severity == Severity.Warning && d.Message.Contains("ignores positional arguments"));
}
