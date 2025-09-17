// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;

namespace Elastic.Markdown.Myst.Components;

public record ApplicabilityItem(
	string Key,
	Applicability PrimaryApplicability,
	IReadOnlyList<Applicability> AllApplicabilities,
	ApplicabilityRenderer.ApplicabilityRenderData RenderData,
	ApplicabilityMappings.ApplicabilityDefinition ApplicabilityDefinition
)
{
	public ApplicabilityItem(string key, Applicability applicability, ApplicabilityRenderer.ApplicabilityRenderData renderData)
		: this(key, applicability, [applicability], renderData, GetDefaultDefinition(key)) { }

	public Applicability Applicability => PrimaryApplicability;

	private static ApplicabilityMappings.ApplicabilityDefinition GetDefaultDefinition(string key) =>
		key switch
		{
			"Stack" => ApplicabilityMappings.Stack,
			"Serverless" => ApplicabilityMappings.Serverless,
			_ => ApplicabilityMappings.Product
		};
}
