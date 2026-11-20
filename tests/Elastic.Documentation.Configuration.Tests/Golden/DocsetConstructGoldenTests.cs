// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>What each table of contents construct of a <c>docset.yml</c> or <c>toc.yml</c> resolves into today.</summary>
public class DocsetConstructGoldenTests
{
	[Test]
	public void FilesAndFolders_Resolved_MatchSnapshot() =>
		DocsetGolden
			.Resolve(
				"""
			project: constructs
			toc:
			  - file: index.md
			  - file: guide.md
			    children:
			      - file: guide/one.md
			      - hidden: guide/two.md
			  - hidden: hidden.md
			  - folder: setup
			    children:
			      - file: index.md
			      - file: install.md
			      - folder: deep
			        children:
			          - file: index.md
			          - file: leaf.md
			  - folder: discovered
			""",
				new DocFile("index.md"),
				new DocFile("guide.md"),
				new DocFile("guide/one.md"),
				new DocFile("guide/two.md"),
				new DocFile("hidden.md"),
				new DocFile("setup/index.md"),
				new DocFile("setup/install.md"),
				new DocFile("setup/deep/index.md"),
				new DocFile("setup/deep/leaf.md"),
				new DocFile("discovered/b.md"),
				new DocFile("discovered/a.md"),
				new DocFile("discovered/index.md"),
				new DocFile("discovered/_private.md"),
				new DocFile("discovered/sub/c.md")
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void FolderSortAndExclude_Resolved_MatchSnapshot() =>
		DocsetGolden
			.Resolve(
				"""
			project: constructs
			toc:
			  - folder: releases
			    sort: desc
			  - folder: natural
			    sort: ascending
			  - folder: filtered
			    exclude:
			      - 'draft-*.md'
			      - 'skip/**'
			""",
				new DocFile("releases/1.2.md"),
				new DocFile("releases/1.10.md"),
				new DocFile("releases/1.9.md"),
				new DocFile("natural/page-10.md"),
				new DocFile("natural/page-2.md"),
				new DocFile("natural/page-1.md"),
				new DocFile("filtered/keep.md"),
				new DocFile("filtered/draft-one.md"),
				new DocFile("filtered/skip/inner.md")
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void NestedTocFiles_Resolved_MatchSnapshot() =>
		DocsetGolden
			.Resolve(
				"""
			project: constructs
			default_cta: docset-level
			cta:
			  docset-level:
			    button:
			      label: Docset
			      url: https://example.com/docset
			  section:
			    button:
			      label: Section
			      url: https://example.com/section
			toc:
			  - file: index.md
			  - toc: reference
			  - toc: guides/advanced
			    island: true
			  - folder: area
			    children:
			      - toc: inner
			""",
				new DocFile("index.md"),
				new DocFile(
					"reference/toc.yml",
					"""
				default_cta: section
				suppress:
				  - DeepLinkingVirtualFile
				toc:
				  - file: index.md
				  - file: api.md
				  - toc: nested
				"""
				),
				new DocFile("reference/index.md"),
				new DocFile("reference/api.md"),
				new DocFile("reference/nested/toc.yml", "toc:\n  - file: only.md\n"),
				new DocFile("reference/nested/only.md"),
				new DocFile("guides/advanced/toc.yml", "island: true\ntoc:\n  - file: index.md\n  - folder: more\n"),
				new DocFile("guides/advanced/index.md"),
				new DocFile("guides/advanced/more/x.md"),
				new DocFile("area/inner/toc.yml", "toc:\n  - file: leaf.md\n"),
				new DocFile("area/inner/leaf.md")
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void CrossLinks_Resolved_MatchSnapshot() =>
		DocsetGolden
			.Resolve(
				"""
			project: constructs
			toc:
			  - file: index.md
			  - crosslink: docs-content://get-started/
			  - crosslink: elasticsearch://reference/elasticsearch
			    title: Elasticsearch reference
			  - folder: area
			    children:
			      - crosslink: kibana://reference/
			        title: Kibana
			""",
				new DocFile("index.md"),
				new DocFile("area/index.md")
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void CliReference_Resolved_MatchSnapshot() =>
		DocsetGolden
			.Resolve(
				"""
			project: constructs
			toc:
			  - file: index.md
			  - cli: cli-schema.json
			    folder: cli
			    title: Tool reference
			    navigation_title: Tool
			    children:
			      - file: overview.md
			      - folder: extra
			        children:
			          - file: more.md
			  - cli: other/schema.json
			""",
				new DocFile("index.md"),
				new DocFile("cli-schema.json", "{}"),
				new DocFile("cli/overview.md"),
				new DocFile("cli/extra/more.md"),
				new DocFile("other/schema.json", "{}")
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void IslandFlagOnEitherSide_Resolved_MatchSnapshot() =>
		DocsetGolden
			.Resolve(
				"""
			project: constructs
			toc:
			  - toc: both
			    island: true
			  - toc: inline-only
			    island: true
			  - toc: file-only
			  - toc: neither
			""",
				new DocFile("both/toc.yml", "island: true\ntoc:\n  - file: a.md\n"),
				new DocFile("both/a.md"),
				new DocFile("inline-only/toc.yml", "toc:\n  - file: a.md\n"),
				new DocFile("inline-only/a.md"),
				new DocFile("file-only/toc.yml", "island: true\ntoc:\n  - file: a.md\n"),
				new DocFile("file-only/a.md"),
				new DocFile("neither/toc.yml", "toc:\n  - file: a.md\n"),
				new DocFile("neither/a.md")
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void CliReferenceInsideFolderAndToc_Resolved_MatchSnapshot() =>
		DocsetGolden
			.Resolve(
				"""
			project: constructs
			toc:
			  - folder: tools
			    children:
			      - file: index.md
			      - cli: tool.json
			        folder: tool-docs
			      - cli: sub/other.json
			  - toc: nested
			""",
				new DocFile("tools/index.md"),
				new DocFile("tools/tool.json", "{}"),
				new DocFile("tools/tool-docs/page.md"),
				new DocFile("tools/sub/other.json", "{}"),
				new DocFile("nested/toc.yml", "toc:\n  - cli: schema.json\n    children:\n      - file: page.md\n"),
				new DocFile("nested/schema.json", "{}"),
				new DocFile("nested/schema/page.md")
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void DetectionRules_Resolved_MatchSnapshot() =>
		DocsetGolden
			.Resolve(
				"""
			project: constructs
			toc:
			  - file: index.md
			  - file: rules/index.md
			    deprecated_file: rules/deprecated.md
			    detection_rules:
			      - ../rules-source
			""",
				new DocFile("index.md"),
				new DocFile("rules/index.md"),
				new DocFile("rules-source/rule-b.toml", "[rule]\nname = \"B\"\n"),
				new DocFile("rules-source/rule-a.toml", "[rule]\nname = \"A\"\n"),
				new DocFile("rules-source/README.md", "# readme\n"),
				new DocFile("rules-source/guide.md", "# guide\n"),
				new DocFile("rules-source/_deprecated/old.toml", "[rule]\nname = \"Old\"\n")
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void Listing_Resolved_MatchSnapshot() =>
		DocsetGolden
			.Resolve(
				"""
			project: constructs
			toc:
			  - file: index.md
			  - listing: posts
			    groups:
			      - news
			      - how-to
			  - listing: notes
			    glob: '*.md'
			    exclude:
			      - 'old-*'
			""",
				new DocFile("index.md"),
				new DocFile("posts/index.md"),
				new DocFile("posts/one.md", "---\nlisting_group: news\n---\n# One\n"),
				new DocFile("posts/two.md", "---\nlisting_group: how-to\n---\n# Two\n"),
				new DocFile("posts/three.md", "# Three\n"),
				new DocFile("posts/archive/deep.md", "---\nlisting_group: news\n---\n# Deep\n"),
				new DocFile("posts/_draft.md"),
				new DocFile("notes/a.md"),
				new DocFile("notes/old-b.md")
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void DocsetSettings_Configured_MatchSnapshot() =>
		DocsetGolden
			.Configure(
				"""
			project: settings
			display_name: Settings docs
			description: A docset that sets every option
			icon: gear
			max_toc_depth: 4
			dev_docs: true
			registry: public
			cross_links:
			  - docs-content
			  - public://elasticsearch
			  - ' '
			exclude:
			  - '_*.md'
			  - 'drafts/**'
			  - '!drafts/keep.md'
			extensions:
			  - detection-rules
			subs:
			  Product: The Product
			  other: Other
			suppress:
			  - AutolinkElasticCoDocs
			features:
			  primary-nav: true
			  disable-github-edit-link: true
			  guide-nav: false
			products:
			  - id: elasticsearch
			  - id: not_a_product
			  - id: cloud-hosted
			cta:
			  custom:
			    button:
			      label: Custom
			      url: https://example.com/custom
			    benefits:
			      - one
			      - two
			storybook:
			  registry: https://example.com/storybook
			release_notes:
			  - product: elasticsearch
			toc:
			  - file: index.md
			""",
				true,
				new DocFile("index.md")
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void GeneratedSubstitutions_Configured_MatchSnapshot() =>
		DocsetGolden.GeneratedSubstitutions("project: subs\ntoc:\n  - file: index.md\n").Should().MatchSnapshot();

	[Test]
	public void ApiConfigurations_Configured_MatchSnapshot() =>
		DocsetGolden
			.Configure(
				"""
			project: apis
			api:
			  elasticsearch:
			    - spec: elasticsearch.json
			      product: elasticsearch
			      repository: elastic/elasticsearch-specification
			  kibana:
			    - spec: specs/kibana.yaml
			      product: kibana
			      children:
			        - file: overview.md
			toc:
			  - file: index.md
			""",
				true,
				new DocFile("index.md"),
				new DocFile("elasticsearch.json", "{}"),
				new DocFile("api/kibana/overview.md")
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void Redirects_Configured_MatchSnapshot() =>
		DocsetGolden
			.Configure(
				"""
			project: redirects
			toc:
			  - file: index.md
			  - file: new-name.md
			""",
				false,
				new DocFile("index.md"),
				new DocFile("new-name.md"),
				new DocFile(
					"redirects.yml",
					"""
				redirects:
				  'old-name.md': 'new-name.md'
				  'removed.md':
				    to: 'index.md'
				    anchors:
				      '!': ''
				      'old-anchor': 'new-anchor'
				  'gone/index.md': 'index.md'
				"""
				)
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void Branding_Configured_MatchSnapshot() =>
		DocsetGolden
			.Configure(
				"""
			project: branded
			branding:
			  icon: images/icon.svg
			  header-bg: '#112233'
			  og-image: images/og.png
			  favicon: images/favicon.ico
			  apple-touch-icon: images/touch.png
			  copyright: Example Inc.
			toc:
			  - file: index.md
			""",
				false,
				new DocFile("index.md"),
				new DocFile("images/icon.svg", "<svg/>"),
				new DocFile("images/og.png", "png"),
				new DocFile("images/favicon.ico", "ico"),
				new DocFile("images/touch.png", "png")
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void CodexMetadata_Resolved_MatchSnapshot() =>
		DocsetGolden
			.Resolve(
				"""
			project: codex-member
			codex:
			  group: platform
			toc:
			  - file: index.md
			""",
				new DocFile("index.md")
			)
			.Should()
			.MatchSnapshot();
}
