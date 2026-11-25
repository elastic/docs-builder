// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Search.Contract;

namespace Elastic.Documentation.Indexing.Tests;

// `--no-semantic` swaps the semantic index's mapping for one without inference fields; search must still find it by the same alias.
public class SemanticWithoutInferenceMappingTests
{
	[Test]
	public void CreateContext_SemanticWithoutInference_ReadsTheSemanticAlias()
	{
		var semantic = DocumentationMappingContext.DocumentationDocumentSemantic.CreateContext(type: "assembler", env: "dev");
		var withoutInference = DocumentationMappingContext.DocumentationDocumentSemanticWithoutInference.CreateContext(
			type: "assembler",
			env: "dev"
		);

		withoutInference.ResolveReadTarget().Should().Be(semantic.ResolveReadTarget());
	}

	[Test]
	public void GetMappingsJson_SemanticWithoutInference_HasNoSemanticTextFields()
	{
		var semantic = DocumentationMappingContext.DocumentationDocumentSemantic.CreateContext(type: "assembler", env: "dev");
		var withoutInference = DocumentationMappingContext.DocumentationDocumentSemanticWithoutInference.CreateContext(
			type: "assembler",
			env: "dev"
		);

		semantic.GetMappingsJson().Should().Contain("\"semantic_text\"");
		withoutInference.GetMappingsJson().Should().NotContain("\"semantic_text\"");
	}
}
