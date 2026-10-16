// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;
using SharpYaml;
using SharpYaml.Serialization;

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
public class WhatsNewBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	private const string WhatsNewFileName = "hub-whats-new.yml";

	private static readonly ConcurrentDictionary<string, (WhatsNewConfig? Config, IReadOnlyList<Diagnostic> Diagnostics)> CentralConfigCache = new();

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
				this.EmitError(
					$"{{whats-new}} :product: '{product}' was not found in {WhatsNewFileName} at the root of this documentation set."
				);
				return;
			}
			Data = ResolveLinks(resolved, context);
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
			Data = YamlSerialization.Deserialize<WhatsNewData>(yaml, Build.ProductsConfiguration, this.YamlReporting())
				?? WhatsNewData.Empty;
		}
		catch (YamlException ex)
		{
			this.EmitError($"{{whats-new}} YAML parse error: {ex.Message}");
			return;
		}

		Data = ResolveLinks(Data, context);
	}

	/// <summary>
	/// Returns a copy of the panel with its links resolved and its substitutions replaced. The
	/// source is never changed, because a <c>:product:</c> panel is shared through
	/// <see cref="CentralConfigCache"/> and every page must resolve its own substitutions and
	/// relative links from the original values.
	/// </summary>
	private WhatsNewData ResolveLinks(WhatsNewData source, ParserContext context) =>
		source with
		{
			// A release link label can carry a substitution, for example `{{version.stack}}`, so a
			// patch release does not mean editing the panel.
			ReleaseLinks =
			[
				.. source.ReleaseLinks.Select(
					link =>
						link with
						{
							Label = link.Label?.ReplaceSubstitutions(context),
							Url = DirectiveLinkValidator.ValidateAndResolve(link.Url, this, context)
						}
				)
			],
			UpgradeLink = source.UpgradeLink is { } upgrade
				? upgrade with { Url = DirectiveLinkValidator.ValidateAndResolve(upgrade.Url, this, context) }
				: null,
			Items =
			[
				.. source.Items.Select(
					item =>
						item with
						{
							Link = DirectiveLinkValidator.ValidateAndResolve(item.Link, this, context),
							Availability = [.. item.Availability.Select(entry => entry.ReplaceSubstitutions(context))]
						}
				)
			]
		};

	/// <summary>
	/// The panel content lives in the content repository, next to the pages that use it, so a
	/// writer edits it without touching the build tool. It is read from the current documentation
	/// set, which means the directive cannot render another repository's panel. In an isolated
	/// build the other repository is not checked out at all, so there would be no file to read.
	/// </summary>
	private WhatsNewData? LoadFromCentralConfig(string productKey)
	{
		var path = Path.Combine(Build.DocumentationSourceDirectory.FullName, WhatsNewFileName);
		if (!Build.ReadFileSystem.File.Exists(path))
			return null;

		// The parse only runs once per path, cached across builds (e.g. --watch). A rebuild that hits
		// the cache must still see the diagnostics the first build's parse produced, so every call
		// replays them into the current Build.Collector rather than relying on whichever collector
		// happened to trigger the parse.
		var (config, diagnostics) = CentralConfigCache.GetOrAdd(path, ParseCentralConfig);
		foreach (var diagnostic in diagnostics)
			Build.Collector.Write(diagnostic);

		if (config?.Products is null)
			return null;
		return config.Products.TryGetValue(productKey, out var data) ? data : null;
	}

	private (WhatsNewConfig? Config, IReadOnlyList<Diagnostic> Diagnostics) ParseCentralConfig(string path)
	{
		var diagnostics = new List<Diagnostic>();
		try
		{
			var yaml = Build.ReadFileSystem.File.ReadAllText(path);
			var config = YamlSerialization.Deserialize<WhatsNewConfig>(
				yaml,
				Build.ProductsConfiguration,
				new YamlUnmappedMemberReporting(diagnostics.Add, path)
			);
			return (config, diagnostics);
		}
		catch (YamlException e)
		{
			var message = $"Unable to parse {WhatsNewFileName}: {e.Message}{Environment.NewLine}{e}"
				+ (e.InnerException != null ? Environment.NewLine + e.InnerException : string.Empty);
			diagnostics.Add(new Diagnostic { Severity = Severity.Error, File = path, Message = message });
			return (null, diagnostics);
		}
	}

	public override IEnumerable<string> GeneratedAnchors => string.IsNullOrWhiteSpace(Data.Id) ? [] : [Data.Id];
}

public record WhatsNewConfig
{
	[YamlPropertyName("products")]
	public Dictionary<string, WhatsNewData> Products { get; set; } = [];
}

public record WhatsNewData
{
	[YamlPropertyName("title")]
	public string? Title { get; set; }

	[YamlPropertyName("id")]
	public string? Id { get; set; }

	[YamlPropertyName("intro")]
	public string? Intro { get; set; }

	[YamlPropertyName("release-links")]
	public LinkCardLink[] ReleaseLinks { get; set; } = [];

	[YamlPropertyName("upgrade-link")]
	public LinkCardLink? UpgradeLink { get; set; }

	[YamlPropertyName("items")]
	public WhatsNewItem[] Items { get; set; } = [];

	[YamlIgnore]
	public static WhatsNewData Empty { get; } = new();
}

public record WhatsNewItem
{
	[YamlPropertyName("title")]
	public string? Title { get; set; }

	[YamlPropertyName("description")]
	public string? Description { get; set; }

	[YamlPropertyName("link")]
	public string? Link { get; set; }

	/// <summary>
	/// Where the item is available, for example <c>9.5</c> and <c>Serverless</c>. The card shows the
	/// entries in order. When the list is empty the card falls back to <see cref="Date"/>.
	/// </summary>
	[YamlPropertyName("availability")]
	public string[] Availability { get; set; } = [];

	[YamlPropertyName("date")]
	public string? Date { get; set; }

	[YamlPropertyName("tag")]
	public string? Tag { get; set; }

	[YamlPropertyName("featured")]
	public bool Featured { get; set; }
}
