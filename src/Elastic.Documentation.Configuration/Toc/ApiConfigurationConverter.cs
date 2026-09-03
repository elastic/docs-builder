// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

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
public class ApiConfigurationConverter : IYamlTypeConverter
{
	private const string ShapeGuidance = "Use the single-entry sequence form instead:\n"
		+ "  <key>:\n"
		+ "    - spec: <path>       # required; its basename resolves the remote version index\n"
		+ "      product: <id>      # required, must match a products.yml entry\n"
		+ "      repository: <org/repo> # optional; only needed if the spec is published from a\n"
		+ "                              # different repo than the current checkout\n"
		+ "      children:          # optional\n"
		+ "        - file: getting-started.md";

	public bool Accepts(Type type) => type == typeof(ApiProductSequence) || type == typeof(ApiProductEntry);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer) =>
		type == typeof(ApiProductSequence) ? ReadSequence(parser) : ReadEntry(parser);

	private ApiProductSequence ReadSequence(IParser parser)
	{
		if (parser.Current is not SequenceStart)
		{
			throw new YamlException(
				parser.Current?.Start ?? Mark.Empty,
				parser.Current?.End ?? Mark.Empty,
				$"API configuration for this key must be a sequence with exactly one entry. {ShapeGuidance}"
			);
		}

		_ = parser.MoveNext(); // consume SequenceStart
		var entries = new List<ApiProductEntry>();
		while (parser.Current is not SequenceEnd)
			entries.Add(ReadEntry(parser));
		_ = parser.MoveNext(); // consume SequenceEnd

		return new ApiProductSequence { Entries = entries };
	}

	private ApiProductEntry ReadEntry(IParser parser)
	{
		if (parser.Current is not MappingStart)
		{
			throw new YamlException(
				parser.Current?.Start ?? Mark.Empty,
				parser.Current?.End ?? Mark.Empty,
				$"Each API entry must be a mapping with 'spec', 'product', and optional 'children' keys. {ShapeGuidance}"
			);
		}

		var entryStart = parser.Current.Start;
		_ = parser.MoveNext(); // consume MappingStart

		var entry = new ApiProductEntry { Line = (int)entryStart.Line, Column = (int)entryStart.Column };

		while (parser.Current is not MappingEnd)
		{
			var key = parser.Consume<Scalar>();
			switch (key.Value)
			{
				case "spec":
					var specStart = parser.Current?.Start;
					if (parser.Current is Scalar specValue)
					{
						entry.Spec = specValue.Value;
						_ = parser.MoveNext();
					}
					else
						parser.SkipThisAndNestedEvents();
					if (specStart.HasValue)
					{
						entry.SpecLine = (int)specStart.Value.Line;
						entry.SpecColumn = (int)specStart.Value.Column;
					}
					break;
				case "product":
					var productStart = parser.Current?.Start;
					if (parser.Current is Scalar productValue)
					{
						entry.Product = productValue.Value;
						_ = parser.MoveNext();
					}
					else
						parser.SkipThisAndNestedEvents();
					if (productStart.HasValue)
					{
						entry.ProductLine = (int)productStart.Value.Line;
						entry.ProductColumn = (int)productStart.Value.Column;
					}
					break;
				case "repository":
					var repositoryStart = parser.Current?.Start;
					if (parser.Current is Scalar repositoryValue)
					{
						entry.Repository = repositoryValue.Value;
						_ = parser.MoveNext();
					}
					else
						parser.SkipThisAndNestedEvents();
					if (repositoryStart.HasValue)
					{
						entry.RepositoryLine = (int)repositoryStart.Value.Line;
						entry.RepositoryColumn = (int)repositoryStart.Value.Column;
					}
					break;
				case "children":
					entry.Children = ReadChildren(parser);
					break;
				case "file":
					throw new YamlException(
						key.Start,
						key.End,
						$"'file:' entries directly in the api sequence (legacy intro/outro shape) are no longer supported. {ShapeGuidance}"
					);
				default:
					// Forward-compatible: ignore unrecognized keys rather than failing the whole build.
					parser.SkipThisAndNestedEvents();
					break;
			}
		}
		_ = parser.MoveNext(); // consume MappingEnd
		return entry;
	}

	private static List<ApiEntryChild> ReadChildren(IParser parser)
	{
		var children = new List<ApiEntryChild>();
		if (parser.Current is not SequenceStart)
		{
			parser.SkipThisAndNestedEvents();
			return children;
		}

		_ = parser.MoveNext(); // consume SequenceStart
		while (parser.Current is not SequenceEnd)
		{
			if (parser.Current is not MappingStart)
			{
				parser.SkipThisAndNestedEvents();
				continue;
			}

			_ = parser.MoveNext(); // consume MappingStart
			var child = new ApiEntryChild();
			while (parser.Current is not MappingEnd)
			{
				var childKey = parser.Consume<Scalar>();
				if (childKey.Value == "file" && parser.Current is Scalar fileValue)
				{
					child.File = fileValue.Value;
					_ = parser.MoveNext();
				}
				else
					parser.SkipThisAndNestedEvents();
			}
			_ = parser.MoveNext(); // consume MappingEnd
			children.Add(child);
		}
		_ = parser.MoveNext(); // consume SequenceEnd
		return children;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) => serializer(value, type);
}
