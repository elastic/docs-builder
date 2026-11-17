// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>What this repository's own <c>docs/</c> docset resolves into, from the frozen copy under <c>Golden/Baseline/docs</c>.</summary>
public class DocsetBaselineGoldenTests
{
	[Test]
	public void BaselineDocs_Resolved_MatchSnapshot()
	{
		var collector = new CapturingDiagnosticsCollector();
		var docSet = DocsetGolden.LoadAndResolveBaselineDocs(collector, DocsetGolden.CreateBaselineDocsFileSystem());

		DocsetGolden.Normalise(collector.Captured.ToSnapshotText(firstLineOnly: true)).Should().MatchSnapshot(name: "diagnostics");
		DocsetGolden.Normalise(TocDump.ToText(docSet.TableOfContents)).Should().MatchSnapshot(name: "toc");
		DocsetGolden.Normalise(ModelDump.ToText(docSet, DocsetGolden.Summarize, "TableOfContents")).Should().MatchSnapshot(name: "docset");
		DocsetGolden.Normalise(ModelDump.ToText(docSet.TableOfContents, DocsetGolden.Summarize)).Should().MatchSnapshot(name: "toc-model");
	}

	[Test]
	public void BaselineDocs_Configured_MatchSnapshot()
	{
		var collector = new CapturingDiagnosticsCollector();
		var fileSystem = DocsetGolden.CreateBaselineDocsFileSystem();
		var docSet = DocsetGolden.LoadAndResolveBaselineDocs(collector, fileSystem);
		var config = DocsetGolden.CreateConfiguration(docSet, collector, fileSystem, withGlobalConfig: true, docsetName: "_docset.yml");

		DocsetGolden.Normalise(collector.Captured.ToSnapshotText(firstLineOnly: true)).Should().MatchSnapshot(name: "diagnostics");
		DocsetGolden
			.Normalise(ModelDump.ToText(config, DocsetGolden.Summarize, "Substitutions"))
			.Should()
			.MatchSnapshot(name: "configuration");
		var docsetSubstitutions = string.Join(
			'\n',
			config
				.Substitutions
				.Where(
					kvp => !kvp.Key.StartsWith("version.", StringComparison.Ordinal) &&
						!kvp.Key.StartsWith("product.", StringComparison.Ordinal) && !kvp.Key.StartsWith('.')
				)
				.OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
				.Select(kvp => $"{kvp.Key}: {kvp.Value}")
		);
		docsetSubstitutions.Should().MatchSnapshot(name: "substitutions");
	}
}
