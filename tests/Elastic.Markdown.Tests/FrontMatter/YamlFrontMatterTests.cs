// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Myst.FrontMatter;
using Elastic.Markdown.Tests.Directives;

namespace Elastic.Markdown.Tests.FrontMatter;

// Regression coverage for `layout:` front matter: MarkdownPageLayoutConverter must be looked up by
// SharpYaml for the *nullable* enum type (YamlFrontMatter.Layout is `MarkdownPageLayout?`), not just
// the non-nullable enum, or every hyphenated value below fails with "Invalid enum scalar".
public class LayoutLandingPage() : DirectiveTest("""
	---
	layout: landing-page
	---

	# Test Page
	""")
{
	[Test]
	public void ReadsLayout()
	{
		Collector.Diagnostics.Should().BeEmpty();
		File.YamlFrontMatter.Should().NotBeNull();
		File.YamlFrontMatter.Layout.Should().Be(MarkdownPageLayout.LandingPage);
	}
}

public class LayoutNotFound() : DirectiveTest("""
	---
	layout: not-found
	---

	# Test Page
	""")
{
	[Test]
	public void ReadsLayout()
	{
		Collector.Diagnostics.Should().BeEmpty();
		File.YamlFrontMatter.Should().NotBeNull();
		File.YamlFrontMatter.Layout.Should().Be(MarkdownPageLayout.NotFound);
	}
}

public class LayoutArchive() : DirectiveTest("""
	---
	layout: archive
	---

	# Test Page
	""")
{
	[Test]
	public void ReadsLayout()
	{
		Collector.Diagnostics.Should().BeEmpty();
		File.YamlFrontMatter.Should().NotBeNull();
		File.YamlFrontMatter.Layout.Should().Be(MarkdownPageLayout.Archive);
	}
}

public class LayoutFullSearch() : DirectiveTest("""
	---
	layout: full-search
	---

	# Test Page
	""")
{
	[Test]
	public void ReadsLayout()
	{
		Collector.Diagnostics.Should().BeEmpty();
		File.YamlFrontMatter.Should().NotBeNull();
		File.YamlFrontMatter.Layout.Should().Be(MarkdownPageLayout.FullSearch);
	}
}

public class LayoutHub() : DirectiveTest("""
	---
	layout: hub
	---

	:::{hero}
	:title: Test Page
	:::
	""")
{
	[Test]
	public void ReadsLayout()
	{
		Collector.Diagnostics.Should().BeEmpty();
		File.YamlFrontMatter.Should().NotBeNull();
		File.YamlFrontMatter.Layout.Should().Be(MarkdownPageLayout.Hub);
	}
}

public class LayoutOmitted() : DirectiveTest("""
	# Test Page
	""")
{
	[Test]
	public void LayoutIsNull()
	{
		Collector.Diagnostics.Should().BeEmpty();
		File.YamlFrontMatter.Should().NotBeNull();
		File.YamlFrontMatter.Layout.Should().BeNull();
	}
}

// Regression coverage for the diagnostics wiring in `YamlSerialization.Deserialize`: an unmapped
// front-matter key is an error reported at the key's file line (the body starts below the opening `---`).
// Comment-only front matter deserializes to null. It must read as empty front matter, not fail the page.
public class CommentOnlyFrontMatter() : DirectiveTest("""
	---
	# no keys yet
	---

	# Test Page
	""")
{
	[Test]
	public void ReadsAsEmpty()
	{
		Collector.Diagnostics.Should().BeEmpty();
		File.YamlFrontMatter.Should().NotBeNull();
		File.Title.Should().Be("Test Page");
	}
}

public class UnknownFrontMatterKeyIsAHint() : DirectiveTest(
	"""
	---
	navigation_title: Test
	navigaton_title: typo
	---

	# Test Page
	"""
)
{
	[Test]
	public void ReportsHintAtFileLineWithSuggestion()
	{
		var diagnostic = Collector.Diagnostics.Should().ContainSingle().Subject;
		diagnostic.Severity.Should().Be(Severity.Hint);
		diagnostic.Line.Should().Be(3);
		diagnostic.Column.Should().Be(1);
		diagnostic.Length.Should().Be("navigaton_title".Length);
		diagnostic.Message.Should().Contain("Unknown key 'navigaton_title'").And.Contain("Did you mean 'navigation_title'?");
	}
}

public class UnknownFrontMatterKeyWithoutSuggestionListsValidKeys() : DirectiveTest(
	"""
	---
	totally_unknown_key: some-value
	---

	# Test Page
	"""
)
{
	[Test]
	public void ListsValidKeys()
	{
		var diagnostic = Collector.Diagnostics.Should().ContainSingle().Subject;
		diagnostic.Severity.Should().Be(Severity.Hint);
		diagnostic.Line.Should().Be(2);
		diagnostic.Message.Should().Contain("Unknown key 'totally_unknown_key'").And.Contain("Valid keys:").And.Contain("navigation_title");
	}
}

