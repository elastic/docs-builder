// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.ApiExplorer.Model;

namespace Elastic.ApiExplorer.Operations;

/// <summary>
/// Lines a sample's request method up with the path it calls. A merged page shows each path with its dominant
/// method (<c>POST</c> also <c>GET</c>), but the spec's samples come from one of the merged operations and may
/// use another of the path's methods. Those methods are interchangeable, so the sample shows the dominant one.
/// A sample whose method the path does not list, or whose path is not one of the operation's, is left as written.
/// </summary>
public static partial class SampleMethods
{
	/// <summary>Console and curl samples with their method aligned; every other language passes through untouched.</summary>
	public static IReadOnlyList<CodeSample> Align(IReadOnlyList<CodeSample> samples, OperationEndpoint endpoint) =>
		[
			.. samples.Select(
				s => s.IsConsole
					? s with { Source = AlignConsole(s.Source, endpoint) }
					: s.IsCurl ? s with { Source = AlignCurl(s.Source, endpoint) } : s
			)
		];

	/// <summary>
	/// The dominant method of the path <paramref name="target"/> calls, uppercase, when <paramref name="method"/> is
	/// one of the methods that path lists but not the dominant one. Null means the text stays as written.
	/// </summary>
	public static string? Dominant(string method, string target, OperationEndpoint endpoint)
	{
		if (!endpoint.TryFindRow(target, out var row))
			return null;
		var own = method.ToLowerInvariant();
		return own != row.Method && row.AlsoMethods.Contains(own, StringComparer.Ordinal) ? row.Method.ToUpperInvariant() : null;
	}

	/// <summary>
	/// Each request line (a method at the start of a line, then its path) against the path it names. The rule
	/// is the Console highlighter's own: a JSON body line never starts with an uppercase method and a path.
	/// </summary>
	public static string AlignConsole(string source, OperationEndpoint endpoint) =>
		ConsoleRequest().Replace(
			source,
			match => Dominant(match.Groups["method"].Value, match.Groups["path"].Value, endpoint) is { } dominant
				? dominant + match.Groups["gap"].Value + match.Groups["path"].Value
				: match.Value
		);

	/// <summary>
	/// The method flag (<c>-X GET</c>, <c>-XGET</c>, <c>--request GET</c>, <c>--request=GET</c>) against the sample's
	/// URL. A sample without a method flag is left alone: its method depends on curl's defaults and other flags.
	/// </summary>
	public static string AlignCurl(string source, OperationEndpoint endpoint)
	{
		if (CurlUrl().Match(source) is not { Success: true } url)
			return source;
		return CurlMethodFlag().Replace(
			source,
			match => Dominant(match.Groups["method"].Value, url.Value, endpoint) is { } dominant
				? match.Groups["flag"].Value + dominant
				: match.Value,
			1
		);
	}

	/// <summary>The <c>Run `METHOD path`</c> line that opens an example description, so the generated samples follow it.</summary>
	public static string AlignDescription(string description, OperationEndpoint endpoint) =>
		RunLine().Replace(
			description,
			match => Dominant(match.Groups["method"].Value, match.Groups["path"].Value, endpoint) is { } dominant
				? match.Groups["run"].Value + dominant + match.Groups["gap"].Value + match.Groups["path"].Value
				: match.Value,
			1
		);

	[GeneratedRegex(@"^(?<method>GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS)(?<gap>[ \t]+)(?<path>\S+)", RegexOptions.Multiline
		| RegexOptions.CultureInvariant)]
	private static partial Regex ConsoleRequest();

	[GeneratedRegex(@"https?://[^\s""']+|\$\{?[A-Za-z0-9_]+\}?/[^\s""']*|(?<=[""'\s])[A-Za-z0-9.-]+:\d+/[^\s""']*", RegexOptions.CultureInvariant)]
	private static partial Regex CurlUrl();

	[GeneratedRegex(@"(?<=\s)(?<flag>-X[ \t]*|--request(?:[ \t]+|=))(?<method>[A-Za-z]+)(?![\w-])", RegexOptions.CultureInvariant)]
	private static partial Regex CurlMethodFlag();

	[GeneratedRegex(@"^(?<run>\s*Run\s+`)(?<method>GET|POST|PUT|PATCH|DELETE|HEAD)(?<gap>\s+)(?<path>[^`]+)(?=`)", RegexOptions.IgnoreCase
		| RegexOptions.CultureInvariant)]
	private static partial Regex RunLine();
}
