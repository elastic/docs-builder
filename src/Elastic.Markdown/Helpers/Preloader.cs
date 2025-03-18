// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Elastic.Markdown.Helpers;

public static partial class FontPreloader
{
	private static List<string>? FontUriCache = null!;

	public static async Task<IEnumerable<string>> GetFontUrisAsync() => FontUriCache ??= await LoadFontUrisAsync();
	public static async Task<List<string>> LoadFontUrisAsync()
	{
		FontUriCache = [];
		var assembly = Assembly.GetExecutingAssembly();
		var stylesResourceName = assembly.GetManifestResourceNames().First(n => n.EndsWith("styles.css"));

		using var cssFileStream = new StreamReader(assembly.GetManifestResourceStream(stylesResourceName)!);

		var cssFile = await cssFileStream.ReadToEndAsync();
		var matches = FontUriRegex().Matches(cssFile);

		foreach (Match match in matches)
		{
			if (match.Success)
				FontUriCache.Add($"/_static/{match.Groups[1].Value}");
		}
		return FontUriCache;
	}

	[GeneratedRegex(@"url\([""']?([^""'\)]+)[""']?\)", RegexOptions.Multiline | RegexOptions.Compiled)]
	private static partial Regex FontUriRegex();
}
