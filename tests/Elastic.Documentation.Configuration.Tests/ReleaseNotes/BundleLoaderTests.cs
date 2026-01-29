// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;
using FluentAssertions;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

/// <summary>
/// Unit tests for BundleLoader in the new ReleaseNotes namespace.
/// These tests verify the bundle loading functionality works correctly
/// after moving from Elastic.Changelog.BundleLoading.
/// </summary>
public class BundleLoaderTests(ITestOutputHelper output)
{
	private readonly ITestOutputHelper _output = output;
	private readonly MockFileSystem _fileSystem = new();
	private readonly List<string> _warnings = [];

	private BundleLoader CreateService() => new(_fileSystem);

	private void EmitWarning(string message)
	{
		_warnings.Add(message);
		_output.WriteLine($"Warning: {message}");
	}

	[Fact]
	public void LoadBundles_WithValidBundle_ReturnsLoadedBundle()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// language=yaml
		var bundleContent =
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - title: Test feature
			    type: feature
			    pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.yaml", bundleContent);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(1);
		bundles[0].Version.Should().Be("9.3.0");
		bundles[0].Repo.Should().Be("elasticsearch");
		bundles[0].Entries.Should().HaveCount(1);
		bundles[0].Entries[0].Title.Should().Be("Test feature");
		bundles[0].Entries[0].Type.Should().Be(ChangelogEntryType.Feature);
		_warnings.Should().BeEmpty();
	}

	[Fact]
	public void FilterEntries_WithPublishBlocker_FiltersBlockedTypes()
	{
		// Arrange
		var service = CreateService();
		var entries = new List<ChangelogEntry>
		{
			new() { Title = "Feature", Type = ChangelogEntryType.Feature },
			new() { Title = "Regression", Type = ChangelogEntryType.Regression },
			new() { Title = "Bug fix", Type = ChangelogEntryType.BugFix }
		};

		var publishBlocker = new PublishBlocker
		{
			Types = ["regression"]
		};

		// Act
		var filtered = service.FilterEntries(entries, publishBlocker, []);

		// Assert
		filtered.Should().HaveCount(2);
		filtered.Select(e => e.Type).Should().NotContain(ChangelogEntryType.Regression);
	}

	[Fact]
	public void FilterEntries_WithFeatureIdFilter_HidesMatchingEntries()
	{
		// Arrange
		var service = CreateService();
		var entries = new List<ChangelogEntry>
		{
			new() { Title = "Public feature", Type = ChangelogEntryType.Feature },
			new() { Title = "Hidden feature", Type = ChangelogEntryType.Feature, FeatureId = "experimental-api" }
		};

		var featureIdsToHide = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "experimental-api" };

		// Act
		var filtered = service.FilterEntries(entries, null, featureIdsToHide);

		// Assert
		filtered.Should().HaveCount(1);
		filtered[0].Title.Should().Be("Public feature");
	}

	[Fact]
	public void MergeBundlesByTarget_WithSameVersion_MergesBundles()
	{
		// Arrange
		var service = CreateService();
		var bundles = new List<LoadedBundle>
		{
			new("9.3.0", "elasticsearch", new Bundle(), "/path/to/es.yaml",
				[new ChangelogEntry { Title = "ES Entry", Type = ChangelogEntryType.Feature }]),
			new("9.3.0", "kibana", new Bundle(), "/path/to/kibana.yaml",
				[new ChangelogEntry { Title = "Kibana Entry", Type = ChangelogEntryType.Feature }])
		};

		// Act
		var merged = service.MergeBundlesByTarget(bundles);

		// Assert
		merged.Should().HaveCount(1);
		merged[0].Version.Should().Be("9.3.0");
		merged[0].Repo.Should().Be("elasticsearch+kibana");
		merged[0].Entries.Should().HaveCount(2);
	}
}
