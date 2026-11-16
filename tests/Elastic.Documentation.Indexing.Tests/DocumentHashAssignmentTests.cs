// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Indexing.Exporters.Elasticsearch;
using Elastic.Documentation.Search.Contract;

namespace Elastic.Documentation.Indexing.Tests;

public class DocumentHashAssignmentTests
{
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

	private static string Hash(DocumentationDocument doc)
	{
		ElasticsearchMarkdownExporter.AssignDocumentMetadata(doc, "semantic", "lexical", "synonyms");
		return doc.Hash;
	}

	[Test]
	public void AssignDocumentMetadata_UnchangedDocument_IsStable() => Hash(NewDocument()).Should().Be(Hash(NewDocument()));

	[Test]
	public void AssignDocumentMetadata_OnlyProductChanged_Differs()
	{
		var changed = NewDocument();
		changed.Product = "cloud";

		Hash(changed).Should().NotBe(Hash(NewDocument()));
	}

	[Test]
	public void AssignDocumentMetadata_OnlyAppliesToChanged_Differs()
	{
		var changed = NewDocument();
		changed.Applies = [new AppliesToEntry { Type = "stack", SubType = "all", Lifecycle = "ga", Version = "9.0" }];

		Hash(changed).Should().NotBe(Hash(NewDocument()));
	}

	[Test]
	public void AssignDocumentMetadata_OnlyApiOperationChanged_Differs()
	{
		var original = NewDocument();
		original.Api = new ApiOperationMetadata { Endpoints = ["GET /a"], Method = "GET", Path = "/a", OperationId = "op" };
		var changed = NewDocument();
		changed.Api = new ApiOperationMetadata { Endpoints = ["GET /a"], Method = "GET", Path = "/a", OperationId = "renamed" };

		Hash(changed).Should().NotBe(Hash(original));
	}
}
