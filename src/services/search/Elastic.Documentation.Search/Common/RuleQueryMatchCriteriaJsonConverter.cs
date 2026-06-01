// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elastic.Documentation.Search.Common;

/// <summary>
/// Serializes <c>Elastic.Internal.Search.Elasticsearch.RuleQueryMatchCriteria</c>, which is internal to the
/// Elasticsearch search package and therefore not covered by public <see cref="JsonSerializerContext"/> types.
/// </summary>
internal sealed class RuleQueryMatchCriteriaJsonConverterFactory : JsonConverterFactory
{
	public static readonly RuleQueryMatchCriteriaJsonConverterFactory Instance = new();

	[DynamicDependency(
		DynamicallyAccessedMemberTypes.All,
		"Elastic.Internal.Search.Elasticsearch.RuleQueryMatchCriteria",
		"Elastic.Internal.Search.Elasticsearch")]
	private static readonly Type RuleQueryMatchCriteriaType = Type.GetType(
		"Elastic.Internal.Search.Elasticsearch.RuleQueryMatchCriteria, Elastic.Internal.Search.Elasticsearch",
		throwOnError: true)!;

	public override bool CanConvert(Type typeToConvert) => typeToConvert == RuleQueryMatchCriteriaType;

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
		RuleQueryMatchCriteriaJsonConverter.Instance;
}

internal sealed class RuleQueryMatchCriteriaJsonConverter : JsonConverter<object>
{
	public static readonly RuleQueryMatchCriteriaJsonConverter Instance = new();

	public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		throw new NotSupportedException();

	public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
	{
		if (value is null)
		{
			writer.WriteNullValue();
			return;
		}

		var queryString = RuleQueryMatchCriteriaAccessors.GetQueryString(value);
		writer.WriteStartObject();
		writer.WriteString("query_string", queryString);
		writer.WriteEndObject();
	}
}

internal static class RuleQueryMatchCriteriaAccessors
{
	[DynamicDependency(
		DynamicallyAccessedMemberTypes.PublicProperties,
		"Elastic.Internal.Search.Elasticsearch.RuleQueryMatchCriteria",
		"Elastic.Internal.Search.Elasticsearch")]
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	private static readonly Type RuleQueryMatchCriteriaType = Type.GetType(
		"Elastic.Internal.Search.Elasticsearch.RuleQueryMatchCriteria, Elastic.Internal.Search.Elasticsearch",
		throwOnError: true)!;

	private static readonly PropertyInfo QueryStringProperty = RuleQueryMatchCriteriaType.GetProperty(
		"QueryString",
		BindingFlags.Public | BindingFlags.Instance)!;

	[UnconditionalSuppressMessage(
		"Trimming",
		"IL2075",
		Justification = "RuleQueryMatchCriteria.QueryString is preserved via DynamicDependency on RuleQueryMatchCriteriaType.")]
	public static string GetQueryString(object target) => (string)QueryStringProperty.GetValue(target)!;
}