public class AllUnknownFrontMatterKeysAreHints() : DirectiveTest(
	"""
	---
	type: connector
	comment: Something
	navigation_title: Test
	---

	# Test Page
	"""
)
{
	[Test]
	public void ReportsOneHintPerKeyAndNoErrors()
	{
		Collector.Errors.Should().Be(0);
		Collector.Diagnostics.Should().HaveCount(2).And.OnlyContain(d => d.Severity == Severity.Hint);
		Collector
			.Diagnostics
			.Select(d => d.Message)
			.Should()
			.Contain(m => m.Contains("Unknown key 'type'"))
			.And
			.Contain(m => m.Contains("Unknown key 'comment'"));
	}
}

public class YamlFrontMatterTests() : DirectiveTest(
	"""
---
navigation_title: "Documentation Guide"
meta_title: "Elastic Docs for {{key}}"
sub:
  key: "value"
---

# Elastic Docs v3
"""
)
{
	[Test]
	public void ReadsTitle() => File.Title.Should().Be("Elastic Docs v3");

	[Test]
	public void ReadsNavigationTitle() => File.NavigationTitle.Should().Be("Documentation Guide");

	[Test]
	public void ReadsMetaTitle() => File.MetaTitle.Should().Be("Elastic Docs for value");

	[Test]
	public void ReadsSubstitutions()
	{
		File.YamlFrontMatter.Should().NotBeNull();
		File.YamlFrontMatter.Properties.Should().NotBeEmpty().And.HaveCount(1).And.ContainKey("key");
	}
}

public class EmptyFileWarnsNeedingATitle() : DirectiveTest("")
{
	[Test]
	public void ReadsTitle() => File.Title.Should().Be("index.md");

	[Test]
	public void ReadsNavigationTitle() => File.NavigationTitle.Should().Be("index.md");

	[Test]
	public void WarnsOfNoTitle() =>
		Collector.Diagnostics.Should().NotBeEmpty().And.Contain(d => d.Message.Contains("Document has no title, using file name as title."));
}

public class NavigationTitleSupportReplacements() : DirectiveTest(
	"""
---
title: Elastic Docs v3
navigation_title: "Documentation Guide: {{key}}"
sub:
  key: "value"
---
"""
)
{
	[Test]
	public void ReadsNavigationTitle() => File.NavigationTitle.Should().Be("Documentation Guide: value");
}

public class MetaTitleEmptyAfterSubstitution() : DirectiveTest(
	"""
---
title: Elastic Docs v3
meta_title: "{{empty}}"
sub:
  empty: ""
---
"""
)
{
	[Test]
	public void TreatsAnEmptyResultAsNoOverride() => File.MetaTitle.Should().BeNull();
}

public class ProductsSingle() : DirectiveTest("""
	---
	products:
	  - id: "apm"
	---

	# APM
	""")
{
	[Test]
	public void ReadsProducts()
	{
		File.YamlFrontMatter.Should().NotBeNull();
		File.YamlFrontMatter.Products.Should().NotBeNull().And.HaveCount(1);
		File.YamlFrontMatter.Products.First().Id.Should().Be("apm");
	}
}

public class ProductsMultiple() : DirectiveTest("""
	---
	products:
	  - id: "apm"
	  - id: "elasticsearch"
	---

	# APM
	""")
{
	[Test]
	public void ReadsProducts()
	{
		File.YamlFrontMatter.Should().NotBeNull();
		File.YamlFrontMatter.Products.Should().NotBeNull().And.HaveCount(2);
		File.YamlFrontMatter.Products.First().Id.Should().Be("apm");
		File.YamlFrontMatter.Products.Last().Id.Should().Be("elasticsearch");
	}
}

public class ProductsSuggestionWhenMispelled() : DirectiveTest("""
	---
	products:
	  - id: aapm
	---

	# APM
	""")
{
	[Test]
	public void HasErrors()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Message.Contains("Invalid products frontmatter value: \"aapm\". Did you mean \"apm\"?"));
	}
}

public class ProductsSuggestionWhenMispelled2() : DirectiveTest("""
	---
	products:
	  - id: apmagent
	---

	# APM
	""")
{
	[Test]
	public void HasErrors()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Message.Contains("Invalid products frontmatter value: \"apmagent\". Did you mean \"apm-agent\"?"));
	}
}

