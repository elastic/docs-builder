// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Links;
using YamlDotNet.RepresentationModel;
using static Elastic.Documentation.SymlinkValidator;

namespace Elastic.Documentation.Configuration.Builder;

public record RedirectFile
{
	public Dictionary<string, LinkRedirect>? Redirects { get; }
	public IFileInfo Source { get; }

	/// <summary>Entries and <c>many</c> items the reader dropped because they hold nothing it can use, in the order read.</summary>
	public IReadOnlyList<DroppedRedirect> Dropped => _dropped;

	private readonly List<DroppedRedirect> _dropped = [];
	private IDiagnosticsCollector Collector { get; }

	public RedirectFile(IDocumentationSetContext context, IFileInfo? source = null) : this(
			context.ConfigurationPath,
			context.Collector,
			source
		)
	{ }

	/// <summary>Reads the redirects file next to <paramref name="docsetConfigurationPath"/>, reporting problems to <paramref name="collector"/>.</summary>
	public RedirectFile(IFileInfo docsetConfigurationPath, IDiagnosticsCollector collector, IFileInfo? source = null)
	{
		var redirectFileName = docsetConfigurationPath.Name.StartsWith('_') ? "_redirects.yml" : "redirects.yml";
		var redirectFileInfo = docsetConfigurationPath
			.FileSystem
			.FileInfo
			.New(Path.Join(docsetConfigurationPath.Directory!.FullName, redirectFileName));
		Source = source ?? redirectFileInfo;
		Collector = collector;

		if (!Source.Exists)
			return;

		// Validate that the redirects.yml is not a symlink (security: prevents path traversal attacks)
		EnsureNotSymlink(Source);

		var reader = new YamlStreamReader(Source, Collector);
		try
		{
			foreach (var entry in reader.Read())
			{
				switch (entry.Key)
				{
					case "redirects":
						Redirects = ReadRedirects(reader, entry.Entry, _dropped);
						break;
					default:
						reader.EmitWarning($"{entry.Key} is not a known configuration", entry.Key);
						break;
				}
			}
		}
		catch (Exception e)
		{
			reader.EmitError("Could not load docset.yml", e);
			throw;
		}
	}

	private static Dictionary<string, LinkRedirect>? ReadRedirects(
		YamlStreamReader reader,
		KeyValuePair<YamlNode, YamlNode> entry,
		List<DroppedRedirect> dropped
	)
	{
		if (!reader.ReadObjectDictionary(entry, out var mapping))
			return null;

		var dictionary = new Dictionary<string, LinkRedirect>(StringComparer.OrdinalIgnoreCase);

		foreach (var entryValue in mapping.Children)
		{
			if (entryValue.Key is not YamlScalarNode scalar || scalar.Value is null)
				continue;
			var key = scalar.Value;
			if (entryValue.Value is YamlScalarNode)
			{
				var to = reader.ReadString(entryValue);
				dictionary.Add(
					key,
					!string.IsNullOrEmpty(to)
						? to.StartsWith('!')
							? new LinkRedirect { To = to.TrimStart('!'), Anchors = LinkRedirect.CatchAllAnchors }
							: new LinkRedirect { To = to }
						: new LinkRedirect { To = "index.md", Anchors = LinkRedirect.CatchAllAnchors }
				);
				continue;
			}

			if (!reader.ReadObjectDictionary(entryValue, out var valueMapping))
				continue;

			var linkRedirect = ReadLinkRedirect(reader, key, valueMapping, dropped);
			if (linkRedirect is not null)
				dictionary.Add(key, linkRedirect);
			else
				dropped.Add(new DroppedRedirect(key));
		}

		return dictionary;
	}

	private static LinkRedirect? ReadLinkRedirect(
		YamlStreamReader reader,
		string file,
		YamlMappingNode mapping,
		List<DroppedRedirect> dropped
	)
	{
		var redirect = new LinkRedirect();
		foreach (var entryValue in mapping.Children)
		{
			if (entryValue.Key is not YamlScalarNode scalar || scalar.Value is null)
				continue;
			var key = scalar.Value;
			switch (key)
			{
				case "anchors":
					redirect = redirect with { Anchors = reader.ReadDictionaryNullValue(entryValue) };
					continue;
				case "to":
					var to = reader.ReadString(entryValue);
					if (to is not null)
						redirect = redirect with { To = to };
					continue;
				case "many":
					var many = ReadManyRedirects(reader, file, entryValue.Value, dropped);
					redirect = redirect with { Many = many };
					continue;
			}
		}

		if (redirect.To is null && redirect.Anchors is null && redirect.Many is null)
			return null;

		if (redirect.To is null && redirect.Many is null or { Length: 0 })
			return redirect with { To = file };

		return string.IsNullOrEmpty(redirect.To) && redirect.Many is null or { Length: 0 } ? null : redirect;
	}

	private static LinkSingleRedirect[]? ReadManyRedirects(
		YamlStreamReader reader,
		string file,
		YamlNode node,
		List<DroppedRedirect> dropped
	)
	{
		if (node is not YamlSequenceNode sequence)
			return null;

		var redirects = new List<LinkSingleRedirect>();
		var index = -1;
		foreach (var entryValue in sequence.Children)
		{
			index++;
			if (entryValue is not YamlMappingNode mapping)
			{
				dropped.Add(new DroppedRedirect(file, index));
				continue;
			}
			var redirect = new LinkRedirect();
			foreach (var keyValue in mapping.Children)
			{
				if (keyValue.Key is not YamlScalarNode scalar || scalar.Value is null)
					continue;
				var key = scalar.Value;
				switch (key)
				{
					case "anchors":
						redirect = redirect with { Anchors = reader.ReadDictionaryNullValue(keyValue) };
						continue;
					case "to":
						var to = reader.ReadString(keyValue);
						if (to is not null)
							redirect = redirect with { To = to };
						continue;
				}
			}

			if (redirect.To is null && redirect.Anchors is not null && redirect.Anchors.Count >= 0)
				redirect = redirect with { To = file };
			redirects.Add(redirect);
			if (redirect.To is null || redirect.Anchors is null)
				dropped.Add(new DroppedRedirect(file, index));
		}

		if (redirects.Count == 0)
			return null;

		return [.. redirects.Where(r => r.To is not null && r.Anchors is not null && r.Anchors.Count >= 0)];
	}
}
