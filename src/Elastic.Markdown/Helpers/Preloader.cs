// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Elastic.Markdown.Helpers;

public static partial class FontPreloader
{
	private static IReadOnlyCollection<string>? FontUriCache = null!;

	public static async Task<IReadOnlyCollection<string>> GetFontUrisAsync(string staticUrlPrefix) => FontUriCache ??= await LoadFontUrisAsync(staticUrlPrefix);
	private static async Task<IReadOnlyCollection<string>> LoadFontUrisAsync(string staticUrlPrefix)
	{
		var cachedFontUris = new List<string>();
		var assembly = Assembly.GetExecutingAssembly();
		var stylesResourceName = assembly.GetManifestResourceNames().First(n => n.EndsWith("styles.css"));

		using var cssFileStream = new StreamReader(assembly.GetManifestResourceStream(stylesResourceName)!);

		var cssFile = await cssFileStream.ReadToEndAsync();
		var matches = FontUriRegex().Matches(cssFile);

		foreach (Match match in matches)
		{
			if (match.Success)
				cachedFontUris.Add($"{staticUrlPrefix}{match.Groups[1].Value}");
		}
		FontUriCache = cachedFontUris;
		return FontUriCache;
	}

	[GeneratedRegex(@"url\([""']?([^""'\)]+?\.(woff2|ttf|otf))[""']?\)", RegexOptions.Multiline | RegexOptions.Compiled)]
	private static partial Regex FontUriRegex();
}
