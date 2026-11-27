// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Serverless;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Serverless;

public class ServerlessPreviewBundlesTests : IDisposable
{
	private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("preview-bundles");

	public void Dispose()
	{
		_root.Delete(true);
		GC.SuppressFinalize(this);
	}

	private static ServerlessService Kibana => ServerlessPromotion.Services["kibana"];
	private static ServerlessService Elasticsearch => ServerlessPromotion.Services["elasticsearch"];

	private string Product(string name) => Path.Combine(_root.FullName, "cloud-serverless", name);

	private void Existing(string name, string content)
	{
		_ = Directory.CreateDirectory(Path.Combine(_root.FullName, "cloud-serverless"));
		File.WriteAllText(Product(name), content);
	}

	private DirectoryInfo Pending(params (string Name, string Content)[] files)
	{
		var pending = Directory.CreateDirectory(Path.Combine(_root.FullName, "preview-pending", "run"));
		foreach (var (name, content) in files)
			File.WriteAllText(Path.Combine(pending.FullName, name), content);
		return pending;
	}

	private string[] ProductFiles()
	{
		var folder = Path.Combine(_root.FullName, "cloud-serverless");
		return Directory.Exists(folder) ? [.. Directory.GetFiles(folder).Select(f => Path.GetFileName(f)).Order()] : [];
	}

	[Test]
	public void Replace_NewFiles_ReplaceTheOnesOfAnEarlierRun()
	{
		Existing("kibana-cloud-serverless-staging.yaml", "old staging");
		Existing("kibana-cloud-serverless-edge.yaml", "old edge");
		var pending = Pending(("kibana-cloud-serverless-staging.yaml", "new staging"), ("kibana-cloud-serverless-edge.yaml", "new edge"));

		ServerlessPreviewBundles.Replace(Kibana, _root, pending, NullLogger.Instance);

		File.ReadAllText(Product("kibana-cloud-serverless-staging.yaml")).Should().Be("new staging");
		File.ReadAllText(Product("kibana-cloud-serverless-edge.yaml")).Should().Be("new edge");
	}

	[Test]
	public void Replace_NothingPending_RemovesTheEarlierBundlesOfTheServiceAndKeepsTheOtherServices()
	{
		Existing("kibana-cloud-serverless-staging.yaml", "kibana");
		Existing("elasticsearch-serverless-cloud-serverless-staging.yaml", "es serverless");
		Existing("elasticsearch-cloud-serverless-edge.yaml", "es");

		ServerlessPreviewBundles.Replace(Kibana, _root, null, NullLogger.Instance);

		ProductFiles().Should().Equal("elasticsearch-cloud-serverless-edge.yaml", "elasticsearch-serverless-cloud-serverless-staging.yaml");
	}

	[Test]
	public void Replace_TwoServicesSharingTheProductFolder_EachKeepsTheBundlesOfTheOther()
	{
		Existing("kibana-cloud-serverless-staging.yaml", "kibana staging");
		Existing("kibana-cloud-serverless-edge.yaml", "kibana edge");
		var pending = Pending(("elasticsearch-serverless-cloud-serverless-edge.yaml", "es edge"));

		ServerlessPreviewBundles.Replace(Elasticsearch, _root, pending, NullLogger.Instance);

		ProductFiles()
			.Should()
			.Equal(
				"elasticsearch-serverless-cloud-serverless-edge.yaml",
				"kibana-cloud-serverless-edge.yaml",
				"kibana-cloud-serverless-staging.yaml"
			);
	}

	[Test]
	public void Replace_NothingPendingAndNothingBefore_LeavesNoProductFolder()
	{
		ServerlessPreviewBundles.Replace(Kibana, _root, null, NullLogger.Instance);

		Directory.Exists(Path.Combine(_root.FullName, "cloud-serverless")).Should().BeFalse();
	}

	[Test]
	public void Replace_LastBundlesOfAServiceRemoved_LeavesNoEmptyProductFolder()
	{
		Existing("kibana-cloud-serverless-staging.yaml", "kibana");

		ServerlessPreviewBundles.Replace(Kibana, _root, null, NullLogger.Instance);

		Directory.Exists(Path.Combine(_root.FullName, "cloud-serverless")).Should().BeFalse();
	}

	[Test]
	public void RemoveLegacy_AFolderFromBeforeTheRenameToEdge_LosesTheHeadBundleOfEveryService()
	{
		Existing("kibana-cloud-serverless-head.yaml", "kibana, called head before");
		Existing("elasticsearch-serverless-cloud-serverless-head.yaml", "elasticsearch, called head before");
		Existing("kibana-cloud-serverless-staging.yaml", "kibana staging");
		Existing("kibana-cloud-serverless-edge.yaml", "kibana edge");

		ServerlessPreviewBundles.RemoveLegacy(_root, NullLogger.Instance);

		ProductFiles().Should().Equal("kibana-cloud-serverless-edge.yaml", "kibana-cloud-serverless-staging.yaml");
	}

	[Test]
	public void RemoveLegacy_ARunOfOneServiceNextToAStaleHeadBundleOfAnother_DoesNotLeaveASecondReleaseBehind()
	{
		// Only Kibana is refreshed. Elasticsearch still has the bundle of the release that was called head.
		Existing("elasticsearch-serverless-cloud-serverless-head.yaml", "elasticsearch, called head before");
		Existing("kibana-cloud-serverless-head.yaml", "kibana, called head before");

		ServerlessPreviewBundles.RemoveLegacy(_root, NullLogger.Instance);
		ServerlessPreviewBundles.Replace(Kibana, _root, Pending(("kibana-cloud-serverless-edge.yaml", "kibana edge")), NullLogger.Instance);

		ProductFiles().Should().Equal("kibana-cloud-serverless-edge.yaml");
	}

	[Test]
	public void RemoveLegacy_NothingToRemove_LeavesNoProductFolder()
	{
		ServerlessPreviewBundles.RemoveLegacy(_root, NullLogger.Instance);

		Directory.Exists(Path.Combine(_root.FullName, "cloud-serverless")).Should().BeFalse();
	}

	[Test]
	public void Replace_AFolderFromBeforeTheRename_TheServiceLosesItsHeadBundleWithItsReplacementAndNoOtherServiceDoes()
	{
		Existing("kibana-cloud-serverless-head.yaml", "kibana, called head before");
		Existing("elasticsearch-serverless-cloud-serverless-head.yaml", "elasticsearch, called head before");

		ServerlessPreviewBundles.Replace(Kibana, _root, Pending(("kibana-cloud-serverless-edge.yaml", "kibana edge")), NullLogger.Instance);

		ProductFiles().Should().Equal("elasticsearch-serverless-cloud-serverless-head.yaml", "kibana-cloud-serverless-edge.yaml");
	}
}
