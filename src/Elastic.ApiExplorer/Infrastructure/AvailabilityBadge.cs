// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Site.ApplicabilityBadges;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// Builds the same <c>applies_to</c> badge Markdown pages render, from the <c>x-state</c> of an operation, a parameter or a
/// schema. Null when there is no x-state or it carries nothing worth a badge.
/// </summary>
public static class AvailabilityBadge
{
	public static ApplicableToViewModel? From(
		IDictionary<string, IOpenApiExtension>? extensions,
		VersioningSystemCatalog? versionsConfig,
		bool designSystem = false
	) => Create(XStateApplicability.From(extensions), versionsConfig, designSystem: designSystem);

	/// <summary>
	/// The badge of an operation. <c>x-state</c> wins; an operation that is only flagged <c>x-beta: true</c> gets the same
	/// <c>beta</c> badge, with the standard lifecycle popover, instead of a bespoke label.
	/// </summary>
	public static ApplicableToViewModel? FromOperation(
		OpenApiOperation operation,
		VersioningSystemCatalog? versionsConfig,
		bool showTooltip = true,
		bool designSystem = false
	) =>
		Create(
			XStateApplicability.From(operation.Extensions) ??
				(OpenApiExtensionReader.IsBeta(operation) ? XStateApplicability.Parse("Beta") : null),
			versionsConfig,
			showTooltip,
			designSystem
		);

	public static ApplicableToViewModel? Create(
		ApplicableTo? applicableTo,
		VersioningSystemCatalog? versionsConfig,
		bool showTooltip = true,
		bool designSystem = false
	)
	{
		if (applicableTo is null || versionsConfig is null || !XStateApplicability.IsWorthABadge(applicableTo))
			return null;

		return new ApplicableToViewModel
		{
			AppliesTo = applicableTo,
			Inline = true,
			ShowTooltip = showTooltip,
			VersionsConfig = versionsConfig,
			DesignSystem = designSystem
		};
	}
}
