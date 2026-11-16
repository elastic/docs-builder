// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using SharpYaml;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

/// <summary>
/// YAML converter for the strict <c>api: &lt;key&gt;</c> schema. Only the RFC sequence shape is
/// accepted:
/// <code>
/// api:
///   &lt;key&gt;:
///     - spec: &lt;path&gt;       # optional local override
///       product: &lt;id&gt;      # required
///       children:            # optional
///         - file: getting-started.md
/// </code>
/// The legacy scalar ("api: key: path.json"), object ("api: key: { spec: path.json }"), and
/// intro/spec/outro sequence shapes are rejected with a precise <see cref="YamlException"/> so
/// docset authors get a clear migration error instead of silent misbehavior.
/// </summary>
internal static class ApiConfigurationShapeGuidance
{
	public const string Text = "Use the single-entry sequence form instead:\n"
		+ "  <key>:\n"
		+ "    - spec: <path>       # required; its basename resolves the remote version index\n"
		+ "      product: <id>      # required, must match a products.yml entry\n"
		+ "      repository: <org/repo> # optional; only needed if the spec is published from a\n"
		+ "                              # different repo than the current checkout\n"
		+ "      children:          # optional\n"
		+ "        - file: getting-started.md\n"
		+ "      catalog:           # optional; deployment tags on the API catalog\n"
		+ "        categories: [self, ece, ess, serverless]";
}

internal class ApiProductSequenceConverter(ApiProductEntryConverter entryConverter) : YamlConverter<ApiProductSequenceDto>
{
	public override ApiProductSequenceDto Read(YamlReader reader)
	{
		if (reader.TokenType != YamlTokenType.StartSequence)
		{
			throw new YamlException(
				reader.SourceName,
				reader.Start,
				reader.End,
				$"API configuration for this key must be a sequence with exactly one entry. {ApiConfigurationShapeGuidance.Text}"
			);
		}

		_ = reader.Read(); // consume StartSequence
		var entries = new List<ApiProductEntryDto>();
		while (reader.TokenType != YamlTokenType.EndSequence)
			entries.Add(entryConverter.Read(reader));
		_ = reader.Read(); // consume EndSequence

		return new ApiProductSequenceDto { Entries = entries };
	}

	public override void Write(YamlWriter writer, ApiProductSequenceDto value) =>
		throw new NotSupportedException(
			"ApiProductSequenceDto YAML serialization is not supported; api configuration is only ever read from YAML."
		);
}

internal class ApiProductEntryConverter : YamlConverter<ApiProductEntryDto>
{
	private static readonly string[] KnownEntryKeys = ["spec", "product", "repository", "children", "catalog"];
	private static readonly string[] KnownCatalogKeys = ["categories"];
	private static readonly string[] KnownChildKeys = ["file"];

	public override ApiProductEntryDto Read(YamlReader reader)
	{
		if (reader.TokenType != YamlTokenType.StartMapping)
		{
			throw new YamlException(
				reader.SourceName,
				reader.Start,
				reader.End,
				$"Each API entry must be a mapping with 'spec', 'product', and optional 'children' keys. {ApiConfigurationShapeGuidance.Text}"
			);
		}

		var entryStart = reader.Start;
		_ = reader.Read(); // consume MappingStart

		// SharpYaml's Mark.Line/Column are 0-based; +1 to match the 1-based convention used
		// everywhere else in this codebase for user-facing diagnostics.
		var entry = new ApiProductEntryDto { Line = entryStart.Line + 1, Column = entryStart.Column + 1 };
		var unknown = new List<string>();

		while (reader.TokenType != YamlTokenType.EndMapping)
		{
			var keyStart = reader.Start;
			var keyEnd = reader.End;
			var key = reader.GetScalarValue();
			_ = reader.Read();

			switch (key)
			{
				case "spec":
					ReadLocatedString(reader, out var specValue, out var specLine, out var specColumn);
					entry.Spec = specValue;
					entry.SpecLine = specLine;
					entry.SpecColumn = specColumn;
					break;
				case "product":
					ReadLocatedString(reader, out var productValue, out var productLine, out var productColumn);
					entry.Product = productValue;
					entry.ProductLine = productLine;
					entry.ProductColumn = productColumn;
					break;
				case "repository":
					ReadLocatedString(reader, out var repositoryValue, out var repositoryLine, out var repositoryColumn);
					entry.Repository = repositoryValue;
					entry.RepositoryLine = repositoryLine;
					entry.RepositoryColumn = repositoryColumn;
					break;
				case "children":
					entry.Children = ReadChildren(reader);
					break;
				case "catalog":
					entry.Catalog = ReadCatalog(reader);
					break;
				case "file":
					throw new YamlException(
						reader.SourceName,
						keyStart,
						keyEnd,
						$"'file:' entries directly in the api sequence (legacy intro/outro shape) are no longer supported. {ApiConfigurationShapeGuidance.Text}"
					);
				default:
					unknown.Add(key);
					reader.SkipOrReportUnmapped(typeof(ApiProductEntryDto), KnownEntryKeys, key, entry);
					break;
			}
		}
		_ = reader.Read(); // consume MappingEnd
		entry.UnknownKeys = unknown;
		return entry;
	}

