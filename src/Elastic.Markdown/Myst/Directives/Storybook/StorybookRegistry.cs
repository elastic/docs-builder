// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elastic.Markdown.Myst.Directives.Storybook;

public sealed class StorybookRegistry
{
	[JsonPropertyName("schemaVersion")]
	public JsonElement SchemaVersion { get; init; }

	[JsonPropertyName("producer")]
	public string? Producer { get; init; }

	[JsonPropertyName("baseUrl")]
	public string? BaseUrl { get; init; }

	[JsonPropertyName("build")]
	public Dictionary<string, JsonElement>? Build { get; init; }

	[JsonPropertyName("stories")]
	public Dictionary<string, StorybookRegistryStory> Stories { get; init; } = [with(StringComparer.Ordinal)];
}

public sealed class StorybookRegistryStory
{
	[JsonPropertyName("alias")]
	public string? Alias { get; init; }

	[JsonPropertyName("docsId")]
	public string? DocsId { get; init; }

	[JsonPropertyName("storybookId")]
	public string? StorybookId { get; init; }

	[JsonPropertyName("title")]
	public string? Title { get; init; }

	[JsonPropertyName("name")]
	public string? Name { get; init; }

	[JsonPropertyName("height")]
	public int? Height { get; init; }

	[JsonPropertyName("renderMode")]
	public string? RenderMode { get; init; }

	[JsonPropertyName("inline")]
	public StorybookRegistryInline? Inline { get; init; }

	[JsonPropertyName("iframe")]
	public StorybookRegistryIframe? Iframe { get; init; }
}

public sealed class StorybookRegistryIframe
{
	[JsonPropertyName("url")]
	public string? Url { get; init; }
}

public sealed class StorybookRegistryInline
{
	[JsonPropertyName("entry")]
	public string? Entry { get; init; }

	[JsonPropertyName("bundleId")]
	public string? BundleId { get; init; }

	[JsonPropertyName("bootstrap")]
	public StorybookRegistryBootstrap? Bootstrap { get; init; }
}

public sealed class StorybookRegistryBootstrap
{
	[JsonPropertyName("publicPath")]
	public string? PublicPath { get; init; }

	[JsonPropertyName("scripts")]
	public IReadOnlyCollection<string> Scripts { get; init; } = [];

	[JsonPropertyName("styles")]
	public IReadOnlyCollection<string> Styles { get; init; } = [];
}

[JsonSerializable(typeof(StorybookRegistry))]
[JsonSerializable(typeof(StorybookRegistryBootstrap))]
internal sealed partial class StorybookRegistryJsonContext : JsonSerializerContext;
