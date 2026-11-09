// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Indexing.Exporters.Elasticsearch;
using Elastic.Documentation.Search.Contract;

namespace Elastic.Documentation.Indexing.Tests;

public class DocumentMetadataFingerprintTests
{
	private static DocumentationDocument NewDocument() =>
		new()
		{
			Title = "title",
			SearchTitle = "title",
			Path = "/p",
			Product = "elasticsearch",
			RelatedProducts =
			[
				new IndexedProduct { Id = "kibana", Repository = "kibana" },
				new IndexedProduct { Id = "logstash", Repository = "logstash" }
			],
			Applies = [new AppliesToEntry { Type = "stack", SubType = "all", Lifecycle = "ga", Version = "9.0" }],
		};

	[Test]
	public void Create_SameMetadata_ReturnsSameFingerprint() =>
		DocumentMetadataFingerprint.Create(NewDocument()).Should().Be(DocumentMetadataFingerprint.Create(NewDocument()));

	[Test]
	public void Create_ProductChanged_ChangesFingerprint()
	{
		var changed = NewDocument();
		changed.Product = "cloud";

		DocumentMetadataFingerprint.Create(changed).Should().NotBe(DocumentMetadataFingerprint.Create(NewDocument()));
	}

	[Test]
	public void Create_ProductMissing_ChangesFingerprint()
	{
		var changed = NewDocument();
		changed.Product = null;

		DocumentMetadataFingerprint.Create(changed).Should().NotBe(DocumentMetadataFingerprint.Create(NewDocument()));
	}

	[Test]
	public void Create_RelatedProductAdded_ChangesFingerprint()
	{
		var changed = NewDocument();
		changed.RelatedProducts = [.. changed.RelatedProducts!, new IndexedProduct { Id = "beats", Repository = "beats" }];

		DocumentMetadataFingerprint.Create(changed).Should().NotBe(DocumentMetadataFingerprint.Create(NewDocument()));
	}

	[Test]
	public void Create_LifecycleChanged_ChangesFingerprint()
	{
		var changed = NewDocument();
		changed.Applies = [new AppliesToEntry { Type = "stack", SubType = "all", Lifecycle = "beta", Version = "9.0" }];

		DocumentMetadataFingerprint.Create(changed).Should().NotBe(DocumentMetadataFingerprint.Create(NewDocument()));
	}

	[Test]
	public void Create_CollectionsReordered_ReturnsSameFingerprint()
	{
		var reordered = NewDocument();
		reordered.RelatedProducts = [.. reordered.RelatedProducts!.Reverse()];

		DocumentMetadataFingerprint.Create(reordered).Should().Be(DocumentMetadataFingerprint.Create(NewDocument()));
	}
}
