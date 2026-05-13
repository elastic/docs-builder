// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.Diagnostics;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// Renders the "What's new" panel for a product. Two usage shapes:
///
/// <para><b>Centralized lookup (preferred):</b></para>
/// <code>
/// :::{whats-new}
/// :product: kibana
/// :::
/// </code>
/// <para>The directive looks the product key up in <c>config/whats-new.yml</c>
/// and renders the data declared there. Authors edit one file; any page can
/// surface the panel.</para>
///
/// <para><b>Inline override:</b> if no <c>:product:</c> option is provided,
/// the directive expects a YAML body declaring <c>title</c>, <c>items</c>,
/// etc. directly. Useful for one-offs that don't belong in the central
/// feed.</para>
/// </summary>
public class WhatsNewBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	// Key the cache by ConfigurationFileProvider identity rather than the temp file path.
	// Each build gets a fresh provider with a fresh GUID-named temp file, so a path-keyed
	// cache would never hit across builds yet would accumulate entries forever in a
	// long-running process. ConditionalWeakTable scopes the cache to the provider's
	// lifetime — entries are reclaimed automatically when the provider is GC'd.
	private static readonly ConditionalWeakTable<ConfigurationFileProvider, Lazy<WhatsNewConfig?>> CentralConfigCache = [];

	public override string Directive => "whats-new";

	public WhatsNewData Data { get; private set; } = WhatsNewData.Empty;

	public override void FinalizeAndValidate(ParserContext context)
	{
		var product = Prop("product");

		if (!string.IsNullOrWhiteSpace(product))
		{
			var resolved = LoadFromCentralConfig(product);
			if (resolved is null)
			{
				this.EmitError($"{{whats-new}} :product: '{product}' was not found in config/whats-new.yml.");
				return;
			}
			Data = resolved;
			ValidateLinks(context);
			return;
		}

		var yaml = HubYamlBody.Extract(this, new BuildContextFileReader(Build.ReadFileSystem));
		if (yaml is null)
		{
			this.EmitError("{whats-new} requires either a `:product:` option or a YAML body.");
			return;
		}

		try
		{
			Data = YamlSerialization.Deserialize<WhatsNewData>(yaml, Build.ProductsConfiguration) ?? WhatsNewData.Empty;
		}
		catch (YamlException ex)
		{
			this.EmitError($"{{whats-new}} YAML parse error: {ex.Message}");
			return;
		}

		ValidateLinks(context);
	}

	private void ValidateLinks(ParserContext context)
	{
		foreach (var link in Data.ReleaseLinks)
			link.Url = HubLinkValidator.ValidateAndResolve(link.Url, this, context);
		foreach (var item in Data.Items)
			item.Link = HubLinkValidator.ValidateAndResolve(item.Link, this, context);
	}

	private WhatsNewData? LoadFromCentralConfig(string productKey)
	{
		var provider = Build.ConfigurationFileProvider;
		var configFile = provider.WhatsNewFile;
		if (configFile is null || !configFile.Exists)
			return null;

		var lazy = CentralConfigCache.GetValue(provider, _ => new Lazy<WhatsNewConfig?>(() =>
		{
			try
			{
				var yaml = Build.ReadFileSystem.File.ReadAllText(configFile.FullName);
				return YamlSerialization.Deserialize<WhatsNewConfig>(yaml, Build.ProductsConfiguration);
			}
			catch
			{
				return null;
			}
		}));
		var config = lazy.Value;

		if (config?.Products is null)
			return null;
		return config.Products.TryGetValue(productKey, out var data) ? data : null;
	}

	public override IEnumerable<string> GeneratedAnchors =>
		string.IsNullOrWhiteSpace(Data.Id) ? [] : [Data.Id];
}

[YamlSerializable]
public record WhatsNewConfig
{
	[YamlMember(Alias = "products")]
	public Dictionary<string, WhatsNewData> Products { get; set; } = [];
}

[YamlSerializable]
public record WhatsNewData
{
	[YamlMember(Alias = "title")]
	public string? Title { get; set; }

	[YamlMember(Alias = "id")]
	public string? Id { get; set; }

	[YamlMember(Alias = "badge")]
	public string? Badge { get; set; } = "New";

	[YamlMember(Alias = "release-links")]
	public LinkCardLink[] ReleaseLinks { get; set; } = [];

	[YamlMember(Alias = "items")]
	public WhatsNewItem[] Items { get; set; } = [];

	public static WhatsNewData Empty { get; } = new();
}

[YamlSerializable]
public record WhatsNewItem
{
	[YamlMember(Alias = "title")]
	public string? Title { get; set; }

	[YamlMember(Alias = "description")]
	public string? Description { get; set; }

	[YamlMember(Alias = "link")]
	public string? Link { get; set; }

	[YamlMember(Alias = "meta")]
	public string? Meta { get; set; }
}
