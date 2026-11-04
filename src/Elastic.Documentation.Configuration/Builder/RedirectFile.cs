// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Links;
using SharpYaml.Model;
using static Elastic.Documentation.SymlinkValidator;

namespace Elastic.Documentation.Configuration.Builder;

public record RedirectFile
{
	public Dictionary<string, LinkRedirect>? Redirects { get; }
	public IFileInfo Source { get; }
	private IDocumentationSetContext Context { get; }

	public RedirectFile(IDocumentationSetContext context, IFileInfo? source = null)
	{
		var docsetConfigurationPath = context.ConfigurationPath;
		var redirectFileName = docsetConfigurationPath.Name.StartsWith('_') ? "_redirects.yml" : "redirects.yml";
		var redirectFileInfo = docsetConfigurationPath
			.FileSystem
			.FileInfo
			.New(System.IO.Path.Join(docsetConfigurationPath.Directory!.FullName, redirectFileName));
		Source = source ?? redirectFileInfo;
		Context = context;

		if (!Source.Exists)
			return;

		// Validate that the redirects.yml is not a symlink (security: prevents path traversal attacks)
		EnsureNotSymlink(Source);

		var reader = new YamlStreamReader(Source, Context.Collector);
		try
		{
			foreach (var entry in reader.Read())
			{
				switch (entry.Key)
				{
					case "redirects":
						Redirects = ReadRedirects(reader, entry.Entry);
						break;
					default:
						reader.EmitUnknownKey("$", entry.Entry.Key, KnownTopLevelKeys);
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

	private static readonly string[] KnownTopLevelKeys = ["redirects"];
	private static readonly string[] KnownRedirectKeys = ["to", "anchors", "many"];
	private static readonly string[] KnownManyKeys = ["to", "anchors"];

	private static Dictionary<string, LinkRedirect>? ReadRedirects(YamlStreamReader reader, KeyValuePair<YamlElement, YamlElement?> entry)
	{
		if (!reader.ReadObjectDictionary(entry, out var mapping))
			return null;

		var dictionary = new Dictionary<string, LinkRedirect>(StringComparer.OrdinalIgnoreCase);

		foreach (var entryValue in mapping)
		{
			if (entryValue.Key is not YamlValue scalar)
				continue;
			var key = scalar.Value;
			if (entryValue.Value is YamlValue)
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

			var linkRedirect = ReadLinkRedirect(reader, key, valueMapping);
			if (linkRedirect is not null)
				dictionary.Add(key, linkRedirect);
		}

		return dictionary;
	}

	private static LinkRedirect? ReadLinkRedirect(YamlStreamReader reader, string file, YamlMapping mapping)
	{
		var redirect = new LinkRedirect();
		foreach (var entryValue in mapping)
		{
			if (entryValue.Key is not YamlValue scalar)
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
					var many = ReadManyRedirects(reader, file, entryValue.Value);
					redirect = redirect with { Many = many };
					continue;
				default:
					reader.EmitUnknownKey($"$.redirects['{file}']", entryValue.Key, KnownRedirectKeys);
					continue;
			}
		}

		if (redirect.To is null && redirect.Anchors is null && redirect.Many is null)
			return null;

		if (redirect.To is null && redirect.Many is null or { Length: 0 })
			return redirect with { To = file };

		return string.IsNullOrEmpty(redirect.To) && redirect.Many is null or { Length: 0 } ? null : redirect;
	}

	private static LinkSingleRedirect[]? ReadManyRedirects(YamlStreamReader reader, string file, YamlElement? node)
	{
		if (node is not YamlSequence sequence)
			return null;

		var redirects = new List<LinkSingleRedirect>();
		foreach (var entryValue in sequence)
		{
			if (entryValue is not YamlMapping mapping)
				continue;
			var redirect = new LinkRedirect();
			foreach (var keyValue in mapping)
			{
				if (keyValue.Key is not YamlValue scalar)
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
					default:
						reader.EmitUnknownKey($"$.redirects['{file}'].many", keyValue.Key, KnownManyKeys);
						continue;
				}
			}

			if (redirect.To is null && redirect.Anchors is not null && redirect.Anchors.Count >= 0)
				redirect = redirect with { To = file };
			redirects.Add(redirect);
		}

		if (redirects.Count == 0)
			return null;

		return [.. redirects.Where(r => r.To is not null && r.Anchors is not null && r.Anchors.Count >= 0)];
	}
}
