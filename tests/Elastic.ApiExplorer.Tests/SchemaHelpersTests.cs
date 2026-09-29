// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Model;

namespace Elastic.ApiExplorer.Tests;

public class SchemaHelpersTests
{
	[Test]
	[Arguments("string")]
	[Arguments("number")]
	[Arguments("boolean")]
	[Arguments("object")]
	[Arguments("strings")]
	public void PrimitiveCssClassOrNull_Group1Atoms_ReturnsTypePrimitive(string name) =>
		SchemaHelpers.PrimitiveCssClassOrNull(name).Should().Be("type-primitive");

	[Test]
	[Arguments("Field")]
	[Arguments("TaskSettings")]
	[Arguments("string[]")]
	[Arguments(null)]
	public void PrimitiveCssClassOrNull_NonAtoms_ReturnsNull(string? name) => SchemaHelpers.PrimitiveCssClassOrNull(name).Should().BeNull();

	[Test]
	[Arguments("Field")]
	[Arguments("Id")]
	[Arguments("uint")]
	public void ValueCssClassOrNull_Group2Aliases_ReturnsTypeValue(string name) =>
		SchemaHelpers.ValueCssClassOrNull(name).Should().Be("type-value");

	[Test]
	[Arguments("string")]
	[Arguments("TaskSettings")]
	[Arguments(null)]
	public void ValueCssClassOrNull_NonAliases_ReturnsNull(string? name) => SchemaHelpers.ValueCssClassOrNull(name).Should().BeNull();

	[Test]
	public void TypeAtomCssClassOrNull_PrefersPrimitiveOverValue() =>
		SchemaHelpers.TypeAtomCssClassOrNull("string").Should().Be("type-primitive");

	[Test]
	public void TypeAtomCssClassOrNull_Alias_ReturnsTypeValue() => SchemaHelpers.TypeAtomCssClassOrNull("Field").Should().Be("type-value");

	[Test]
	public void TypeAtomCssClassOrNull_NamedObject_ReturnsTypeObject() =>
		SchemaHelpers.TypeAtomCssClassOrNull("TaskSettings").Should().Be("type-object");

	[Test]
	public void TypeAtomCssClassOrNull_LinkedContainer_ReturnsTypeLinked() =>
		SchemaHelpers.TypeAtomCssClassOrNull("QueryContainer").Should().Be("type-linked");

	[Test]
	public void TypeAtomCssClassOrNull_Unknown_ReturnsNull() => SchemaHelpers.TypeAtomCssClassOrNull("unknown").Should().BeNull();

	[Test]
	[Arguments("string | string[]")]
	[Arguments("string[]")]
	[Arguments("string to HighlightField")]
	public void TypeAtomCssClassOrNull_CompoundFormula_ReturnsNull(string name) =>
		SchemaHelpers.TypeAtomCssClassOrNull(name).Should().BeNull();

	[Test]
	public void UnionOptionClasses_NamedObject_IncludesTypeObject() =>
		SchemaHelpers.UnionOptionClasses(true, "TaskSettings").Should().Be("union-type-option type-object");

	[Test]
	public void UnionOptionClasses_LinkedContainer_IncludesTypeLinked() =>
		SchemaHelpers.UnionOptionClasses(true, "AggregationContainer").Should().Be("union-type-option type-linked");

	[Test]
	public void UnionOptionClasses_PrimitiveTypeOption_IncludesTypePrimitive() =>
		SchemaHelpers.UnionOptionClasses(true, "string").Should().Be("union-type-option type-primitive");

	[Test]
	public void UnionOptionClasses_ValueTypeOption_IncludesTypeValue() =>
		SchemaHelpers.UnionOptionClasses(true, "Field").Should().Be("union-type-option type-value");

	[Test]
	public void UnionOptionClasses_Literal_DoesNotIncludeTypePrimitive() =>
		SchemaHelpers.UnionOptionClasses(false, "false_positive").Should().Be("union-option");

	[Test]
	[Arguments("Security_Lists_API_ListMetadata")]
	[Arguments("Security_Lists_API_ListType")]
	[Arguments("Cases_case_description")]
	public void IsInternalSchemaName_CodegenIds_ReturnsTrue(string name) => SchemaHelpers.IsInternalSchemaName(name).Should().BeTrue();

	[Test]
	[Arguments("Field")]
	[Arguments("SearchMode")]
	[Arguments("QueryContainer")]
	[Arguments("string")]
	[Arguments("Field | Field[]")]
	[Arguments(null)]
	public void IsInternalSchemaName_ReadableNames_ReturnsFalse(string? name) =>
		SchemaHelpers.IsInternalSchemaName(name).Should().BeFalse();

	[Test]
	[Arguments("Security_Lists_API_PlatformErrorResponse", "PlatformErrorResponse")]
	[Arguments("Security_Lists_API_SiemErrorResponse", "SiemErrorResponse")]
	[Arguments("Field", "Field")]
	[Arguments("QueryContainer", "QueryContainer")]
	public void ReadableSchemaName_CodegenIds_UsesLastSegment(string input, string expected) =>
		SchemaHelpers.ReadableSchemaName(input).Should().Be(expected);
}