	private static void ReadLocatedString(YamlReader reader, out string? value, out int? line, out int? column)
	{
		if (reader.TokenType == YamlTokenType.Scalar)
		{
			var start = reader.Start;
			value = reader.ScalarValue;
			// SharpYaml's Mark.Line/Column are 0-based; +1 to match the 1-based convention.
			line = start.Line + 1;
			column = start.Column + 1;
			_ = reader.Read();
		}
		else
		{
			value = null;
			line = null;
			column = null;
			reader.Skip();
		}
	}

	private static ApiCatalogDto? ReadCatalog(YamlReader reader)
	{
		if (reader.TokenType != YamlTokenType.StartMapping)
		{
			reader.Skip();
			return null;
		}

		var start = reader.Start;
		_ = reader.Read();
		// SharpYaml's Mark.Line/Column are 0-based; +1 to match the 1-based convention.
		var catalog = new ApiCatalogDto { Line = start.Line + 1, Column = start.Column + 1 };
		while (reader.TokenType != YamlTokenType.EndMapping)
		{
			var key = reader.GetScalarValue();
			_ = reader.Read();
			if (key == "categories")
				catalog.Categories = ReadStringSequence(reader);
			else
				reader.SkipOrReportUnmapped(typeof(ApiCatalogDto), KnownCatalogKeys, key, catalog);
		}
		_ = reader.Read();
		return catalog;
	}

	private static List<string> ReadStringSequence(YamlReader reader)
	{
		var values = new List<string>();
		if (reader.TokenType != YamlTokenType.StartSequence)
		{
			reader.Skip();
			return values;
		}

		_ = reader.Read();
		while (reader.TokenType != YamlTokenType.EndSequence)
		{
			if (reader.TokenType == YamlTokenType.Scalar)
			{
				values.Add(reader.ScalarValue ?? string.Empty);
				_ = reader.Read();
			}
			else
				reader.Skip();
		}
		_ = reader.Read();
		return values;
	}

	private static List<ApiEntryChildDto> ReadChildren(YamlReader reader)
	{
		var children = new List<ApiEntryChildDto>();
		if (reader.TokenType != YamlTokenType.StartSequence)
		{
			reader.Skip();
			return children;
		}

		_ = reader.Read(); // consume StartSequence
		while (reader.TokenType != YamlTokenType.EndSequence)
		{
			if (reader.TokenType != YamlTokenType.StartMapping)
			{
				reader.Skip();
				continue;
			}

			_ = reader.Read(); // consume MappingStart
			var child = new ApiEntryChildDto();
			while (reader.TokenType != YamlTokenType.EndMapping)
			{
				var childKey = reader.GetScalarValue();
				_ = reader.Read();
				if (childKey == "file" && reader.TokenType == YamlTokenType.Scalar)
				{
					child.File = reader.ScalarValue;
					_ = reader.Read();
				}
				else if (childKey != "file")
					reader.SkipOrReportUnmapped(typeof(ApiEntryChildDto), KnownChildKeys, childKey, child);
				else
					reader.Skip();
			}
			_ = reader.Read(); // consume MappingEnd
			children.Add(child);
		}
		_ = reader.Read(); // consume SequenceEnd
		return children;
	}

	public override void Write(YamlWriter writer, ApiProductEntryDto value) =>
		throw new NotSupportedException(
			"ApiProductEntryDto YAML serialization is not supported; api configuration is only ever read from YAML."
		);
}
