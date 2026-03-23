// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

public interface INavV2Item { }

public record LabelNavV2Item(
	string Label,
	bool Expanded,
	IReadOnlyList<INavV2Item> Children
) : INavV2Item;

public record TocNavV2Item(
	Uri Source,
	IReadOnlyList<INavV2Item> Children
) : INavV2Item;

/// <summary>
/// Represents a single-page crosslink (when <see cref="Page"/> is non-null) or
/// a placeholder disabled link (when <see cref="Page"/> is null and only <see cref="Title"/> is set).
/// </summary>
public record PageNavV2Item(Uri? Page, string? Title) : INavV2Item;

/// <summary>
/// A folder node — has a title and children, with an optional <c>page:</c> URI.
/// When <see cref="Page"/> is set, the header is a real clickable link; otherwise it renders
/// as a disabled placeholder (cursor-not-allowed).
/// </summary>
public record GroupNavV2Item(
	string Title,
	Uri? Page,
	IReadOnlyList<INavV2Item> Children
) : INavV2Item;

public class NavigationV2File
{
	[YamlMember(Alias = "nav")]
	public IReadOnlyList<INavV2Item> Nav { get; set; } = [];

	public static NavigationV2File Deserialize(string yaml) =>
		ConfigurationFileProvider.NavV2Deserializer.Deserialize<NavigationV2File>(yaml);
}

public class NavV2FileYamlConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(NavigationV2File) || type == typeof(INavV2Item);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		if (type == typeof(NavigationV2File))
			return ReadFile(parser, rootDeserializer);
		return ReadItem(parser, rootDeserializer);
	}

	private static NavigationV2File ReadFile(IParser parser, ObjectDeserializer rootDeserializer)
	{
		if (!parser.TryConsume<MappingStart>(out _))
			return new NavigationV2File();

		var file = new NavigationV2File();
		while (!parser.TryConsume<MappingEnd>(out _))
		{
			var key = parser.Consume<Scalar>();
			if (key.Value == "nav")
				file.Nav = ReadItemList(parser, rootDeserializer);
			else
				parser.SkipThisAndNestedEvents();
		}
		return file;
	}

	private static IReadOnlyList<INavV2Item> ReadItemList(IParser parser, ObjectDeserializer rootDeserializer)
	{
		var items = new List<INavV2Item>();
		if (!parser.TryConsume<SequenceStart>(out _))
			return items;

		while (!parser.TryConsume<SequenceEnd>(out _))
		{
			var item = ReadItem(parser, rootDeserializer);
			if (item is not null)
				items.Add(item);
		}
		return items;
	}

	private static INavV2Item? ReadItem(IParser parser, ObjectDeserializer rootDeserializer)
	{
		if (!parser.TryConsume<MappingStart>(out _))
			return null;

		var dict = new Dictionary<string, object?>();
		while (!parser.TryConsume<MappingEnd>(out _))
		{
			var key = parser.Consume<Scalar>();
			if (key.Value == "children")
				dict["children"] = ReadItemList(parser, rootDeserializer);
			else if (parser.Accept<Scalar>(out var val))
			{
				dict[key.Value] = val.Value;
				_ = parser.MoveNext();
			}
			else
				parser.SkipThisAndNestedEvents();
		}

		if (dict.TryGetValue("label", out var labelVal) && labelVal is string labelStr)
		{
			var expanded = dict.TryGetValue("expanded", out var expVal)
				&& expVal is string expStr
				&& bool.TryParse(expStr, out var expBool)
				&& expBool;
			var labelChildren = dict.TryGetValue("children", out var lch) && lch is IReadOnlyList<INavV2Item> lChildList
				? lChildList
				: [];
			return new LabelNavV2Item(labelStr, expanded, labelChildren);
		}

		if (dict.TryGetValue("toc", out var tocVal) && tocVal is string tocStr)
		{
			var uriString = tocStr.Contains("://") ? tocStr : $"docs-content://{tocStr}";
			if (!Uri.TryCreate(uriString, UriKind.Absolute, out var source))
				throw new InvalidOperationException($"Invalid TOC source: '{tocStr}'");
			var tocChildren = dict.TryGetValue("children", out var tch) && tch is IReadOnlyList<INavV2Item> tChildList
				? tChildList
				: [];
			return new TocNavV2Item(source, tocChildren);
		}

		if (dict.TryGetValue("page", out var pageVal) && pageVal is string pageStr)
		{
			var uriString = pageStr.Contains("://") ? pageStr : $"docs-content://{pageStr}";
			_ = Uri.TryCreate(uriString, UriKind.Absolute, out var pageUri);
			var title = dict.TryGetValue("title", out var t) && t is string ts ? ts : null;
			return new PageNavV2Item(pageUri, title);
		}

		if (dict.TryGetValue("group", out var groupVal) && groupVal is string groupStr)
		{
			var groupChildren = dict.TryGetValue("children", out var gch) && gch is IReadOnlyList<INavV2Item> gChildList
				? gChildList
				: [];
			Uri? groupPage = null;
			if (dict.TryGetValue("page", out var gpVal) && gpVal is string gpStr)
			{
				var gpUri = gpStr.Contains("://") ? gpStr : $"docs-content://{gpStr}";
				_ = Uri.TryCreate(gpUri, UriKind.Absolute, out groupPage);
			}
			return new GroupNavV2Item(groupStr, groupPage, groupChildren);
		}

		if (dict.TryGetValue("title", out var titleVal) && titleVal is string titleStr)
			return new PageNavV2Item(null, titleStr);

		return null;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
		serializer.Invoke(value, type);
}
