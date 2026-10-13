// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Site.ApplicabilityBadges;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// Builds the same <c>applies_to</c> badge Markdown pages render, from an operation's or schema's <c>x-state</c>.
/// Null when there is no x-state or it carries nothing worth a badge.
/// </summary>
public static class AvailabilityBadge
{
	public static ApplicableToViewModel? FromOperation(OpenApiOperation operation, VersionsConfiguration? versionsConfig) =>
		Create(XStateApplicability.FromOperation(operation), versionsConfig);

	public static ApplicableToViewModel? FromSchema(IOpenApiSchema schema, VersionsConfiguration? versionsConfig) =>
		Create(XStateApplicability.FromSchema(schema), versionsConfig);

	private static ApplicableToViewModel? Create(ApplicableTo? applicableTo, VersionsConfiguration? versionsConfig)
	{
		if (applicableTo is null || versionsConfig is null || !XStateApplicability.IsWorthABadge(applicableTo))
			return null;

		return new ApplicableToViewModel { AppliesTo = applicableTo, Inline = true, VersionsConfig = versionsConfig };
	}
}
