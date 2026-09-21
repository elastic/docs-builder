// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Model;

namespace Elastic.ApiExplorer.Tests;

public class SchemaHelpersTests
{
	[Theory]
	[InlineData("string")]
	[InlineData("number")]
	[InlineData("boolean")]
	[InlineData("object")]
	[InlineData("strings")]
	public void PrimitiveCssClassOrNull_Group1Atoms_ReturnsTypePrimitive(string name) =>
		SchemaHelpers.PrimitiveCssClassOrNull(name).Should().Be("type-primitive");

	[Theory]
	[InlineData("Field")]
	[InlineData("TaskSettings")]
	[InlineData("string[]")]
	[InlineData(null)]
	public void PrimitiveCssClassOrNull_NonAtoms_ReturnsNull(string? name) => SchemaHelpers.PrimitiveCssClassOrNull(name).Should().BeNull();

	[Theory]
	[InlineData("Field")]
	[InlineData("Id")]
	[InlineData("uint")]
	public void ValueCssClassOrNull_Group2Aliases_ReturnsTypeValue(string name) =>
		SchemaHelpers.ValueCssClassOrNull(name).Should().Be("type-value");

	[Theory]
	[InlineData("string")]
	[InlineData("TaskSettings")]
	[InlineData(null)]
	public void ValueCssClassOrNull_NonAliases_ReturnsNull(string? name) => SchemaHelpers.ValueCssClassOrNull(name).Should().BeNull();

	[Fact]
	public void TypeAtomCssClassOrNull_PrefersPrimitiveOverValue() =>
		SchemaHelpers.TypeAtomCssClassOrNull("string").Should().Be("type-primitive");

	[Fact]
	public void TypeAtomCssClassOrNull_Alias_ReturnsTypeValue() => SchemaHelpers.TypeAtomCssClassOrNull("Field").Should().Be("type-value");

	[Fact]
	public void TypeAtomCssClassOrNull_NamedObject_ReturnsTypeObject() =>
		SchemaHelpers.TypeAtomCssClassOrNull("TaskSettings").Should().Be("type-object");

	[Fact]
	public void TypeAtomCssClassOrNull_LinkedContainer_ReturnsTypeLinked() =>
		SchemaHelpers.TypeAtomCssClassOrNull("QueryContainer").Should().Be("type-linked");

	[Fact]
	public void TypeAtomCssClassOrNull_Unknown_ReturnsNull() => SchemaHelpers.TypeAtomCssClassOrNull("unknown").Should().BeNull();

	[Theory]
	[InlineData("string | string[]")]
	[InlineData("string[]")]
	[InlineData("string to HighlightField")]
	public void TypeAtomCssClassOrNull_CompoundFormula_ReturnsNull(string name) =>
		SchemaHelpers.TypeAtomCssClassOrNull(name).Should().BeNull();

	[Fact]
	public void UnionOptionClasses_NamedObject_IncludesTypeObject() =>
		SchemaHelpers.UnionOptionClasses(true, "TaskSettings").Should().Be("union-type-option type-object");

	[Fact]
	public void UnionOptionClasses_LinkedContainer_IncludesTypeLinked() =>
		SchemaHelpers.UnionOptionClasses(true, "AggregationContainer").Should().Be("union-type-option type-linked");

	[Fact]
	public void UnionOptionClasses_PrimitiveTypeOption_IncludesTypePrimitive() =>
		SchemaHelpers.UnionOptionClasses(true, "string").Should().Be("union-type-option type-primitive");

	[Fact]
	public void UnionOptionClasses_ValueTypeOption_IncludesTypeValue() =>
		SchemaHelpers.UnionOptionClasses(true, "Field").Should().Be("union-type-option type-value");

	[Fact]
	public void UnionOptionClasses_Literal_DoesNotIncludeTypePrimitive() =>
		SchemaHelpers.UnionOptionClasses(false, "false_positive").Should().Be("union-option");
}
