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
/// Builds the same <c>applies_to</c> badge Markdown pages render, from the <c>x-state</c> of an operation, a parameter or a
/// schema. Null when there is no x-state or it carries nothing worth a badge.
/// </summary>
public static class AvailabilityBadge
{
	public static ApplicableToViewModel? From(IDictionary<string, IOpenApiExtension>? extensions, VersionsConfiguration? versionsConfig) =>
		Create(XStateApplicability.From(extensions), versionsConfig);

	public static ApplicableToViewModel? Create(ApplicableTo? applicableTo, VersionsConfiguration? versionsConfig)
	{
		if (applicableTo is null || versionsConfig is null || !XStateApplicability.IsWorthABadge(applicableTo))
			return null;

		return new ApplicableToViewModel { AppliesTo = applicableTo, Inline = true, VersionsConfig = versionsConfig };
	}
}
