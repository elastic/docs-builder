// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Elastic.Documentation.Search.Common;

/// <summary>
/// Supplies <see cref="JsonTypeInfo"/> for internal Elasticsearch search query types not declared on public contexts.
/// </summary>
internal sealed class RuleQueryMatchCriteriaTypeInfoResolver(IJsonTypeInfoResolver inner) : IJsonTypeInfoResolver
{
	[DynamicDependency(
		DynamicallyAccessedMemberTypes.All,
		"Elastic.Internal.Search.Elasticsearch.RuleQueryMatchCriteria",
		"Elastic.Internal.Search.Elasticsearch")]
	private static readonly Type RuleQueryMatchCriteriaType = Type.GetType(
		"Elastic.Internal.Search.Elasticsearch.RuleQueryMatchCriteria, Elastic.Internal.Search.Elasticsearch",
		throwOnError: true)!;

	private JsonTypeInfo? _ruleQueryMatchCriteriaTypeInfo;

	public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		if (type == RuleQueryMatchCriteriaType)
		{
			return _ruleQueryMatchCriteriaTypeInfo ??= CreateRuleQueryMatchCriteriaTypeInfo(options);
		}

		return inner.GetTypeInfo(type, options);
	}

	[UnconditionalSuppressMessage(
		"Trimming",
		"IL2026",
		Justification = "RuleQueryMatchCriteria is internal to Elastic.Internal.Search.Elasticsearch; serialization uses an UnsafeAccessor-based converter.")]
	[UnconditionalSuppressMessage(
		"AOT",
		"IL3050",
		Justification = "RuleQueryMatchCriteria is internal to Elastic.Internal.Search.Elasticsearch; serialization uses an UnsafeAccessor-based converter.")]
	private static JsonTypeInfo CreateRuleQueryMatchCriteriaTypeInfo(JsonSerializerOptions options) =>
		JsonTypeInfo.CreateJsonTypeInfo(RuleQueryMatchCriteriaType, options);
}
