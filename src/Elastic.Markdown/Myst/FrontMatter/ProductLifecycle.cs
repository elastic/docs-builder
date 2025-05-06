// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.FrontMatter;

[YamlSerializable]
public enum ProductLifecycle
{
	// technical preview (exists in current docs system per https://github.com/elastic/docs?tab=readme-ov-file#beta-dev-and-preview-experimental)
	[YamlMember(Alias = "preview")]
	TechnicalPreview,
	// beta (ditto)
	[YamlMember(Alias = "beta")]
	Beta,
	// dev (ditto, though it's uncertain whether it's ever used or still needed)
	[YamlMember(Alias = "development")]
	Development,
	// deprecated (exists in current docs system per https://github.com/elastic/docs?tab=readme-ov-file#additions-and-deprecations)
	[YamlMember(Alias = "deprecated")]
	Deprecated,
	// coming (ditto)
	[YamlMember(Alias = "coming")]
	Planned,
	// discontinued (historically we've immediately removed content when the feature ceases to be supported, but this might not be the case with pages that contain information that spans versions)
	[YamlMember(Alias = "discontinued")]
	Discontinued,
	// unavailable (for content that doesn't exist in a specific context and is never coming or not coming anytime soon)
	[YamlMember(Alias = "unavailable")]
	Unavailable,
	// ga (replaces "added" in the current docs system since it was not entirely clear how/if that overlapped with beta/preview states)
	[YamlMember(Alias = "ga")]
	GenerallyAvailable
}
