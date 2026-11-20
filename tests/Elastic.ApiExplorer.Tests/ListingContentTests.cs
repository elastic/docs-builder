// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// <see cref="ListingContent"/> hashes a listing so a repeat can link to an identical one. A field it leaves out makes two
/// different listings look the same, so every <see cref="ApiProperty"/> member is either hashed or named here with a reason.
/// </summary>
public class ListingContentTests
{
	/// <summary>Plain values that the hash reads directly; each is changed in turn below.</summary>
	private static readonly string[] HashedScalars =
	[
		"Name",
		"IsRequired",
		"IsNullable",
		"IsRecursive",
		"ShowDeprecatedBadge",
		"DescriptionMarkdown",
		"EnumValues",
		"DefaultValue"
	];

	/// <summary>Hashed through one of their values (a type's text, a link's URL, the children's own hash).</summary>
	private static readonly string[] HashedThroughValues =
	[
		"Type",
		"Availability",
		"ExternalDocs",
		"Union",
		"AlsoIncludes",
		"Requires",
		"SingleOrArray",
		"TypeLink",
		"Repeats",
		"Children"
	];

	private static readonly Dictionary<string, string> Unhashed = new()
	{
		["Schema"] = "the raw schema; what a row shows of it is hashed through Type, the constraints and the children",
		["AnchorId"] = "where the row sits, so identical listings under different rows still match",
		["JsonPath"] = "where the row sits",
		["Depth"] = "where the row sits",
		["IsLast"] = "where the row sits",
		["IsRequest"] = "a property of the page, the same for every row in it",
		["DescriptionHtml"] = "rendered from DescriptionMarkdown, which is hashed",
		["Constraints"] = "already part of Type.Text",
		["OptionalRoute"] = "only a parameter row has one, and a parameter list is not a shared listing",
		["IsOptionalPath"] = "only a parameter row has one",
		["Example"] = "only a parameter row has one",
		["ArrayItemTypeName"] = "always null: the type text already reads as an array",
		["IsCollapsible"] = "derived from the children",
		["DefaultExpanded"] = "derived from the children",
		["NestedCount"] = "derived from the children"
	};

	[Test]
	public void ApiProperty_EveryMember_IsHashedOrExcludedWithAReason()
	{
		var members = typeof(ApiProperty).GetProperties().Where(static p => p.SetMethod is not null).Select(static p => p.Name);

		members.Should().BeEquivalentTo(
			[.. HashedScalars, .. HashedThroughValues, .. Unhashed.Keys],
			"a new member must be hashed by ListingContent or added to Unhashed with the reason it can be left out"
		);
	}

	[Test]
	[MethodDataSource(nameof(ScalarNames))]
	public async Task Of_ChangingAHashedScalar_ChangesTheHash(string member)
	{
		var original = await RowAsync();
		var property = typeof(ApiProperty).GetProperty(member)!;
		var changed = Clone(original);

		property.SetValue(changed, Changed(property.GetValue(original), property.PropertyType));

		HashOf(changed).Should().NotBe(HashOf(original), $"{member} is part of what a listing says");
	}

	public static IEnumerable<string> ScalarNames() => HashedScalars;

	private static async Task<ApiProperty> RowAsync()
	{
		var document = await LoadSpecAsync(
			WithSchemas("""{ "Holder": { "type": "object", "properties": { "a": { "type": "string" } } } }""")
		);
		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!;
		return list.Items.Single();
	}

	private static string HashOf(ApiProperty row) =>
		ListingContent.Of(
			new ApiPropertyChildren { Kind = ChildKind.PropertyList, UseHidden = false, Properties = new ApiPropertyList([row]) },
			new PageShapes()
		);

	/// <summary>A copy to change: the record's own <c>with</c> clone, since a member is picked by name here.</summary>
	private static ApiProperty Clone(ApiProperty row) =>
		(ApiProperty)typeof(ApiProperty).GetMethod("<Clone>$", BindingFlags.Instance | BindingFlags.Public)!.Invoke(row, null)!;

	private static object Changed(object? value, Type type) => value switch
	{
		bool flag => !flag,
		string text => text + "x",
		IReadOnlyList<string> list => (IReadOnlyList<string>)[.. list, "extra"],
		null when type == typeof(string) => "x",
		_ => throw new NotSupportedException($"Add a change for {type.Name} to this test.")
	};
}