public class ProductsSuggestionWhenCasingError() : DirectiveTest("""
	---
	products:
	  - id: Apm
	---

	# APM
	""")
{
	[Test]
	public void HasErrors()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Message.Contains("Invalid products frontmatter value: \"Apm\". Did you mean \"apm\"?"));
	}
}

public class ProductsSuggestionWhenEmpty() : DirectiveTest("""
	---
	products:
	  - id: ""
	---

	# APM
	""")
{
	[Test]
	public void HasErrors()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Message.Contains("Invalid products frontmatter value: \"Product 'id' field is required."));
	}
}

public class MappedPagesValidUrl() : DirectiveTest(
	"""
	---
	mapped_pages:
	  - "https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html"
	---

	# Test Page
	"""
)
{
	[Test]
	public void NoErrors() => Collector.Diagnostics.Should().BeEmpty();
}

public class MappedPagesInvalidUrl() : DirectiveTest(
	"""
	---
	mapped_pages:
	  - "https://www.elastic.co/docs/get-started/deployment-options"
	---

	# Test Page
	"""
)
{
	[Test]
	public void HasErrors()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector
			.Diagnostics
			.Should()
			.Contain(
				d => d.Message.Contains(
					"Invalid mapped_pages URL: \"https://www.elastic.co/docs/get-started/deployment-options\". All mapped_pages URLs must start with \"https://www.elastic.co/guide\". Please update the URL to reference content under the Elastic documentation guide."
				)
			);
	}
}

public class MappedPagesMixedUrls() : DirectiveTest(
	"""
	---
	mapped_pages:
	  - "https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html"
	  - "https://www.elastic.co/docs/invalid-url"
	  - "https://www.elastic.co/guide/en/kibana/current/index.html"
	---

	# Test Page
	"""
)
{
	[Test]
	public void HasErrorsForInvalidUrl()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector
			.Diagnostics
			.Should()
			.Contain(
				d => d.Message.Contains(
					"Invalid mapped_pages URL: \"https://www.elastic.co/docs/invalid-url\". All mapped_pages URLs must start with \"https://www.elastic.co/guide\""
				)
			);
	}
}

public class MappedPagesEmptyUrl() : DirectiveTest("""
	---
	mapped_pages:
	  - ""
	---

	# Test Page
	""")
{
	[Test]
	public void NoErrorsForEmptyUrl()
	{
		// Empty URLs are ignored, no validation error should occur
		Collector.Diagnostics.Should().BeEmpty();
	}
}

public class MappedPagesExternalUrl() : DirectiveTest(
	"""
	---
	mapped_pages:
	  - "https://github.com/elastic/docs-builder"
	---

	# Test Page
	"""
)
{
	[Test]
	public void HasErrorsForExternalUrl()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector
			.Diagnostics
			.Should()
			.Contain(
				d => d.Message.Contains(
					"Invalid mapped_pages URL: \"https://github.com/elastic/docs-builder\". All mapped_pages URLs must start with \"https://www.elastic.co/guide\""
				)
			);
	}
}

public class MappedPagesMalformedUri() : DirectiveTest(
	"""
	---
	mapped_pages:
	  - "https://www.elastic.co/guide/[invalid-characters]"
	---

	# Test Page
	"""
)
{
	[Test]
	public void HasErrorsForMalformedUri()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector
			.Diagnostics
			.Should()
			.Contain(
				d => d.Message.Contains(
					"Invalid mapped_pages URL: \"https://www.elastic.co/guide/[invalid-characters]\". All mapped_pages URLs must start with \"https://www.elastic.co/guide\""
				)
			);
	}
}

public class MappedPagesInvalidScheme() : DirectiveTest(
	"""
	---
	mapped_pages:
	  - "https://www.elastic.co/guide/invalid uri with spaces"
	---

	# Test Page
	"""
)
{
	[Test]
	public void HasErrorsForInvalidScheme()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector
			.Diagnostics
			.Should()
			.Contain(
				d => d.Message.Contains(
					"Invalid mapped_pages URL: \"https://www.elastic.co/guide/invalid uri with spaces\". All mapped_pages URLs must start with \"https://www.elastic.co/guide\""
				)
			);
	}
}

public class MappedPagesNotAbsoluteUri() : DirectiveTest("""
	---
	mapped_pages:
	  - "not-a-uri-at-all"
	---

	# Test Page
	""")
{
	[Test]
	public void HasErrorsForNotAbsoluteUri()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector
			.Diagnostics
			.Should()
			.Contain(
				d => d.Message.Contains(
					"Invalid mapped_pages URL: \"not-a-uri-at-all\". All mapped_pages URLs must start with \"https://www.elastic.co/guide\""
				)
			);
	}
}
