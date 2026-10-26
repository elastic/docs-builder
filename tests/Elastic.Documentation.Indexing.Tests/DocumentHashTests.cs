// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Indexing.Exporters.Elasticsearch;
using Elastic.Documentation.Search.Contract;

namespace Elastic.Documentation.Indexing.Tests;

public class DocumentHashTests
{
	private static readonly DocumentHashInputs Normal = new(
		"semantic",
		"lexical",
		"synonyms",
		ForceReindexSalt.Create(forceReindex: false)
	);

	private static DocumentationDocument NewDocument() =>
		new()
		{
			Title = "title",
			SearchTitle = "title",
			Path = "/p",
			Body = "same body",
			Product = "elasticsearch",
			Applies = [new AppliesToEntry { Type = "stack", SubType = "all", Lifecycle = "beta", Version = "9.0" }]
		};

	private static string Hash(DocumentationDocument doc, DocumentHashInputs inputs) => AssignHash(doc, inputs);

	private static string AssignHash(DocumentationDocument doc, DocumentHashInputs inputs)
	{
		ElasticsearchMarkdownExporter.AssignDocumentMetadata(doc, inputs);
		return doc.Hash;
	}

	[Test]
	public void ComputeDocumentHash_UnchangedDocumentWithoutForce_IsStable() =>
		Hash(NewDocument(), Normal).Should().Be(Hash(NewDocument(), Normal));

	[Test]
	public void ComputeDocumentHash_UnchangedDocumentWithForce_DiffersFromNormalRun() =>
		Hash(NewDocument(), Normal with { ForceReindexSalt = ForceReindexSalt.Create(forceReindex: true) })
			.Should()
			.NotBe(Hash(NewDocument(), Normal));

	[Test]
	public void ComputeDocumentHash_UnchangedDocumentAcrossForcedRuns_Differs()
	{
		var first = Normal with { ForceReindexSalt = ForceReindexSalt.Create(forceReindex: true) };
		var second = Normal with { ForceReindexSalt = ForceReindexSalt.Create(forceReindex: true) };

		Hash(NewDocument(), first).Should().NotBe(Hash(NewDocument(), second));
	}

	[Test]
	public void ComputeDocumentHash_OnlyProductChanged_Differs()
	{
		var changed = NewDocument();
		changed.Product = "cloud";

		Hash(changed, Normal).Should().NotBe(Hash(NewDocument(), Normal));
	}

	[Test]
	public void ComputeDocumentHash_OnlyAppliesToChanged_Differs()
	{
		var changed = NewDocument();
		changed.Applies = [new AppliesToEntry { Type = "stack", SubType = "all", Lifecycle = "ga", Version = "9.0" }];

		Hash(changed, Normal).Should().NotBe(Hash(NewDocument(), Normal));
	}

	[Test]
	public void ComputeDocumentHash_OnlyApiOperationChanged_Differs()
	{
		var original = NewDocument();
		original.Api = new ApiOperationMetadata { Endpoints = ["GET /a"], Method = "GET", Path = "/a", OperationId = "op" };
		var changed = NewDocument();
		changed.Api = new ApiOperationMetadata { Endpoints = ["GET /a"], Method = "GET", Path = "/a", OperationId = "renamed" };

		Hash(changed, Normal).Should().NotBe(Hash(original, Normal));
	}
}
