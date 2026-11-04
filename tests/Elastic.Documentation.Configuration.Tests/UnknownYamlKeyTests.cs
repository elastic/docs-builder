// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.ScopedFileSystem;
using SharpYaml;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>Every surface that reads user-authored YAML reports unknown keys as errors with a precise location and message.</summary>
public class UnknownYamlKeyTests
{
	[Test]
	public void DocumentationSet_TypoInTocItem_ErrorsWithSuggestionInsteadOfDroppingEntry()
	{
		var fileSystem = new MockFileSystem();
		// language=yaml
		const string docsetYaml =
			"""
			project: 'test-project'
			toc:
			  - file: index.md
			  - fiel: other.md
			""";
		fileSystem.AddFile("/docs/docset.yml", new MockFileData(docsetYaml));
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Test"));
		fileSystem.AddFile("/docs/other.md", new MockFileData("# Other"));

		var collector = new CapturingDiagnosticsCollector();
		_ = DocumentationSetFile.LoadAndResolve(
			collector,
			fileSystem.FileInfo.New("/docs/docset.yml"),
			new ScopedFileSystem(fileSystem, "/docs")
		);

		var diagnostic = collector.Captured.Should().ContainSingle(d => d.Severity == Severity.Error).Subject;
		diagnostic.Message.Should().Contain("Unknown key 'fiel'").And.Contain("Did you mean 'file'?");
		diagnostic.Line.Should().Be(4);
		diagnostic.Column.Should().Be(5);
	}

	[Test]
	public void SiteNavigation_TypoInTocEntry_ErrorsWithSuggestion()
	{
		// language=yaml
		const string yaml = """
			toc:
			  - toc: get-started
			    path_prefx: get-started
			""";
		var collector = new CapturingDiagnosticsCollector();

		_ = SiteNavigationFile.Deserialize(yaml, collector, "/config/navigation.yml");

		var diagnostic = collector.Captured.Should().ContainSingle().Subject;
		diagnostic.Severity.Should().Be(Severity.Error);
		diagnostic.File.Should().Be("/config/navigation.yml");
		diagnostic.Line.Should().Be(3);
		diagnostic.Message.Should().Contain("Unknown key 'path_prefx'").And.Contain("Did you mean 'path_prefix'?");
	}

	[Test]
	public void SiteNavigation_UnknownKeyFarFromAnyKnownKey_ListsValidKeys()
	{
		// language=yaml
		const string yaml = """
			toc:
			  - toc: get-started
			    colour_scheme: blue
			""";
		var collector = new CapturingDiagnosticsCollector();

		_ = SiteNavigationFile.Deserialize(yaml, collector, "/config/navigation.yml");

		collector
			.Captured
			.Should()
			.ContainSingle()
			.Which
			.Message
			.Should()
			.Contain("Unknown key 'colour_scheme'")
			.And
			.Contain("Valid keys:")
			.And
			.Contain("path_prefix");
	}

	[Test]
	public void Products_SeveralUnknownKeys_ThrowOnceListingAll()
	{
		var provider = new ConfigurationFileProvider(new NullLoggerFactory(), new ConfigurationFileSystem());
		var versions = provider.CreateVersionConfiguration();
		// language=yaml
		const string yaml =
			"""
			products:
			  widget:
			    display: 'Widget'
			    versioning: 'stack'
			    dispaly_name: 'typo'
			    colour: red
			""";
		using var reader = new StringReader(yaml);

		var act = () => ProductExtensions.CreateProducts(reader, versions, "/config/products.yml");

		var exception = act.Should().Throw<YamlException>().Which;
		exception.Message.Should().Contain("Unknown key 'dispaly_name'").And.Contain("Unknown key 'colour'");
		exception.Message.Should().Contain("(5,").And.Contain("(6,");
	}

	[Test]
	public void ApiConfiguration_UnknownEntryKey_ReportedThroughCallback()
	{
		var collector = new CapturingDiagnosticsCollector();
		var options = new YamlSerializerOptions
		{
			Converters = [new ApiProductSequenceConverter(new ApiProductEntryConverter())]
		}.WithUnmappedReporting(new YamlUnmappedMemberReporting(collector, "docset.yml"));
		// language=yaml
		const string yaml = """
			- spec: api.json
			  product: elasticsearch
			  prodcut_id: typo
			""";

		var sequence = YamlSerializer.Deserialize<ApiProductSequence>(yaml, options)!;

		sequence.SingleEntry!.Product.Should().Be("elasticsearch");
		var diagnostic = collector.Captured.Should().ContainSingle().Subject;
		diagnostic.Severity.Should().Be(Severity.Error);
		diagnostic.Line.Should().Be(3);
		diagnostic.Message.Should().Contain("Unknown key 'prodcut_id'");
	}

	[Test]
	public void LegacyKeyPolicy_CanDowngradeOrSuppressSpecificKeys()
	{
		var collector = new CapturingDiagnosticsCollector();
		var reporting = new YamlUnmappedMemberReporting(
			collector,
			"toc.yml",
			policy: member =>
				member.Name switch
				{
					"old_key" => new YamlUnknownKeyOverride(Severity.Hint, "'old_key' is deprecated."),
					"ignored" => YamlUnknownKeyOverride.Suppress,
					_ => null
				}
		);
		var options = new YamlSerializerOptions
		{
			Converters = [new ApiProductSequenceConverter(new ApiProductEntryConverter())]
		}.WithUnmappedReporting(reporting);
		// language=yaml
		const string yaml =
			"""
			- spec: api.json
			  product: elasticsearch
			  old_key: 1
			  ignored: 2
			  other: 3
			""";

		_ = YamlSerializer.Deserialize<ApiProductSequence>(yaml, options);

		collector
			.Captured
			.Select(d => (d.Severity, d.Message))
			.Should()
			.SatisfyRespectively(hint =>
			{
				hint.Severity.Should().Be(Severity.Hint);
				hint.Message.Should().Be("'old_key' is deprecated.");
			}, error =>
			{
				error.Severity.Should().Be(Severity.Error);
				error.Message.Should().Contain("Unknown key 'other'");
			});
	}

	[Test]
	public void CodexExampleConfiguration_HasNoUnknownKeys()
	{
		var path = System.IO.Path.Combine(Paths.WorkingDirectoryRoot.FullName, "config", "codex.example.yml");

		var config = CodexConfiguration.Deserialize(File.ReadAllText(path), path);

		config.Groups.Should().ContainSingle().Which.Id.Should().Be("docs");
	}
}
