// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;

namespace Elastic.Documentation.Configuration;

/// <summary>
/// Reads <c>remote "origin"</c> URL entries from Git <c>config</c> file text.
/// </summary>
public static class GitConfigOriginParser
{
	/// <summary>
	/// Returns the first <c>url</c> value under <c>[remote "origin"]</c>.
	/// </summary>
	public static bool TryGetRemoteOriginUrl(string configContent, [NotNullWhen(true)] out string? url)
	{
		url = null;
		if (string.IsNullOrEmpty(configContent))
			return false;

		var inOrigin = false;
		foreach (var rawLine in configContent.Split(['\r', '\n'], StringSplitOptions.None))
		{
			var line = rawLine.Trim();
			if (line.StartsWith('['))
			{
				inOrigin = line.Equals("[remote \"origin\"]", StringComparison.Ordinal);
				continue;
			}

			if (!inOrigin)
				continue;

			if (!line.StartsWith("url", StringComparison.OrdinalIgnoreCase))
				continue;

			var eq = line.IndexOf('=');
			if (eq < 0 || eq >= line.Length - 1)
				continue;

			var value = line[(eq + 1)..].Trim();
			if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
				value = value[1..^1];

			if (string.IsNullOrEmpty(value))
				continue;

			url = value;
			return true;
		}

		return false;
	}
}
