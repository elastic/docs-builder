// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.AppliesSwitch;
using Nullean.Snapshots;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class ApplicabilitySwitchMalformedYamlWithSyncTests() : DirectiveTest<AppliesSwitchBlock>(
	"""
:::::{applies-switch}

::::{applies-item} stack: [ga
:sync: explicit-key
:::{tip}
This item has an applies_to definition that is not valid YAML.
:::
::::

:::::
"""
)
{
	[Test]
	public void ReportsTheDefinitionAsAnError() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
		error {repo}/docs/index.md:5 Unable to parse applies_to definition: stack: [ga
		SharpYaml.SemanticErrorException: (Lin: 1, Col: 0, Chr: 10) - (Lin: 1, Col: 0, Chr: 10): While parsing a flow sequence, did not find expected ',' or ']'.
		   at SharpYaml.Parser`1.ParseFlowSequenceEntry(Boolean isFirst)
		   at SharpYaml.Parser`1.StateMachine()
		   at SharpYaml.Parser`1.MoveNext()
		   at SharpYaml.Serialization.YamlReader.YamlReaderState.ReadCore()
		   at SharpYaml.Serialization.YamlReader.YamlReaderState.Read()
		   at SharpYaml.Serialization.YamlReader.Read()
		   at SharpYaml.Serialization.Converters.YamlUntypedObjectConverter.Read(YamlReader reader, Type typeToConvert)
		   at SharpYaml.Serialization.Converters.YamlUntypedObjectConverter.Read(YamlReader reader, Type typeToConvert)
		   at SharpYaml.Serialization.Converters.YamlUntypedObjectConverter.Read(YamlReader reader, Type typeToConvert)
		   at Elastic.Documentation.AppliesTo.ApplicableToYamlConverter.ReadUntyped(YamlReader reader) in {repo}/src/Elastic.Documentation.Configuration/Converters/ApplicableToYamlConverter.cs:line 41
		   at Elastic.Documentation.AppliesTo.ApplicableToYamlConverter.Read(YamlReader reader) in {repo}/src/Elastic.Documentation.Configuration/Converters/ApplicableToYamlConverter.cs:line 94
		   at SharpYaml.Serialization.YamlConverter`1.Read(YamlReader reader, Type typeToConvert)
		   at Elastic.Markdown.Myst.DocsBuilderYamlStaticContext.ReadValue4(YamlReader reader, RuntimeCustomConverterCache runtimeConverters) in {repo}/.artifacts/obj/Elastic.Markdown/release/SharpYaml.SourceGenerator/SharpYaml.SourceGeneration.YamlSerializerContextGenerator/DocsBuilderYamlStaticContext.g.cs:line 2768
		   at Elastic.Markdown.Myst.DocsBuilderYamlStaticContext.GeneratedTypeInfo4.Read(YamlReader reader) in {repo}/.artifacts/obj/Elastic.Markdown/release/SharpYaml.SourceGenerator/SharpYaml.SourceGeneration.YamlSerializerContextGenerator/DocsBuilderYamlStaticContext.g.cs:line 485
		   at SharpYaml.YamlTypeInfo`1.ReadAsObject(YamlReader reader)
		   at SharpYaml.YamlSerializer.DeserializeCore(YamlTypeInfo typeInfo, String yaml)
		   at SharpYaml.YamlSerializer.Deserialize(String yaml, Type returnType, YamlSerializerOptions options)
		   at SharpYaml.YamlSerializer.Deserialize[T](String yaml, YamlSerializerOptions options)
		   at Elastic.Markdown.Myst.YamlSerialization.Deserialize[T](String yaml, ProductsConfiguration products, Func`2 configure) in {repo}/src/Elastic.Markdown/Myst/YamlSerialization.cs:line 59
		   at Elastic.Markdown.Myst.YamlSerialization.Deserialize[T](String yaml, ProductsConfiguration products) in {repo}/src/Elastic.Markdown/Myst/YamlSerialization.cs:line 21
		   at Elastic.Markdown.Myst.Directives.AppliesSwitch.AppliesItemBlock.ParseApplicableTo(String yaml) in {repo}/src/Elastic.Markdown/Myst/Directives/AppliesSwitch/AppliesSwitchBlock.cs:line 78
		error {repo}/docs/index.md:5 Unable to parse applies_to definition: stack: [ga
		SharpYaml.SemanticErrorException: (Lin: 1, Col: 0, Chr: 10) - (Lin: 1, Col: 0, Chr: 10): While parsing a flow sequence, did not find expected ',' or ']'.
		   at SharpYaml.Parser`1.ParseFlowSequenceEntry(Boolean isFirst)
		   at SharpYaml.Parser`1.StateMachine()
		   at SharpYaml.Parser`1.MoveNext()
		   at SharpYaml.Serialization.YamlReader.YamlReaderState.ReadCore()
		   at SharpYaml.Serialization.YamlReader.YamlReaderState.Read()
		   at SharpYaml.Serialization.YamlReader.Read()
		   at SharpYaml.Serialization.Converters.YamlUntypedObjectConverter.Read(YamlReader reader, Type typeToConvert)
		   at SharpYaml.Serialization.Converters.YamlUntypedObjectConverter.Read(YamlReader reader, Type typeToConvert)
		   at SharpYaml.Serialization.Converters.YamlUntypedObjectConverter.Read(YamlReader reader, Type typeToConvert)
		   at Elastic.Documentation.AppliesTo.ApplicableToYamlConverter.ReadUntyped(YamlReader reader) in {repo}/src/Elastic.Documentation.Configuration/Converters/ApplicableToYamlConverter.cs:line 41
		   at Elastic.Documentation.AppliesTo.ApplicableToYamlConverter.Read(YamlReader reader) in {repo}/src/Elastic.Documentation.Configuration/Converters/ApplicableToYamlConverter.cs:line 94
		   at SharpYaml.Serialization.YamlConverter`1.Read(YamlReader reader, Type typeToConvert)
		   at Elastic.Markdown.Myst.DocsBuilderYamlStaticContext.ReadValue4(YamlReader reader, RuntimeCustomConverterCache runtimeConverters) in {repo}/.artifacts/obj/Elastic.Markdown/release/SharpYaml.SourceGenerator/SharpYaml.SourceGeneration.YamlSerializerContextGenerator/DocsBuilderYamlStaticContext.g.cs:line 2768
		   at Elastic.Markdown.Myst.DocsBuilderYamlStaticContext.GeneratedTypeInfo4.Read(YamlReader reader) in {repo}/.artifacts/obj/Elastic.Markdown/release/SharpYaml.SourceGenerator/SharpYaml.SourceGeneration.YamlSerializerContextGenerator/DocsBuilderYamlStaticContext.g.cs:line 485
		   at SharpYaml.YamlTypeInfo`1.ReadAsObject(YamlReader reader)
		   at SharpYaml.YamlSerializer.DeserializeCore(YamlTypeInfo typeInfo, String yaml)
		   at SharpYaml.YamlSerializer.Deserialize(String yaml, Type returnType, YamlSerializerOptions options)
		   at SharpYaml.YamlSerializer.Deserialize[T](String yaml, YamlSerializerOptions options)
		   at Elastic.Markdown.Myst.YamlSerialization.Deserialize[T](String yaml, ProductsConfiguration products, Func`2 configure) in {repo}/src/Elastic.Markdown/Myst/YamlSerialization.cs:line 59
		   at Elastic.Markdown.Myst.YamlSerialization.Deserialize[T](String yaml, ProductsConfiguration products) in {repo}/src/Elastic.Markdown/Myst/YamlSerialization.cs:line 21
		   at Elastic.Markdown.Myst.Directives.DirectiveHtmlRenderer.ParseApplicableTo(String yaml, DirectiveBlock block) in {repo}/src/Elastic.Markdown/Myst/Directives/DirectiveHtmlRenderer.cs:line 648
		"""
			);

	[Test]
	public void KeepsTheExplicitSyncKey() => Block!.OfType<AppliesItemBlock>().Single().SyncKey.Should().Be("explicit-key");
}

[InheritsTests]
public class ApplicabilitySwitchMalformedYamlTests() : DirectiveTest<AppliesSwitchBlock>(
	"""
:::::{applies-switch}

::::{applies-item} stack: [ga
:::{tip}
This item has an applies_to definition that is not valid YAML.
:::
::::

:::::
"""
)
{
	[Test]
	public void ReportsTheDefinitionAsAnError() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
		error {repo}/docs/index.md:5 Unable to parse applies_to definition: stack: [ga
		SharpYaml.SemanticErrorException: (Lin: 1, Col: 0, Chr: 10) - (Lin: 1, Col: 0, Chr: 10): While parsing a flow sequence, did not find expected ',' or ']'.
		   at SharpYaml.Parser`1.ParseFlowSequenceEntry(Boolean isFirst)
		   at SharpYaml.Parser`1.StateMachine()
		   at SharpYaml.Parser`1.MoveNext()
		   at SharpYaml.Serialization.YamlReader.YamlReaderState.ReadCore()
		   at SharpYaml.Serialization.YamlReader.YamlReaderState.Read()
		   at SharpYaml.Serialization.YamlReader.Read()
		   at SharpYaml.Serialization.Converters.YamlUntypedObjectConverter.Read(YamlReader reader, Type typeToConvert)
		   at SharpYaml.Serialization.Converters.YamlUntypedObjectConverter.Read(YamlReader reader, Type typeToConvert)
		   at SharpYaml.Serialization.Converters.YamlUntypedObjectConverter.Read(YamlReader reader, Type typeToConvert)
		   at Elastic.Documentation.AppliesTo.ApplicableToYamlConverter.ReadUntyped(YamlReader reader) in {repo}/src/Elastic.Documentation.Configuration/Converters/ApplicableToYamlConverter.cs:line 41
		   at Elastic.Documentation.AppliesTo.ApplicableToYamlConverter.Read(YamlReader reader) in {repo}/src/Elastic.Documentation.Configuration/Converters/ApplicableToYamlConverter.cs:line 94
		   at SharpYaml.Serialization.YamlConverter`1.Read(YamlReader reader, Type typeToConvert)
		   at Elastic.Markdown.Myst.DocsBuilderYamlStaticContext.ReadValue4(YamlReader reader, RuntimeCustomConverterCache runtimeConverters) in {repo}/.artifacts/obj/Elastic.Markdown/release/SharpYaml.SourceGenerator/SharpYaml.SourceGeneration.YamlSerializerContextGenerator/DocsBuilderYamlStaticContext.g.cs:line 2768
		   at Elastic.Markdown.Myst.DocsBuilderYamlStaticContext.GeneratedTypeInfo4.Read(YamlReader reader) in {repo}/.artifacts/obj/Elastic.Markdown/release/SharpYaml.SourceGenerator/SharpYaml.SourceGeneration.YamlSerializerContextGenerator/DocsBuilderYamlStaticContext.g.cs:line 485
		   at SharpYaml.YamlTypeInfo`1.ReadAsObject(YamlReader reader)
		   at SharpYaml.YamlSerializer.DeserializeCore(YamlTypeInfo typeInfo, String yaml)
		   at SharpYaml.YamlSerializer.Deserialize(String yaml, Type returnType, YamlSerializerOptions options)
		   at SharpYaml.YamlSerializer.Deserialize[T](String yaml, YamlSerializerOptions options)
		   at Elastic.Markdown.Myst.YamlSerialization.Deserialize[T](String yaml, ProductsConfiguration products, Func`2 configure) in {repo}/src/Elastic.Markdown/Myst/YamlSerialization.cs:line 59
		   at Elastic.Markdown.Myst.YamlSerialization.Deserialize[T](String yaml, ProductsConfiguration products) in {repo}/src/Elastic.Markdown/Myst/YamlSerialization.cs:line 21
		   at Elastic.Markdown.Myst.Directives.AppliesSwitch.AppliesItemBlock.ParseApplicableTo(String yaml) in {repo}/src/Elastic.Markdown/Myst/Directives/AppliesSwitch/AppliesSwitchBlock.cs:line 78
		error {repo}/docs/index.md:5 Unable to parse applies_to definition: stack: [ga
		SharpYaml.SemanticErrorException: (Lin: 1, Col: 0, Chr: 10) - (Lin: 1, Col: 0, Chr: 10): While parsing a flow sequence, did not find expected ',' or ']'.
		   at SharpYaml.Parser`1.ParseFlowSequenceEntry(Boolean isFirst)
		   at SharpYaml.Parser`1.StateMachine()
		   at SharpYaml.Parser`1.MoveNext()
		   at SharpYaml.Serialization.YamlReader.YamlReaderState.ReadCore()
		   at SharpYaml.Serialization.YamlReader.YamlReaderState.Read()
		   at SharpYaml.Serialization.YamlReader.Read()
		   at SharpYaml.Serialization.Converters.YamlUntypedObjectConverter.Read(YamlReader reader, Type typeToConvert)
		   at SharpYaml.Serialization.Converters.YamlUntypedObjectConverter.Read(YamlReader reader, Type typeToConvert)
		   at SharpYaml.Serialization.Converters.YamlUntypedObjectConverter.Read(YamlReader reader, Type typeToConvert)
		   at Elastic.Documentation.AppliesTo.ApplicableToYamlConverter.ReadUntyped(YamlReader reader) in {repo}/src/Elastic.Documentation.Configuration/Converters/ApplicableToYamlConverter.cs:line 41
		   at Elastic.Documentation.AppliesTo.ApplicableToYamlConverter.Read(YamlReader reader) in {repo}/src/Elastic.Documentation.Configuration/Converters/ApplicableToYamlConverter.cs:line 94
		   at SharpYaml.Serialization.YamlConverter`1.Read(YamlReader reader, Type typeToConvert)
		   at Elastic.Markdown.Myst.DocsBuilderYamlStaticContext.ReadValue4(YamlReader reader, RuntimeCustomConverterCache runtimeConverters) in {repo}/.artifacts/obj/Elastic.Markdown/release/SharpYaml.SourceGenerator/SharpYaml.SourceGeneration.YamlSerializerContextGenerator/DocsBuilderYamlStaticContext.g.cs:line 2768
		   at Elastic.Markdown.Myst.DocsBuilderYamlStaticContext.GeneratedTypeInfo4.Read(YamlReader reader) in {repo}/.artifacts/obj/Elastic.Markdown/release/SharpYaml.SourceGenerator/SharpYaml.SourceGeneration.YamlSerializerContextGenerator/DocsBuilderYamlStaticContext.g.cs:line 485
		   at SharpYaml.YamlTypeInfo`1.ReadAsObject(YamlReader reader)
		   at SharpYaml.YamlSerializer.DeserializeCore(YamlTypeInfo typeInfo, String yaml)
		   at SharpYaml.YamlSerializer.Deserialize(String yaml, Type returnType, YamlSerializerOptions options)
		   at SharpYaml.YamlSerializer.Deserialize[T](String yaml, YamlSerializerOptions options)
		   at Elastic.Markdown.Myst.YamlSerialization.Deserialize[T](String yaml, ProductsConfiguration products, Func`2 configure) in {repo}/src/Elastic.Markdown/Myst/YamlSerialization.cs:line 59
		   at Elastic.Markdown.Myst.YamlSerialization.Deserialize[T](String yaml, ProductsConfiguration products) in {repo}/src/Elastic.Markdown/Myst/YamlSerialization.cs:line 21
		   at Elastic.Markdown.Myst.Directives.DirectiveHtmlRenderer.ParseApplicableTo(String yaml, DirectiveBlock block) in {repo}/src/Elastic.Markdown/Myst/Directives/DirectiveHtmlRenderer.cs:line 648
		"""
			);

	[Test]
	public void FallsBackToAKeyDerivedFromTheDefinition() =>
		Block!.OfType<AppliesItemBlock>().Single().SyncKey.Should().StartWith("applies-");
}
