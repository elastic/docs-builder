// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Backfill;

namespace Elastic.Changelog.Tests.Backfill;

/// <summary>
/// A realistic release-notes Markdown fixture, modeled on the published EDOT Java page
/// (elastic/elastic-otel-java docs/release-notes/index.md before the repo switched to
/// native bundle YAMLs). Exercises frontmatter, comment templates, prose-only releases,
/// typed subsections, PR-reference variants, trailing prose, and a post-cutoff release.
/// </summary>
public static class ReleaseNotesFixture
{
	public static BackfillScope Scope { get; } = new()
	{
		ProductId = "edot-java",
		Path = "edot/sdks/java",
		Owner = "elastic",
		Repo = "elastic-otel-java",
		Ref = "9a61ce4faaf08e272c433a083bcc6f0e96d80e0a",
		RepoPath = "docs/release-notes/index.md",
		Cutoff = "1.10.0"
	};

	// language=markdown
	public const string Markdown = """
		---
		navigation_title: EDOT Java
		description: Release notes for Elastic Distribution of OpenTelemetry Java.
		products:
		  - id: edot-sdk
		---

		# Elastic Distribution of OpenTelemetry Java release notes [edot-java-release-notes]

		Review the changes, fixes, and more in each version.

		% Release notes include only features, enhancements, and fixes.

		% ## version.next [edot-java-X.X.X-release-notes]

		% ### Features and enhancements [edot-java-X.X.X-features-enhancements]
		% *

		## 2.0.0 [edot-java-2-0-0-release-notes]
		**Release date:** May 1, 2026

		### Features and enhancements [edot-java-2-0-0-features-enhancements]
		* A release owned by the live pipeline #1200

		## 1.10.0 [edot-java-1-10-0-release-notes]
		**Release date:** March 24, 2026

		The 1.10.0 release contains fixes for potential security vulnerabilities.
		Refer to our [security advisory](https://discuss.elastic.co/t/example/385700) for more details.

		This release is based on the following upstream versions:

		* opentelemetry-javaagent: [2.26.1](https://github.com/open-telemetry/opentelemetry-java-instrumentation/releases/tag/v2.26.1)
		* opentelemetry-sdk: [1.60.1](https://github.com/open-telemetry/opentelemetry-java/releases/tag/v1.60.1)

		## 1.9.0 [edot-java-1-9-0-release-notes]
		**Release date:** February 9, 2026

		### Breaking changes [edot-java-1-9-0-fixes]
		- universal profiling is disabled by default #958

		### Deprecations [edot-java-1-9-0-deprecations]
		* The legacy exporter is deprecated #960

		## 1.7.0 [edot-java-1-7-0-release-notes]
		**Release date:** November 5, 2025

		### Features and enhancements [edot-java-1-7-0-features-enhancements]
		* Inferred spans can now be disabled and re-enabled via central config - [#838](https://github.com/elastic/elastic-otel-java/pull/838)
		* The agent config is now logged on startup - [835](https://github.com/elastic/elastic-otel-java/pull/835)
		* add header support for OpAMP integration [#848](https://github.com/elastic/elastic-otel-java/pull/848)

		This release is based on the following upstream versions:

		* opentelemetry-javaagent: [2.21.0](https://github.com/open-telemetry/opentelemetry-java-instrumentation/releases/tag/v2.21.0)

		### Known issues [edot-java-1-7-0-known-issues]
		* OpAMP header support can fail on restart #850

		## 1.4.1 [edot-java-1.4.1-release-notes]

		### Fixes [edot-java-1.4.1-fixes]

		* Fixed `otel.exporter.otlp.metrics.temporality.preference` config option having no effect.

		### Upgrade notes [edot-java-1.4.1-upgrade-notes]

		Re-run the installer after upgrading.
		""";
}
