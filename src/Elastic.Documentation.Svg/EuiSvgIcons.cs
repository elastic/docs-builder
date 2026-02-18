// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Svg;

/// <summary>
/// Provides access to EUI SVG icons embedded as resources.
/// Icons are sourced from https://eui.elastic.co/docs/components/display/icons/
/// </summary>
public static class EuiSvgIcons
{
	private static readonly Lazy<IReadOnlyDictionary<string, string>> LazyIconMap = new(LoadIcons);
	private static readonly Lazy<IReadOnlyDictionary<string, string>> LazyTokenMap = new(LoadTokens);

	/// <summary>
	/// Dictionary of icon names to their SVG content.
	/// </summary>
	public static IReadOnlyDictionary<string, string> Icons => LazyIconMap.Value;

	/// <summary>
	/// Dictionary of token names to their SVG content.
	/// </summary>
	public static IReadOnlyDictionary<string, string> Tokens => LazyTokenMap.Value;

	/// <summary>
	/// Tries to get an icon SVG by name.
	/// </summary>
	/// <param name="name">The icon name (without .svg extension)</param>
	/// <param name="svg">The SVG content if found</param>
	/// <returns>True if the icon was found, false otherwise</returns>
	public static bool TryGetIcon(string name, out string? svg) =>
		Icons.TryGetValue(name, out svg);

	/// <summary>
	/// Tries to get a token SVG by name.
	/// </summary>
	/// <param name="name">The token name (without .svg extension)</param>
	/// <param name="svg">The SVG content if found</param>
	/// <returns>True if the token was found, false otherwise</returns>
	public static bool TryGetToken(string name, out string? svg) =>
		Tokens.TryGetValue(name, out svg);

	/// <summary>
	/// Gets an icon SVG by name, returning null if not found.
	/// </summary>
	/// <param name="name">The icon name (without .svg extension)</param>
	/// <returns>The SVG content or null if not found</returns>
	public static string? GetIcon(string name) =>
		Icons.TryGetValue(name, out var svg) ? svg : null;

	/// <summary>
	/// Gets an icon SVG by name with an optional CSS class injected into the svg element.
	/// </summary>
	/// <param name="name">The icon name (without .svg extension)</param>
	/// <param name="cssClass">Optional CSS class to add to the svg element</param>
	/// <returns>The SVG content or null if not found</returns>
	public static string? GetIcon(string name, string? cssClass) =>
		Icons.TryGetValue(name, out var svg)
			? cssClass is not null ? InjectClass(svg, cssClass) : svg
			: null;

	private static string InjectClass(string svg, string cssClass) =>
		svg.Replace("<svg ", $"<svg class=\"{cssClass}\" ");

	/// <summary>
	/// Gets a token SVG by name, returning null if not found.
	/// </summary>
	/// <param name="name">The token name (without .svg extension)</param>
	/// <returns>The SVG content or null if not found</returns>
	public static string? GetToken(string name) =>
		Tokens.TryGetValue(name, out var svg) ? svg : null;

	private static IReadOnlyDictionary<string, string> LoadIcons() =>
		LoadFromPrefix("svgs.");

	private static IReadOnlyDictionary<string, string> LoadTokens() =>
		LoadFromPrefix("svgs.tokens.");

	private static IReadOnlyDictionary<string, string> LoadFromPrefix(string folderPrefix)
	{
		var assembly = typeof(EuiSvgIcons).Assembly;
		var assemblyName = assembly.GetName().Name;
		var fullPrefix = $"{assemblyName}.{folderPrefix}";

		return assembly.GetManifestResourceNames()
			.Where(r => r.StartsWith(fullPrefix, StringComparison.Ordinal) && r.EndsWith(".svg", StringComparison.Ordinal))
			.Where(r =>
			{
				// For the main svgs folder, exclude tokens subfolder
				if (folderPrefix == "svgs.")
				{
					var afterPrefix = r[fullPrefix.Length..];
					return !afterPrefix.StartsWith("tokens.", StringComparison.Ordinal);
				}
				return true;
			})
			.ToDictionary(
				r => r[fullPrefix.Length..^4], // Remove prefix and ".svg" suffix
				r =>
				{
					using var stream = assembly.GetManifestResourceStream(r);
					if (stream is null)
						return string.Empty;
					using var reader = new StreamReader(stream);
					return reader.ReadToEnd();
				}
			);
	}
}
