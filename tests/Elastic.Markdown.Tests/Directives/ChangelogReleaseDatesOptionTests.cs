// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.Changelog;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>Tests for the <c>:release-dates:</c> directive option.</summary>
[InheritsTests]
public class ChangelogReleaseDatesOptionDefaultOffTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogReleaseDatesOptionDefaultOffTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/1.34.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: apm-agent-dotnet
			  target: 1.34.0
			release-date: "2026-04-09"
			entries:
			- title: Add tracing improvements
			  type: feature
			  products:
			  - product: apm-agent-dotnet
			    target: 1.34.0
			  prs:
			  - "500"
			"""
			)
		);

	[Test]
	public void ReleaseDatesDisabledByDefault() => Block!.ReleaseDatesEnabled.Should().BeFalse();

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="heading-wrapper" id="1.34.0">
			<h2>
				<a class="headerlink" href="#1.34.0">1.34.0</a>
			</h2>
		</div>
		<div class="heading-wrapper" id="apm-agent-dotnet-1.34.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#apm-agent-dotnet-1.34.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>Add tracing improvements.
				<a href="https://github.com/elastic/apm-agent-dotnet/pull/500">#500</a>
			</li>
		</ul>
		"""
		);
}

[InheritsTests]
public class ChangelogReleaseDatesOptionEnabledTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogReleaseDatesOptionEnabledTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:release-dates:
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/1.34.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: apm-agent-dotnet
			  target: 1.34.0
			release-date: "2026-04-09"
			entries:
			- title: Add tracing improvements
			  type: feature
			  products:
			  - product: apm-agent-dotnet
			    target: 1.34.0
			  prs:
			  - "500"
			"""
			)
		);

	[Test]
	public void ReleaseDatesEnabledWhenFlagPresent() => Block!.ReleaseDatesEnabled.Should().BeTrue();

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="heading-wrapper" id="1.34.0">
			<h2>
				<a class="headerlink" href="#1.34.0">1.34.0</a>
			</h2>
		</div>
		<p>
			<em>Released: April 9, 2026</em>
		</p>
		<div class="heading-wrapper" id="apm-agent-dotnet-1.34.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#apm-agent-dotnet-1.34.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>Add tracing improvements.
				<a href="https://github.com/elastic/apm-agent-dotnet/pull/500">#500</a>
			</li>
		</ul>
		"""
		);
}

[InheritsTests]
public class ChangelogReleaseDatesOptionEnabledWithoutBundleDateTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogReleaseDatesOptionEnabledWithoutBundleDateTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:release-dates:
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: New feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "100"
			"""
			)
		);

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="heading-wrapper" id="9.3.0">
			<h2>
				<a class="headerlink" href="#9.3.0">9.3.0</a>
			</h2>
		</div>
		<div class="heading-wrapper" id="elasticsearch-9.3.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#elasticsearch-9.3.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>New feature.
				<a href="https://github.com/elastic/elasticsearch/pull/100">#100</a>
			</li>
		</ul>
		"""
		);
}

[InheritsTests]
public class ChangelogReleaseDatesOptionDescriptionStillRendersTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogReleaseDatesOptionDescriptionStillRendersTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/1.34.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: apm-agent-dotnet
			  target: 1.34.0
			release-date: "2026-04-09"
			description: |
			  This release includes tracing improvements and bug fixes.
			entries:
			- title: Add tracing improvements
			  type: feature
			  products:
			  - product: apm-agent-dotnet
			    target: 1.34.0
			  prs:
			  - "500"
			"""
			)
		);

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="heading-wrapper" id="1.34.0">
			<h2>
				<a class="headerlink" href="#1.34.0">1.34.0</a>
			</h2>
		</div>
		<p>This release includes tracing improvements and bug fixes.</p>
		<div class="heading-wrapper" id="apm-agent-dotnet-1.34.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#apm-agent-dotnet-1.34.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>Add tracing improvements.
				<a href="https://github.com/elastic/apm-agent-dotnet/pull/500">#500</a>
			</li>
		</ul>
		"""
		);
}
