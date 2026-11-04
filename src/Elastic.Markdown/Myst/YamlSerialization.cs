// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.Changelog;
using Elastic.Markdown.Myst.Directives.Contributors;
using Elastic.Markdown.Myst.Directives.Settings;
using Elastic.Markdown.Myst.FrontMatter;
using SharpYaml;
using SharpYaml.Serialization;

namespace Elastic.Markdown.Myst;

public static class YamlSerialization
{
	public static T Deserialize<T>(string yaml, ProductsConfiguration products) => Deserialize<T>(yaml, products, o => o);

	/// <summary>
	/// Deserializes YAML content (e.g. front matter or a directive body), reporting unknown keys as errors
	/// through <paramref name="reporting"/> instead of silently skipping them.
	/// </summary>
	public static T Deserialize<T>(string yaml, ProductsConfiguration products, YamlUnmappedMemberReporting reporting) =>
		Deserialize<T>(yaml, products, o => o.WithUnmappedReporting(reporting));

	private static T Deserialize<T>(
		string yaml,
		ProductsConfiguration products,
		Func<YamlSerializerOptions, YamlSerializerOptions> configure
	)
	{
		var context = DocsBuilderYamlStaticContext.Default;
		var options = context.CreateOptions(
			o => configure(o with
			{
				Converters =
				[
					new SemVersionConverter(),
					new MarkdownPageLayoutConverter(),
					new SettingMutabilityConverter(),
					new ProductConverter(products),
					new ApplicableToYamlConverter(products.PublicReferenceProducts.Keys),
					new ListingFrontMatterConverter()
				],
				// Settings YAML anchors/aliases a setting shared across groups (e.g. `&common ... *common`).
				// Without this the generated reader throws when it hits the alias token. See the matching
				// comment on ConfigurationFileProvider.DeserializerOptions.
				ReferenceHandling = YamlReferenceHandling.Preserve
			})
		);

		// A null or comment-only YAML document deserializes to null rather than throwing. Preserve that
		// nullable result instead of raising: hub directives (e.g. {get-started}, {link-card}) fall back to
		// an empty data record via `?? ...Empty` and continue validating, same as the pre-migration behavior.
		return YamlSerializer.Deserialize<T>(yaml, options)!;
	}
}

/// <summary>
/// Handles both <c>listing: group-name</c> (scalar shorthand) and <c>listing: {group: name}</c> (mapping).
/// </summary>
internal class ListingFrontMatterConverter : YamlConverter<ListingFrontMatter>
{
	private static readonly string[] KnownKeys = ["group"];

	public override ListingFrontMatter? Read(YamlReader reader)
	{
		if (reader.TokenType == YamlTokenType.Scalar)
		{
			var scalarValue = reader.ScalarValue;
			_ = reader.Read();
			return new ListingFrontMatter { Group = string.IsNullOrWhiteSpace(scalarValue) ? null : scalarValue };
		}

		if (reader.TokenType != YamlTokenType.StartMapping)
		{
			reader.Skip();
			return null;
		}

		_ = reader.Read(); // consume StartMapping
		var result = new ListingFrontMatter();
		while (reader.TokenType != YamlTokenType.EndMapping)
		{
			if (reader.TokenType != YamlTokenType.Scalar)
			{
				reader.Skip();
				continue;
			}
			var key = reader.ScalarValue;
			_ = reader.Read();
			if (key == "group" && reader.TokenType == YamlTokenType.Scalar)
			{
				var value = reader.ScalarValue;
				_ = reader.Read();
				result.Group = string.IsNullOrWhiteSpace(value) ? null : value;
			}
			else if (key != "group")
				reader.SkipOrReportUnmapped(typeof(ListingFrontMatter), KnownKeys, key!, result);
			else
				reader.Skip();
		}
		_ = reader.Read(); // consume EndMapping

		return result;
	}

	public override void Write(YamlWriter writer, ListingFrontMatter value) =>
		throw new NotSupportedException("ListingFrontMatter YAML serialization is not supported; it is only ever read from YAML.");
}

[YamlSerializable(typeof(YamlSettings))]
[YamlSerializable(typeof(List<ContributorEntry>))]
[YamlSerializable(typeof(ChangelogDirectiveConfigYaml))]
[YamlSerializable(typeof(ChangelogDirectiveBundleConfigYaml))]
[YamlSerializable(typeof(ApplicableTo))]
[YamlSerializable(typeof(YamlFrontMatter))]
[YamlSerializable(typeof(Elastic.Markdown.Myst.Directives.Hub.LinkCardData))]
[YamlSerializable(typeof(Elastic.Markdown.Myst.Directives.Hub.GetStartedData))]
[YamlSerializable(typeof(Elastic.Markdown.Myst.Directives.Hub.WhatsNewData))]
[YamlSerializable(typeof(Elastic.Markdown.Myst.Directives.Hub.WhatsNewConfig))]
[YamlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
public partial class DocsBuilderYamlStaticContext : YamlSerializerContext;
