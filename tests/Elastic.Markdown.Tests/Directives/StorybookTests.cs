// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.Storybook;

namespace Elastic.Markdown.Tests.Directives;

public abstract class StorybookRegistryTest(ITestOutputHelper output, string content) : DirectiveTest<StorybookBlock>(output, content)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile("docs/docs_registry.json", new MockFileData(RegistryJson));

	protected override string? GetDocsetExtraYaml() =>
"""
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

public class StorybookInlineIdTests(ITestOutputHelper output) : StorybookRegistryTest(output,
"""
:::{storybook}
:id: kibana:shared_ux:ai-components-aibutton--default
:title: AI Button / Default story
:::
"""
)
{
	[Fact]
	public void ResolvesStory()
	{
		Block!.Project.Should().Be("kibana");
		Block.Storybook.Should().Be("shared_ux");
		Block.DocsId.Should().Be("ai-components-aibutton--default");
		Block.StoryId.Should().Be("ai-components-aibutton--default");
		Block.InlineEntry.Should().Be("http://127.0.0.1:6007/storybook-docs/shared_ux/registry.js");
		Block.StoryUrl.Should().Be("http://127.0.0.1:6007/storybook/shared_ux/iframe.html?id=ai-components-aibutton--default&viewMode=story");
		Block.Height.Should().Be(360);
	}

	[Fact]
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

public class StorybookStructuredReferenceTests(ITestOutputHelper output) : StorybookRegistryTest(output,
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
	[Fact]
	public void ResolvesComponentAndStory() =>
		Block!.StoryId.Should().Be("ai-components-aibutton--default");
}

public class StorybookStructuredReferenceWrongStorybookTests(ITestOutputHelper output) : StorybookRegistryTest(output,
"""
:::{storybook}
:project: kibana
:storybook: content_management
:story: ai-components-aibutton--default
:::
"""
)
{
	[Fact]
	public void DoesNotFallbackToAnotherStorybook() =>
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("does not contain id 'kibana:content_management:ai-components-aibutton--default'"));
}

public class StorybookBareIdTests(ITestOutputHelper output) : StorybookRegistryTest(output,
"""
:::{storybook}
:id: ai-components-aibutton--default
:::
"""
)
{
	[Fact]
	public void ResolvesFromConfiguredRegistry() =>
		Block!.StoryId.Should().Be("ai-components-aibutton--default");
}

public class StorybookIframeTests(ITestOutputHelper output) : StorybookRegistryTest(output,
"""
:::{storybook}
:id: kibana:shared_ux:components-callout--info
:::
"""
)
{
	[Fact]
	public void RendersIframeFallback()
	{
		Block!.HasInlineStory.Should().BeFalse();
		Html.Should().Contain("<iframe");
		Html.Should().Contain("src=\"http://127.0.0.1:6007/storybook/shared_ux/iframe.html?id=components-callout--info-storybook&amp;viewMode=story\"");
	}
}

public class StorybookBodyTests(ITestOutputHelper output) : StorybookRegistryTest(output,
"""
:::{storybook}
:id: kibana:shared_ux:components-callout--info
Supporting details for this story.
:::
"""
)
{
	[Fact]
	public void RendersBodyContent() =>
		Html.Should().Contain("Supporting details for this story.");
}

public class StorybookInvalidHeightTests(ITestOutputHelper output) : StorybookRegistryTest(output,
"""
:::{storybook}
:id: kibana:shared_ux:components-callout--info
:height: tall
:::
"""
)
{
	[Fact]
	public void WarnsAndFallsBackToDefaultHeight()
	{
		Block!.Height.Should().Be(400);
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Warning
			&& d.Message.Contains(":height: must be a positive integer"));
		Html.Should().Contain("height:400px");
	}
}

public class StorybookMissingRegistryTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook}
:id: kibana:shared_ux:ai-components-aibutton--default
:::
"""
)
{
	[Fact]
	public void EmitsError() =>
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("requires docset.yml storybook.registry"));
}

public class StorybookMissingIdTests(ITestOutputHelper output) : StorybookRegistryTest(output,
"""
:::{storybook}
:::
"""
)
{
	[Fact]
	public void EmitsError() =>
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("requires :id: or :project:"));
}

public class StorybookPositionalArgumentWarningTests(ITestOutputHelper output) : StorybookRegistryTest(output,
"""
:::{storybook} /storybook/ignored
:id: kibana:shared_ux:components-callout--info
:::
"""
)
{
	[Fact]
	public void EmitsWarning() =>
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Warning
			&& d.Message.Contains("ignores positional arguments"));
}
