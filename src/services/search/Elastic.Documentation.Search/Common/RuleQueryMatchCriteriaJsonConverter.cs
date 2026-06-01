// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elastic.Documentation.Search.Common;

/// <summary>
/// Serializes <c>Elastic.Internal.Search.Elasticsearch.RuleQueryMatchCriteria</c>, which is internal to the
/// Elasticsearch search package and therefore not covered by public <see cref="JsonSerializerContext"/> types.
/// </summary>
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
	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_QueryString")]
	private static extern string GetQueryStringInternal(object target);

	public static string GetQueryString(object target) => GetQueryStringInternal(target);
}
