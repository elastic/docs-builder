// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Diagnostics;

/// <summary>
/// What stops the process when global configuration is invalid: every diagnostic the loaders found, not only the first.
/// The message is every error's message, one per line.
/// </summary>
public sealed class ConfigurationException(
	IReadOnlyList<Diagnostic> diagnostics,
	string fallbackMessage = "Invalid configuration"
) : InvalidOperationException(MessageOf(diagnostics, fallbackMessage))
{
	/// <summary>Every diagnostic of the failed load, warnings and hints included.</summary>
	public IReadOnlyList<Diagnostic> Diagnostics { get; } = diagnostics;

	/// <summary>The diagnostics as one line each: <c>error: file(line,column): message [rule-id]</c>.</summary>
	public IEnumerable<string> Lines => Diagnostics.Select(Format);

	private static string MessageOf(IReadOnlyList<Diagnostic> diagnostics, string fallback)
	{
		var errors = diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message).ToList();
		return errors.Count == 0 ? fallback : string.Join(Environment.NewLine, errors);
	}

	private static string Format(Diagnostic d)
	{
		var position = d.Line is { } line ? d.Column is { } column ? $"({line},{column})" : $"({line})" : string.Empty;
		var rule = d.RuleId is { } id ? $" [{id}]" : string.Empty;
		return $"{d.Severity.ToString().ToLowerInvariant()}: {d.File}{position}: {d.Message}{rule}";
	}
}
