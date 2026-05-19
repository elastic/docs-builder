// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.AppliesTo;

public enum ProductLifecycle
{
	// technical preview (exists in current docs system per https://github.com/elastic/docs?tab=readme-ov-file#beta-dev-and-preview-experimental)
	[JsonStringEnumMemberName("preview")]
	TechnicalPreview,
	// beta (ditto)
	[JsonStringEnumMemberName("beta")]
	Beta,
	// ga (replaces "added" in the current docs system since it was not entirely clear how/if that overlapped with beta/preview states)
	[JsonStringEnumMemberName("ga")]
	GenerallyAvailable,
	// deprecated (exists in current docs system per https://github.com/elastic/docs?tab=readme-ov-file#additions-and-deprecations)
	[JsonStringEnumMemberName("deprecated")]
	Deprecated,
	// removed content
	[JsonStringEnumMemberName("removed")]
	Removed,
	// unavailable (for content that doesn't exist in a specific context and is never coming or not coming anytime soon)
	[JsonStringEnumMemberName("unavailable")]
	Unavailable,

	// TODO remove these enum members in a future version when docs have been cleaned up
	// discontinued (historically we've immediately removed content when the feature ceases to be supported, but this might not be the case with pages that contain information that spans versions)
	[JsonStringEnumMemberName("discontinued")]
	Discontinued,
	// coming (ditto)
	[JsonStringEnumMemberName("planned")]
	Planned,
	// dev (ditto, though it's uncertain whether it's ever used or still needed)
	[JsonStringEnumMemberName("development")]
	Development,
}
